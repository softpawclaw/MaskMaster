using Player;
using UnityEngine;
using UnityEngine.Events;

namespace Interactable.Workbench
{
    public abstract class WorkbenchInteractableBase : Interactable
    {
        [Header("Workbench Links")]
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private Transform overviewView;

        [Header("Transition")]
        [SerializeField] private float enterDuration = 0.3f;
        [SerializeField] private float exitDuration = 0.25f;
        [SerializeField] private float lockedViewDuration = 0.2f;
        [SerializeField] private AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve lockedViewCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Overview Look Limits")]
        [SerializeField] private float leftYawLimit = 30f;
        [SerializeField] private float rightYawLimit = 30f;
        [SerializeField] private float upPitchLimit = 20f;
        [SerializeField] private float downPitchLimit = 20f;
        [SerializeField] private float lookSensitivity = 0.08f;

        [Header("Events")]
        [SerializeField] private UnityEvent onWorkbenchEntered;
        [SerializeField] private UnityEvent onWorkbenchExitStarted;
        [SerializeField] private UnityEvent onWorkbenchExited;

        public Transform PlayerAnchor => playerAnchor;
        public Transform OverviewView => overviewView;
        public float EnterDuration => enterDuration;
        public float ExitDuration => exitDuration;
        public float LockedViewDuration => lockedViewDuration;
        public AnimationCurve EnterCurve => enterCurve;
        public AnimationCurve ExitCurve => exitCurve;
        public AnimationCurve LockedViewCurve => lockedViewCurve;
        public float LeftYawLimit => leftYawLimit;
        public float RightYawLimit => rightYawLimit;
        public float UpPitchLimit => upPitchLimit;
        public float DownPitchLimit => downPitchLimit;
        public float LookSensitivity => lookSensitivity;

        protected override void OnInteract(GameObject interactor)
        {
            if (playerAnchor == null || overviewView == null)
            {
                Debug.LogError($"{name}: Workbench anchors are not configured.");
                CompleteInteraction(interactor);
                return;
            }

            PlayerWorkbenchModeController controller = interactor.GetComponent<PlayerWorkbenchModeController>();
            if (controller == null)
            {
                Debug.LogError($"{name}: PlayerWorkbenchModeController not found on interactor.");
                CompleteInteraction(interactor);
                return;
            }

            if (!controller.TryEnterWorkbench(this))
            {
                CompleteInteraction(interactor);
                return;
            }
        }

        public virtual void OnWorkbenchEntered(PlayerWorkbenchModeController controller)
        {
            onWorkbenchEntered?.Invoke();
            CompleteInteraction(controller.gameObject);
        }

        public virtual void OnWorkbenchExitStarted(PlayerWorkbenchModeController controller)
        {
            onWorkbenchExitStarted?.Invoke();
        }

        public virtual void OnWorkbenchExited(PlayerWorkbenchModeController controller)
        {
            onWorkbenchExited?.Invoke();
        }

        public virtual void HandleCancelFromOverview()
        {
            var controller = FindController();
            if (controller == null) return;
            controller.ExitWorkbench(exitDuration);
        }

        public virtual void HandleCancelFromLockedView()
        {
            var controller = FindController();
            if (controller == null) return;
            controller.ReturnToOverview(lockedViewDuration);
        }

        public abstract void HandleFocusInteract(WorkbenchFocusTarget target);

        public virtual void OnLockedViewEnterStarted(WorkbenchFocusTarget target) { }
        public virtual void OnLockedViewEntered(WorkbenchFocusTarget target) { }
        public virtual void OnLockedViewExitStarted(WorkbenchFocusTarget target) { }
        public virtual void OnLockedViewExited(WorkbenchFocusTarget target) { }

        protected PlayerWorkbenchModeController FindController()
        {
            return FindFirstObjectByType<PlayerWorkbenchModeController>();
        }
    }
}
