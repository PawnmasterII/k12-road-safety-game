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
            if (_state != EscortState.Escorting) return;
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                _player = p.transform;
            }

            // A modal (quiz/dialog/minigame) owns input — the escort holds still and
            // judges nothing while it is open (so quiz visits can't complete crossings).
            if (IsModalOpen()) return;

            // 1. Leash: player wandered off.
            if (Vector2.Distance(transform.position, _player.position) > leashDistance)
            {
                Abandon("I got scared! Stay close to me!");
                return;
            }

            // 2. Corridor containment: while ON the road, the NPC must be within the
            //    painted stripes' x-range. Dragging along/at the road anywhere else
            //    is jaywalking — abandon.
            var zone = CrossingZone.Instance;
            if (zone != null && Mathf.Abs(transform.position.y) <= zone.roadHalfY)
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
            if (npcSide != 0 && npcSide == -_startSide)
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

        /// <summary>One-shot spoken reason, no pause, escort resets so the player can retry.</summary>
        private void Abandon(string reason)
        {
            _state = EscortState.None;
            _startSide = 0;
            var dm = DialogManager.Instance;
            if (dm != null && !dm.IsOpen)
            {
                var line = ScriptableObject.CreateInstance<Conversation>();
                line.speakerName = npcName;
                line.lines = new[] { reason };
                dm.StartDialog(line, gameObject);
            }
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
            if (DialogManager.Instance.IsOpen) return;

            DialogManager.Instance.StartDialog(_intro, gameObject, () =>
            {
                _player = GameObject.FindWithTag("Player")?.transform;
                _startSide = CrossingZone.SideOf(transform.position);   // record where the pair starts
                _state = _startSide != 0 ? EscortState.Escorting : EscortState.None;
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
