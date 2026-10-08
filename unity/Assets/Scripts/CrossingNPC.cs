using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// An NPC who wants to cross the road. Talk to them (SPACE near them) -> they
    /// say they need help -> on dialog end they follow the player. Walk them to the
    /// zebra stripes -> when both are on the crossing, the crossing "counts"
    /// (QuestManager), the NPC walks to the other side and disappears with a thank-you.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrossingNPC : MonoBehaviour
    {
        public string npcName = "friend";

        private Conversation _intro;
        private Conversation _thanks;
        private bool _following;
        private bool _crossed;
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
            _intro.closingTip = "Walk onto the white stripes together!";

            _thanks = ScriptableObject.CreateInstance<Conversation>();
            _thanks.speakerName = npcName;
            _thanks.lines = new[] { "Thank you! You're a safety hero!" };
        }

        private void Update()
        {
            if (_following && !_crossed)
            {
                if (_player == null) return;

                if (InZebraZone(transform.position) && InZebraZone(_player.position)
                    && Vector2.Distance(transform.position, _player.position) < 2f)
                {
                    StartCoroutine(CrossAndLeave());
                    return;
                }

                // follow the player
                var dir = (_player.position - transform.position);
                if (dir.magnitude > 0.8f)
                    transform.position = Vector3.MoveTowards(
                        transform.position, _player.position, 2.5f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Zebra zone read from the actual scene objects (any active Zebra* sprite),
        /// not hardcoded numbers — moving/rebuilding the crossing can't silently break it.
        /// </summary>
        public static bool InZebraZone(Vector3 pos)
        {
            foreach (var z in GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!z.name.StartsWith("Zebra")) continue;
                var b = z.bounds;
                if (pos.x >= b.min.x - 0.3f && pos.x <= b.max.x + 0.3f
                    && pos.y >= b.min.y - 0.3f && pos.y <= b.max.y + 0.3f)
                    return true;
            }
            return false;
        }

        /// <summary>Called by InteractionManager when the player talks to this NPC.</summary>
        public void Talk()
        {
            if (_crossed || _following) return;
            if (DialogManager.Instance.IsOpen) return;

            DialogManager.Instance.StartDialog(_intro, gameObject, () =>
            {
                _following = true;
                _player = GameObject.FindWithTag("Player").transform;
            });
        }

        private IEnumerator CrossAndLeave()
        {
            _crossed = true;
            QuestManager.Instance.RecordCrossing(npcName);

            // walk off to the other side, then vanish
            var target = transform.position + new Vector3(0f, 8f, 0f);
            while (Vector3.Distance(transform.position, target) > 0.2f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target, 2.5f * Time.deltaTime);
                yield return null;
            }

            // thank-you WITHOUT freezing the player for a fixed timer: show the line,
            // wait until it's read (or 6s cap), then despawn. No dialog race.
            if (DialogManager.Instance != null && !DialogManager.Instance.IsOpen)
            {
                // one-shot toast line: reuse the dialog panel but DON'T set Source/Pause
                var dm = DialogManager.Instance;
                dm.StartDialog(_thanks, gameObject);
                float waited = 0f;
                while (dm.IsOpen && waited < 6f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                if (dm.IsOpen && dm.Source == gameObject) dm.Close();
            }
            Destroy(gameObject);
        }
    }
}
