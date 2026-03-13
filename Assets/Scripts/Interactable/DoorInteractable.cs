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

        private Quaternion closedRot;
        private Quaternion openRot;
        private bool isMoving;

        private void Awake()
        {
            if (doorPivot == null)
                doorPivot = transform;

            closedRot = doorPivot.localRotation;
            openRot = closedRot * Quaternion.Euler(0f, 0f, openDegrees);
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (isMoving) return;

            isOpen = !isOpen;
            StopAllCoroutines();
            StartCoroutine(RotateTo(isOpen ? openRot : closedRot));
        }

        private System.Collections.IEnumerator RotateTo(Quaternion target)
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
        }
    }
}