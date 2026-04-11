using UnityEngine;
using UnityEngine.Events;

namespace Interactable.Workbench
{
    public class WorkbenchFocusTarget : MonoBehaviour
    {
        public enum FocusKind
        {
            Recipe = 0,
            Drawer = 1,
            Custom = 2
        }

        [Header("Identity")]
        [SerializeField] private FocusKind focusKind = FocusKind.Custom;
        [SerializeField] private string targetId;

        [Header("Locked View")]
        [SerializeField] private Transform lockedView;

        [Header("Events")]
        [SerializeField] private UnityEvent onFocusEntered;
        [SerializeField] private UnityEvent onFocusExited;

        public FocusKind Kind => focusKind;
        public string TargetId => targetId;
        public Transform LockedView => lockedView;

        public bool IsAvailableFor(WorkbenchInteractableBase workbench)
        {
            return workbench != null;
        }

        public void NotifyFocusEntered()
        {
            onFocusEntered?.Invoke();
        }

        public void NotifyFocusExited()
        {
            onFocusExited?.Invoke();
        }
    }
}
