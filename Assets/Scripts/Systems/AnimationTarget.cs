using System;
using UnityEngine;

namespace Systems
{
    public class AnimationTarget : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private Action _onComplete;

        private void Awake()
        {
            animator.enabled = false;
        }

        public bool PlayTrigger(string triggerName, Action onComplete)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName))
                return false;

            animator.enabled = true;
            
            _onComplete = onComplete;
            animator.SetTrigger(triggerName);
            return true;
        }

        public void NotifyAnimationComplete()
        {
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
            animator.enabled = false;
        }
    }
}