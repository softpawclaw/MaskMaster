using UnityEngine;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Настраиваемый селектор. Ничего не создаёт сам и не меняет свой размер:
    /// только переносит уже собранный руками визуал к нужному якорю.
    /// </summary>
    public class MaskWorkpieceSelectorView : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool copyTargetRotation;

        private Transform homeParent;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform;

            homeParent = visualRoot.parent;
        }

        public void ShowAt(Transform target)
        {
            if (visualRoot == null)
                visualRoot = transform;

            if (target == null)
            {
                Hide();
                return;
            }

            // Важно: селектор больше НЕ переподчиняется маске/болванке.
            // Он остаётся жить на уровне стола, поэтому не удаляется вместе с первой маской
            // и не наследует кривой/нулевой scale от runtime-объектов.
            if (homeParent != null && visualRoot.parent != homeParent)
                visualRoot.SetParent(homeParent, true);

            visualRoot.gameObject.SetActive(true);
            visualRoot.position = target.TransformPoint(localOffset);

            if (copyTargetRotation)
                visualRoot.rotation = target.rotation;
        }

        public void Hide()
        {
            if (visualRoot == null)
                visualRoot = transform;

            if (homeParent != null && visualRoot.parent != homeParent)
                visualRoot.SetParent(homeParent, true);

            visualRoot.gameObject.SetActive(false);
        }
    }
}
