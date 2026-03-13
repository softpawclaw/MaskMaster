using System;
using UnityEngine;

namespace Interactable
{
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private bool oneShot = false;
        [SerializeField] private bool autoComplete = false;

        protected bool used;
        private bool interactionInProgress;
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

            used = true;
            interactionInProgress = true;
            completionCallback = onCompleted;

            OnInteract(interactor);

            if (autoComplete)
            {
                CompleteInteraction(interactor);
            }
        }

        protected void CompleteInteraction(GameObject interactor)
        {
            if (!interactionInProgress) return;

            interactionInProgress = false;

            var callback = completionCallback;
            completionCallback = null;

            InteractionCompleted?.Invoke(interactor);
            callback?.Invoke();
        }

        protected bool IsInteractionInProgress => interactionInProgress;

        protected abstract void OnInteract(GameObject interactor);
    }
}