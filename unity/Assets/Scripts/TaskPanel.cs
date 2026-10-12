using System;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 — GGD TaskPanel port (base class).
    /// GGD reference (dump.cs 139190): TaskPanel has Action&lt;bool&gt; onComplete,
    /// CompleteAndClose(delay 0.25) / FailAndClose(delay 0.25), CoClose coroutine,
    /// InitAsSubTask. Panel closing resumes movement (InteractionManager handles
    /// the pause; the panel just reports its result).
    /// </summary>
    public abstract class TaskPanel : MonoBehaviour
    {
        public const float DefaultCloseDelay = 0.25f;   // GGD DEFAULT_CLOSE_DELAY

        /// <summary>true = completed, false = failed/cancelled.</summary>
        public event Action<bool> OnComplete;

        public bool HasInitialized { get; protected set; }
        protected bool IsClosing_ { get; set; }

        protected Coroutine _closeCo;

        /// <summary>Open the panel (and pause the player — mirrors GGD task flow).</summary>
        public virtual void Open()
        {
            HasInitialized = true;
            IsClosing_ = false;
            InteractionManager.PauseForTask(true);
            PanelAnimator.Ensure(gameObject).FadeIn();   // R6: GGD-style fade-in
        }

        /// <summary>GGD CompleteAndClose: report success, close after the delay.</summary>
        public virtual void CompleteAndClose(float delay = DefaultCloseDelay)
        {
            if (IsClosing_) return;
            IsClosing_ = true;
            Finish(true, delay);
        }

        /// <summary>GGD FailAndClose: report failure, close after the delay.</summary>
        public virtual void FailAndClose(float delay = DefaultCloseDelay)
        {
            if (IsClosing_) return;
            IsClosing_ = true;
            Finish(false, delay);
        }

        private void Finish(bool success, float delay)
        {
            if (_closeCo != null) StopCoroutine(_closeCo);
            _closeCo = StartCoroutine(CoClose(success, delay));
        }

        /// <summary>GGD CoClose: hold the result on screen for the delay, fade out,
        /// then deactivate + resume movement.</summary>
        private System.Collections.IEnumerator CoClose(bool success, float delay)
        {
            yield return new WaitForSeconds(delay);
            _closeCo = null;
            var pa = PanelAnimator.Ensure(gameObject);
            pa.FadeOut(() =>
            {
                if (InteractionManager.QuestWon)
                {
                    InteractionManager.ClearTaskOwnership();   // R7: task no longer owns input…
                    return;                                    // …but the win-freeze owns the player
                }
                InteractionManager.PauseForTask(false);
                OnComplete?.Invoke(success);
            });
        }

        protected virtual void OnDestroy()
        {
            // safety: never leave the player frozen if the panel dies mid-task
            if (gameObject.activeInHierarchy && InteractionManager.TaskPaused) InteractionManager.PauseForTask(false);
        }
    }
}
