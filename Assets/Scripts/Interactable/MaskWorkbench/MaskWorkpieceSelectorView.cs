using UnityEngine;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Настраиваемый селектор. Ничего не создаёт сам: только двигает уже собранный руками визуал.
    /// </summary>
    public class MaskWorkpieceSelectorView : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool copyTargetRotation;
        [SerializeField] private bool copyTargetScale;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform;
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

            visualRoot.gameObject.SetActive(true);
            visualRoot.SetParent(target, false);
            visualRoot.localPosition = localOffset;

            if (copyTargetRotation)
                visualRoot.localRotation = Quaternion.identity;

            if (copyTargetScale)
                visualRoot.localScale = Vector3.one;
        }

        public void Hide()
        {
            if (visualRoot == null)
                visualRoot = transform;

            visualRoot.gameObject.SetActive(false);
        }
    }
}
