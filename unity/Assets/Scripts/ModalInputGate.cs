using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 006 R4/R5 — the "never again" procedure for the modal-input bug class:
    /// one physical input (or one stale async callback) consumed by the wrong
    /// consumer because Update() order between scripts is undefined.
    ///
    /// THE RULES (add to any new modal or input consumer):
    ///   1. Every code path that OPENS or CLOSES a modal calls SwallowSpacePress().
    ///   2. Every Update that polls Input checks ModalInputGate.InputAllowed first.
    ///   3. Openers (StartDialog/StartQuiz/...) REFUSE while blocked — the choke
    ///      point covers every caller: router, direct calls, walk-in triggers.
    ///   4. (R5) Any resume-movement callback passed to a FadeOut must validate the
    ///      modal is STILL closed (`!IsOpen` / `Open == null`) before unpausing —
    ///      a new modal may have opened during the 0.16s fade.
    ///   5. (R5) In the Editor, ALL input is dead while the window is unfocused —
    ///      stray physical keypresses must never drive play mode (session-5 defect).
    /// Run SafeStreets.EditorTools.ModalSafetyAudit.Run() (Tools > Modal Safety
    /// Audit) — it FAILS on any script that polls Input without the gate.
    /// </summary>
    public static class ModalInputGate
    {
        static int _swallowUntilFrame = int.MinValue;

        /// <summary>True while SPACE polls must be ignored (close frame + the frame after).</summary>
        public static bool SpaceBlocked => Time.frameCount <= _swallowUntilFrame;

        /// <summary>
        /// R5: the ONE check every input poller uses. False while a swallow window is
        /// active, and (Editor only) while the Editor window is unfocused — stray
        /// physical keypresses must never drive the game.
        /// </summary>
        public static bool InputAllowed =>
            !SpaceBlocked && (!Application.isEditor || Application.isFocused);

        /// <summary>Call from every modal open/close path.</summary>
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
