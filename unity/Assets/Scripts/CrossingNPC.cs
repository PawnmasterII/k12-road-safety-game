using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// An NPC who wants to cross the road. Talk to them (SPACE near them) -> they
    /// say they need help -> on dialog end they follow the player.
    ///
    /// Spec 006 rework: a crossing only COUNTS as a real traversal —
    ///   1. the pair starts on one side of the ROAD (recorded at escort start),
    ///   2. stays inside the zebra corridor while on the road (jaywalking outside
    ///      the stripes abandons the escort),
    ///   3. and the NPC reaches the OPPOSITE shoulder with the player on that same
    ///      opposite shoulder within escortRadius — "arrive on the far side together".
    /// Entering the stripes and walking back out the same side never scores; visiting
    /// the zebra QUIZ zone mid-escort never scores (modals pause the escort logic);
    /// losing the player (leash) or dragging the NPC along the road off-corridor
    /// abandons with a spoken reason. The old logic (both-in-rect co-location from a
    /// per-frame sprite sweep + 0.3 phantom margin, hardcoded north exit) is gone —
    /// see specs/006-crossing-logic.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrossingNPC : MonoBehaviour
    {
        public string npcName = "friend";

        [Tooltip("Escort breaks if the player gets farther than this (world units).")]
        public float leashDistance = 6f;
        [Tooltip("How close the player must be to the NPC on the far shoulder for the crossing to count.")]
        public float escortRadius = 2.5f;

        private enum EscortState { None, Escorting, Crossed }
        private EscortState _state = EscortState.None;

        private Conversation _intro;
        private Conversation _thanks;
        private int _startSide;          // +1 north, -1 south (CrossingZone.SideOf at escort start)
        private Transform _player;

        // ---- Spec 006 R2: abandons are NON-BLOCKING toasts, not modal dialogs ----
        // The old Abandon() opened a full dialog (freezes player, SPACE to clear);
        // retrying immediately re-ran the intro dialog -> abandon -> intro -> ...
        // infinite dialog loop. Now: floating toast over the NPC + a short re-talk
        // cooldown, player never frozen, no dialog chain.
        private const float ToastSeconds = 2.2f;
        private const float RetalkCooldown = 3f;
        private float _cooldownUntil;
        private TextMesh _toast;
        private float _toastUntil;

        // ---- Spec 006 R3: the re-talk loop ----
        // After an abandon the NPC could be stranded ON the road (side 0); Talk()
        // gated the escort on _startSide != 0, so the escort silently never started
        // and every SPACE re-ran the intro dialog — the "infinite dialog loop".
        // Fixes: (1) escort always starts; startSide==0 means "undetermined" and is
        // captured the first time the NPC reaches a shoulder; (2) an abandon while
        // on-road makes the NPC flee to the nearest shoulder (never stranded in
        // traffic, next re-talk always starts sane); (3) retries use a one-liner.
        private bool _fleeing;
        private Conversation _retry;
        private bool _attempted;

        private void Awake()
        {
            _intro = ScriptableObject.CreateInstance<Conversation>();
            _intro.speakerName = npcName;
            _intro.lines = new[]
            {
                $"Hi! I'm {npcName} and I need to cross this road.",
                "Will you walk with me to the zebra stripes?",
            };
            _intro.closingTip = "Walk onto the white stripes and cross to the other side together!";

            _thanks = ScriptableObject.CreateInstance<Conversation>();
            _thanks.speakerName = npcName;
            _thanks.lines = new[] { "Thank you! You're a safety hero!" };
        }

        private void Update()
        {
            if (_toast != null && Time.time > _toastUntil)
            {
                Destroy(_toast.gameObject);
                _toast = null;
            }
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                _player = p.transform;
            }

            // R3: fleeing to the shoulder after an on-road abandon. No leash, no
            // rules — just get off the road, then resume glue-follow.
            if (_fleeing)
            {
                if (IsModalOpen()) return;
                int side = CrossingZone.SideOf(transform.position);
                if (side != 0) { _fleeing = false; return; }
                float dirY = transform.position.y >= 0f ? 1f : -1f;
                transform.position = Vector3.MoveTowards(transform.position,
                    new Vector3(transform.position.x, dirY * 4f, transform.position.z),
                    3f * Time.deltaTime);
                return;
            }

            if (_state != EscortState.Escorting) return;

            // A modal (quiz/dialog/minigame) owns input — the escort holds still and
            // judges nothing while it is open (so quiz visits can't complete crossings).
            if (IsModalOpen()) return;

            // R3: startSide 0 = not yet on a shoulder — capture it the first time
            // the NPC reaches one (this also makes mid-road re-talks work).
            if (_startSide == 0)
            {
                int s = CrossingZone.SideOf(transform.position);
                if (s != 0) _startSide = s;
            }

            // 1. Leash: player wandered off.
            if (Vector2.Distance(transform.position, _player.position) > leashDistance)
            {
                Abandon("I got scared! Stay close to me!");
                return;
            }

            // 2. Corridor containment: while ON the road, the NPC must be within the
            //    painted stripes. Only enforced once we know the starting side —
            //    a mid-road escort start walks to the nearest shoulder (flee) or
            //    into the corridor freely until a side is established.
            var zone = CrossingZone.Instance;
            if (zone != null && _startSide != 0 && Mathf.Abs(transform.position.y) <= zone.roadHalfY)
            {
                var c = zone.corridor;
                if (transform.position.x < c.xMin || transform.position.x > c.xMax)
                {
                    Abandon("We must stay ON the stripes! Let's try again.");
                    return;
                }
            }

            // 3. Completion: NPC reached the opposite shoulder...
            int npcSide = zone != null ? CrossingZone.SideOf(transform.position) : 0;
            if (_startSide != 0 && npcSide != 0 && npcSide == -_startSide)
            {
                // ...and the player came too — same opposite shoulder, within radius.
                int playerSide = CrossingZone.SideOf(_player.position);
                float dist = Vector2.Distance(transform.position, _player.position);
                if (playerSide == npcSide && dist <= escortRadius)
                {
                    StartCoroutine(CrossAndLeave());
                    return;
                }
                // NPC alone on the far side with the player not there: hold position
                // (the follow step below drags them back toward the player).
            }

            // 4. Follow the player (simple MoveTowards glue; nav steering is out of scope).
            var dir = (_player.position - transform.position);
            if (dir.magnitude > 0.8f)
                transform.position = Vector3.MoveTowards(
                    transform.position, _player.position, 2.5f * Time.deltaTime);
        }

        private static bool IsModalOpen()
        {
            if (InteractionManager.TaskPaused) return true;
            if (QuizManager.Instance != null && QuizManager.Instance.IsOpen) return true;
            if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return true;
            return false;
        }

        /// <summary>
        /// Non-blocking abandon: floating toast above the NPC (auto-expires), escort
        /// resets, player is NEVER frozen. A re-talk within RetalkCooldown is refused,
        /// which breaks the intro->abandon->intro dialog loop. No dialogs involved.
        /// R3: if the NPC is stranded ON the road, she flees to the nearest shoulder
        /// first — the next talk always starts from sane footing.
        /// </summary>
        private void Abandon(string reason)
        {
            _state = EscortState.None;
            _startSide = 0;
            _fleeing = CrossingZone.SideOf(transform.position) == 0;
            _cooldownUntil = Time.time + RetalkCooldown;
            ShowToast(reason);
        }

        /// <summary>Small world-space text above the NPC; expires on its own (no input).</summary>
        private void ShowToast(string text)
        {
            if (_toast == null)
            {
                var go = new GameObject(npcName + "_Toast");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, 0.85f, -3f);
                _toast = go.AddComponent<TextMesh>();
                DialogStyle.Style(_toast, 0.11f, 26, TextAnchor.MiddleCenter,
                    new Color(1f, 0.62f, 0.35f), 30);
            }
            _toast.text = text;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.time + ToastSeconds;
        }

        /// <summary>
        /// Zebra geometry authority lives in CrossingZone (stripe-union corridor +
        /// road half-height from the Road sprite). Kept as a thin static for any
        /// external callers; no per-frame sprite sweep anymore.
        /// </summary>
        public static bool InZebraZone(Vector3 pos) => CrossingZone.InCorridor(pos);

        /// <summary>Called by InteractionManager when the player talks to this NPC.</summary>
        public void Talk()
        {
            if (_state != EscortState.None) return;
            if (Time.time < _cooldownUntil) return;   // breaks the abandon->intro dialog loop
            if (DialogManager.Instance.IsOpen) return;

            // R3: retries get a one-liner, not the full intro (no dialog marathon).
            // _attempted flips true after the first escort attempt.
            if (_attempted && _retry == null)
            {
                _retry = ScriptableObject.CreateInstance<Conversation>();
                _retry.speakerName = npcName;
                _retry.lines = new[] { "Okay, let's try again - stripes, together!" };
            }
            var conv = _attempted ? _retry : _intro;
            _attempted = true;

            DialogManager.Instance.StartDialog(conv, gameObject, () =>
            {
                _player = GameObject.FindWithTag("Player")?.transform;
                // R3: escort ALWAYS starts. Side 0 (stranded mid-road) = "undetermined";
                // it is captured the first time the NPC reaches a shoulder in Update.
                _startSide = CrossingZone.SideOf(transform.position);
                _state = EscortState.Escorting;
            });
        }

        private IEnumerator CrossAndLeave()
        {
            _state = EscortState.Crossed;
            QuestManager.Instance.RecordCrossing(npcName);

            // walk AWAY from the road on the side we actually arrived on
            var target = transform.position + new Vector3(0f, Mathf.Sign(transform.position.y) * 8f, 0f);
            while (Vector3.Distance(transform.position, target) > 0.2f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target, 2.5f * Time.deltaTime);
                yield return null;
            }

            // thank-you WITHOUT freezing the player: defer until no modal owns the
            // screen (quiz-visit deferral, spec 006; 6s cap), show the line, wait
            // until it's read, then despawn. No dialog race.
            var dm = DialogManager.Instance;
            if (dm != null)
            {
                float waited = 0f;
                while (waited < 6f && IsModalOpen())
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                if (!dm.IsOpen)
                {
                    dm.StartDialog(_thanks, gameObject);
                    waited = 0f;
                    while (dm.IsOpen && waited < 6f)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                    if (dm.IsOpen && dm.Source == gameObject) dm.Close();
                }
            }
            Destroy(gameObject);
        }
    }
}
