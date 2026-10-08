using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// GGD-style interaction zone (mirror of GGD's Proximity component).
    /// A child trigger collider around an Interactable; the manager no longer
    /// measures distances — it asks whether the player's SENSE TRIGGER
    /// (a larger trigger bubble on the player, GGD's PlayerTriggerCollider)
    /// currently overlaps this zone. Solid colliders (walls, bodies) never
    /// generate these callbacks, so only genuine walk-in counts.
    /// </summary>
    public class Proximity2D : MonoBehaviour
    {
        /// <summary>Set while the player's sense bubble overlaps this zone.</summary>
        public bool IsInProximity { get; private set; }

        /// <summary>Last time IsInProximity flipped true (for priority tiebreaks).</summary>
        public float EnteredAt { get; private set; } = -999f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerSense(other)) { IsInProximity = true; EnteredAt = Time.time; }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerSense(other)) IsInProximity = false;
        }

        /// <summary>
        /// Only the player's trigger bubble counts. The player has two colliders
        /// (GGD's two-collider pattern): a solid body that blocks walls, and a
        /// trigger "sense" collider that walks into these zones. This test fires
        /// for the trigger one; the solid body never calls trigger callbacks.
        /// NPC/quiz trigger colliders fail the tag + component test and are ignored.
        /// </summary>
        private static bool IsPlayerSense(Collider2D other)
        {
            if (!other.isTrigger) return false;
            var rb = other.attachedRigidbody;
            return rb != null && rb.CompareTag("Player") && rb.GetComponent<PlayerController2D>() != null;
        }

        /// <summary>Editor/scene-build helper: keeps honest state after moving the zone.</summary>
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
            var b = col.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
