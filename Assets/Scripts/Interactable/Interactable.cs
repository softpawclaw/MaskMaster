using System;
using UnityEngine;

namespace Interactable
{
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private bool oneShot = false;
        [SerializeField] private bool autoComplete = false;
        [SerializeField] private ContextInteractionCondition[] contextInteractionConditions;

        protected bool used;
        private bool interactionInProgress;
        private bool suppressAfterContextOnCompletion;
        private Action completionCallback;

        public event Action<GameObject> InteractionCompleted;

        public void Interact(GameObject interactor)
        {
            StartInteraction(interactor, null);
        }

        public void Interact(GameObject interactor, Action onCompleted)
        {
            StartInteraction(interactor, onCompleted);
        }

        private void StartInteraction(GameObject interactor, Action onCompleted)
        {
            if (oneShot && used) return;
            if (interactionInProgress) return;

            if (TryHandleContextConditions(ContextInteractionCondition.InteractionMoment.BeforeInteraction, interactor, onCompleted))
            {
                return;
            }

            StartMainInteraction(interactor, onCompleted);
        }

        private void StartMainInteraction(GameObject interactor, Action onCompleted)
        {
            used = true;
            interactionInProgress = true;
            completionCallback = onCompleted;

            OnInteract(interactor);

            if (autoComplete)
            {
                CompleteInteraction(interactor);
            }
        }

        protected bool TryHandleContextConditions(ContextInteractionCondition.InteractionMoment moment, GameObject interactor, Action onCompleted)
        {
            var conditions = GetContextConditions();
            if (conditions == null || conditions.Length == 0) return false;

            for (int i = 0; i < conditions.Length; i++)
            {
                var condition = conditions[i];
                if (condition == null || !condition.isActiveAndEnabled) continue;

                if (moment == ContextInteractionCondition.InteractionMoment.BeforeInteraction)
                {
                    interactionInProgress = true;
                }

                bool handled = condition.TryExecute(moment, interactor, allowContinue =>
                {
                    if (moment == ContextInteractionCondition.InteractionMoment.AfterInteraction)
                    {
                        return;
                    }

                    interactionInProgress = false;

                    if (allowContinue)
                    {
                        StartMainInteraction(interactor, onCompleted);
                    }
                    else
                    {
                        interactionInProgress = true;
                        suppressAfterContextOnCompletion = true;
                        completionCallback = onCompleted;
                        CompleteInteraction(interactor);
                    }
                });

                if (!handled)
                {
                    if (moment == ContextInteractionCondition.InteractionMoment.BeforeInteraction)
                    {
                        interactionInProgress = false;
                    }

                    continue;
                }

                return true;
            }

            return false;
        }

        private ContextInteractionCondition[] GetContextConditions()
        {
            if (contextInteractionConditions != null && contextInteractionConditions.Length > 0)
            {
                return contextInteractionConditions;
            }

            return GetComponents<ContextInteractionCondition>();
        }

        protected void CompleteInteraction(GameObject interactor)
        {
            CompleteInteraction(interactor, true);
        }

        protected void CompleteInteraction(GameObject interactor, bool runAfterContext)
        {
            if (!interactionInProgress) return;

            interactionInProgress = false;

            var callback = completionCallback;
            completionCallback = null;

            InteractionCompleted?.Invoke(interactor);
            callback?.Invoke();

            if (runAfterContext && !suppressAfterContextOnCompletion)
            {
                TryHandleContextConditions(ContextInteractionCondition.InteractionMoment.AfterInteraction, interactor, null);
            }

            suppressAfterContextOnCompletion = false;
        }

        protected bool IsInteractionInProgress => interactionInProgress;

        protected abstract void OnInteract(GameObject interactor);
    }
}