using UnityEngine;

namespace Interactable
{
    public class DoorInteractable : Interactable
    {
        [Header("Door")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openDegrees = 90f;
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private bool isOpen = false;

        [Header("Interaction")]
        [Tooltip("If enabled, direct interaction from the player is ignored. Scripted interactions from controllers still work.")]
        [SerializeField] private bool blockPlayerInteraction = false;

        private Quaternion closedRot;
        private Quaternion openRot;
        private bool isMoving;

        public bool IsOpen => isOpen;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (doorPivot == null)
                doorPivot = transform;

            closedRot = doorPivot.localRotation;
            openRot = closedRot * Quaternion.Euler(0f, 0f, openDegrees);
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (blockPlayerInteraction && IsPlayerInteractor(interactor))
            {
                CompleteInteraction(interactor);
                return;
            }

            if (isMoving)
            {
                CompleteInteraction(interactor);
                return;
            }

            isOpen = !isOpen;
            StopAllCoroutines();
            StartCoroutine(RotateTo(isOpen ? openRot : closedRot, interactor));
        }

        private static bool IsPlayerInteractor(GameObject interactor)
        {
            if (interactor == null)
                return false;

            return interactor.GetComponent<PlayerController>() != null
                   || interactor.GetComponent<Player.PlayerHandsController>() != null;
        }

        private System.Collections.IEnumerator RotateTo(Quaternion target, GameObject interactor)
        {
            isMoving = true;

            while (Quaternion.Angle(doorPivot.localRotation, target) > 0.2f)
            {
                doorPivot.localRotation = Quaternion.RotateTowards(
                    doorPivot.localRotation,
                    target,
                    rotateSpeed * Time.deltaTime
                );
                yield return null;
            }

            doorPivot.localRotation = target;
            isMoving = false;
            CompleteInteraction(interactor);
        }
    }
}