using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 006 R4 — the "never again" procedure for modal-input races.
    ///
    /// One frame-based swallow window: the SPACE keypress that CLOSED a modal must
    /// never also open/advance the next thing in the same or the next frame.
    /// Without this, Update() order between DialogManager and InteractionManager is
    /// undefined, so "advance + close" then "route the same still-down keypress"
    /// produced the infinite dialog loop on Officer Ada (quiz zones and interaction
    /// popups carried the same latent race).
    ///
    /// THE RULE (add to any new modal or input consumer):
    ///   1. Every code path that CLOSES a modal calls SwallowSpacePress().
    ///   2. Every Update that polls Input.GetKeyDown(Space) checks SpaceBlocked first.
    /// </summary>
    public static class ModalInputGate
    {
        static int _swallowUntilFrame = int.MinValue;

        /// <summary>True while SPACE polls must be ignored (close frame + the frame after).</summary>
        public static bool SpaceBlocked => Time.frameCount <= _swallowUntilFrame;

        /// <summary>Call from every modal Close() path.</summary>
        public static void SwallowSpacePress() => _swallowUntilFrame = Time.frameCount + 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            // Statics survive play-mode transitions and domain reloads in the Editor
            // (project convention); a stale swallow would eat real inputs next session.
            _swallowUntilFrame = int.MinValue;
        }
    }
}
