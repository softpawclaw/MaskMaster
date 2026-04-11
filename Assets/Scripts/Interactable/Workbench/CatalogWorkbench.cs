using Player;
using UnityEngine;
using UnityEngine.Events;

namespace Interactable.Workbench
{
    public class CatalogWorkbench : WorkbenchInteractableBase
    {
        private enum CatalogState
        {
            Overview = 0,
            RecipeInspect = 1,
            DrawerInspect = 2
        }

        [System.Serializable]
        private class DrawerRuntimeEvent
        {
            public string DrawerId;
            public UnityEvent OnOpened;
            public UnityEvent OnClosed;
        }

        [Header("Catalog State Events")]
        [SerializeField] private UnityEvent onOverviewEntered;
        [SerializeField] private UnityEvent onRecipeInspectEntered;
        [SerializeField] private UnityEvent onRecipeInspectExited;
        [SerializeField] private UnityEvent onDrawerInspectEntered;
        [SerializeField] private UnityEvent onDrawerInspectExited;
        [SerializeField] private DrawerRuntimeEvent[] drawerEvents;

        private CatalogState catalogState = CatalogState.Overview;
        private WorkbenchFocusTarget activeTarget;

        public override void OnWorkbenchEntered(PlayerWorkbenchModeController controller)
        {
            base.OnWorkbenchEntered(controller);
            catalogState = CatalogState.Overview;
            activeTarget = null;
            onOverviewEntered?.Invoke();
        }

        public override void OnWorkbenchExited(PlayerWorkbenchModeController controller)
        {
            if (catalogState == CatalogState.RecipeInspect)
                onRecipeInspectExited?.Invoke();

            if (catalogState == CatalogState.DrawerInspect)
            {
                onDrawerInspectExited?.Invoke();
                InvokeDrawerClosed(activeTarget?.TargetId);
            }

            activeTarget = null;
            catalogState = CatalogState.Overview;
            base.OnWorkbenchExited(controller);
        }

        public override void HandleFocusInteract(WorkbenchFocusTarget target)
        {
            if (target == null) return;

            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;
            if (!controller.IsInOverview) return;
            if (target.LockedView == null)
            {
                Debug.LogWarning($"{name}: Focus target '{target.name}' has no locked view assigned.");
                return;
            }

            activeTarget = target;

            switch (target.Kind)
            {
                case WorkbenchFocusTarget.FocusKind.Recipe:
                    catalogState = CatalogState.RecipeInspect;
                    onRecipeInspectEntered?.Invoke();
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    break;

                case WorkbenchFocusTarget.FocusKind.Drawer:
                    catalogState = CatalogState.DrawerInspect;
                    onDrawerInspectEntered?.Invoke();
                    InvokeDrawerOpened(target.TargetId);
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    break;

                default:
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    break;
            }
        }

        public override void HandleCancelFromLockedView()
        {
            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;

            if (catalogState == CatalogState.RecipeInspect)
                onRecipeInspectExited?.Invoke();

            if (catalogState == CatalogState.DrawerInspect)
            {
                onDrawerInspectExited?.Invoke();
                InvokeDrawerClosed(activeTarget?.TargetId);
            }

            catalogState = CatalogState.Overview;
            activeTarget = null;
            onOverviewEntered?.Invoke();
            controller.ReturnToOverview(LockedViewDuration);
        }

        private void InvokeDrawerOpened(string drawerId)
        {
            if (string.IsNullOrWhiteSpace(drawerId)) return;

            for (int i = 0; i < drawerEvents.Length; i++)
            {
                if (drawerEvents[i] == null || drawerEvents[i].DrawerId != drawerId)
                    continue;

                drawerEvents[i].OnOpened?.Invoke();
                return;
            }
        }

        private void InvokeDrawerClosed(string drawerId)
        {
            if (string.IsNullOrWhiteSpace(drawerId)) return;

            for (int i = 0; i < drawerEvents.Length; i++)
            {
                if (drawerEvents[i] == null || drawerEvents[i].DrawerId != drawerId)
                    continue;

                drawerEvents[i].OnClosed?.Invoke();
                return;
            }
        }
    }
}
