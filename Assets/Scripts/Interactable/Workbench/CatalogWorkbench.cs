using System.Collections.Generic;
using Global;
using Items;
using Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

        private enum StackSide
        {
            Right = 0,
            Left = 1
        }

        [System.Serializable]
        private class DrawerRuntimeEvent
        {
            public string DrawerId;
            public UnityEvent OnOpened;
            public UnityEvent OnClosed;
        }

        [System.Serializable]
        private class DrawerConfig
        {
            public string DrawerId;
            public CatalogPageItem.CatalogPageKind PageKind;
        }

        [Header("Catalog State Events")]
        [SerializeField] private UnityEvent onOverviewEntered;
        [SerializeField] private UnityEvent onRecipeInspectEntered;
        [SerializeField] private UnityEvent onRecipeInspectExited;
        [SerializeField] private UnityEvent onDrawerInspectEntered;
        [SerializeField] private UnityEvent onDrawerInspectExited;
        [SerializeField] private UnityEvent onRightStackActivated;
        [SerializeField] private UnityEvent onLeftStackActivated;
        [SerializeField] private DrawerRuntimeEvent[] drawerEvents;

        [Header("Runtime Holders")]
        [SerializeField] private MainRecipeDisplaySlot mainRecipeSlot;
        [SerializeField] private ComplexItemPlaceHolder selectedPagesHolder;
        [SerializeField] private ComplexItemPlaceHolder activeDrawerHolder;

        [Header("Drawers")]
        [SerializeField] private DrawerConfig[] drawerConfigs;

        [Header("Input Maps")]
        [SerializeField] private string recipeFocusActionMap = "CatalogRecipe";
        [SerializeField] private string stacksFocusActionMap = "CatalogStacks";

        [Header("Input Actions")]
        [SerializeField] private string toggleActiveStackActionName = "ToggleActiveStack";
        [SerializeField] private string nextPageActionName = "NextPage";
        [SerializeField] private string previousPageActionName = "PreviousPage";
        [SerializeField] private string transferPageActionName = "TransferPage";

        [Header("Legacy Input Fallbacks")]
        [SerializeField] private string[] toggleActiveStackFallbackActionNames = { "SelectPlace" };
        [SerializeField] private string[] nextPageFallbackActionNames = { "Next", "FlipPage" };
        [SerializeField] private string[] previousPageFallbackActionNames = { "Previous" };
        [SerializeField] private string[] transferPageFallbackActionNames = { "Interact" };

        private CatalogState catalogState = CatalogState.Overview;
        private StackSide activeStackSide = StackSide.Right;
        private WorkbenchFocusTarget activeTarget;

        private ItemsFactory itemsFactory;
        private PlayerHandsController handsController;
        private PlayerInput playerInput;

        private InputAction toggleActiveStackAction;
        private InputAction nextPageAction;
        private InputAction previousPageAction;
        private InputAction transferPageAction;
        private bool inputSubscribed;

        private readonly Dictionary<string, List<CatalogPageItem>> drawerContents = new();
        private PaperStackItem selectedPagesStack;
        private PaperStackItem activeDrawerStack;
        private string activeDrawerId;

        private void OnEnable()
        {
            EnsureReferences();
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
        }

        private void OnDisable()
        {
            UnsubscribeInputActions();
        }

        public override void OnWorkbenchEntered(PlayerWorkbenchModeController controller)
        {
            EnsureReferences();
            EnsureCatalogDataBuilt();
            EnsureRuntimeStacks();
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            TryAbsorbPlayerPaperStack();

            base.OnWorkbenchEntered(controller);
            catalogState = CatalogState.Overview;
            activeTarget = null;
            SetActiveStack(StackSide.Right);
            controller?.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
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

            ReturnAllDrawerPagesFromActiveStack();
            ReturnPaperStackToPlayerIfNeeded();
            CleanupRuntimeStacks();

            activeTarget = null;
            activeDrawerId = null;
            catalogState = CatalogState.Overview;
            SetActiveStack(StackSide.Right);
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
                    mainRecipeSlot?.RefreshVisual();
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    break;

                case WorkbenchFocusTarget.FocusKind.Drawer:
                    catalogState = CatalogState.DrawerInspect;
                    onDrawerInspectEntered?.Invoke();
                    OpenDrawer(target.TargetId);
                    SetActiveStack(StackSide.Right);
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
            controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            onOverviewEntered?.Invoke();
            controller.ReturnToOverview(LockedViewDuration);
        }

        public override void OnLockedViewEntered(WorkbenchFocusTarget target)
        {
            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;

            switch (catalogState)
            {
                case CatalogState.RecipeInspect:
                    controller.SwitchWorkbenchActionMap(recipeFocusActionMap);
                    break;
                case CatalogState.DrawerInspect:
                    controller.SwitchWorkbenchActionMap(stacksFocusActionMap);
                    break;
                default:
                    controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
                    break;
            }

            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
        }

        public void SetActiveStackRight()
        {
            SetActiveStack(StackSide.Right);
        }

        public void SetActiveStackLeft()
        {
            SetActiveStack(StackSide.Left);
        }

        public void ToggleActiveStack()
        {
            SetActiveStack(activeStackSide == StackSide.Right ? StackSide.Left : StackSide.Right);
        }

        public void TransferSelectedPageFromActiveStack()
        {
            if (catalogState != CatalogState.DrawerInspect)
                return;

            if (activeStackSide == StackSide.Right)
            {
                MoveSelectedPageRightToLeft();
                return;
            }

            MoveSelectedPageLeftToRight();
        }

        public void SelectNextActiveStackPage()
        {
            if (catalogState != CatalogState.DrawerInspect)
                return;

            if (activeStackSide == StackSide.Right)
            {
                activeDrawerStack?.SelectNext();
                return;
            }

            selectedPagesStack?.SelectNext();
        }

        public void SelectPreviousActiveStackPage()
        {
            if (catalogState != CatalogState.DrawerInspect)
                return;

            if (activeStackSide == StackSide.Right)
            {
                activeDrawerStack?.SelectPrevious();
                return;
            }

            selectedPagesStack?.SelectPrevious();
        }

        public void MoveSelectedPageRightToLeft()
        {
            if (activeDrawerStack == null || selectedPagesStack == null) return;
            var page = activeDrawerStack.TryRemoveSelected() as CatalogPageItem;
            if (page == null) return;
            selectedPagesStack.TryAddPage(page);
        }

        public void MoveSelectedPageLeftToRight()
        {
            if (selectedPagesStack == null) return;
            var page = selectedPagesStack.TryRemoveSelected() as CatalogPageItem;
            if (page == null) return;

            if (!string.IsNullOrWhiteSpace(activeDrawerId) && page.SourceDrawerId == activeDrawerId && activeDrawerStack != null)
            {
                activeDrawerStack.TryAddPage(page);
                return;
            }

            AddPageBackToSourceDrawer(page);
        }

        public void SelectNextRightStackPage() => activeDrawerStack?.SelectNext();
        public void SelectNextLeftStackPage() => selectedPagesStack?.SelectNext();

        public void ResetCatalogState()
        {
            ReturnAllDrawerPagesFromActiveStack();
            ReturnAllSelectedPagesToSourceDrawers();

            if (mainRecipeSlot != null)
            {
                mainRecipeSlot.ClearAndDestroy();
            }

            CleanupRuntimeStacks();
            RebuildDrawerContentsFromDatabases();
            EnsureRuntimeStacks();
            activeDrawerId = null;
            SetActiveStack(StackSide.Right);
        }

        private void EnsureReferences()
        {
            var linker = Linker.Instance;
            if (linker == null) return;

            if (itemsFactory == null)
                itemsFactory = linker.ItemsFactory;

            if (handsController == null)
                handsController = linker.PlayerHandsController;

            if (playerInput == null)
                playerInput = linker.PlayerInput;
        }

        private void RefreshInputActionsForCurrentMap()
        {
            UnsubscribeInputActions();

            toggleActiveStackAction = null;
            nextPageAction = null;
            previousPageAction = null;
            transferPageAction = null;

            if (playerInput == null)
                return;

            InputActionMap currentMap = playerInput.currentActionMap;
            if (currentMap == null)
                return;

            toggleActiveStackAction = FindActionWithFallbacks(currentMap, toggleActiveStackActionName, toggleActiveStackFallbackActionNames);
            nextPageAction = FindActionWithFallbacks(currentMap, nextPageActionName, nextPageFallbackActionNames);
            previousPageAction = FindActionWithFallbacks(currentMap, previousPageActionName, previousPageFallbackActionNames);
            transferPageAction = FindActionWithFallbacks(currentMap, transferPageActionName, transferPageFallbackActionNames);
        }

        private static InputAction FindAction(InputActionMap map, string actionName)
        {
            if (map == null || string.IsNullOrWhiteSpace(actionName))
                return null;

            return map.FindAction(actionName, false);
        }

        private static InputAction FindActionWithFallbacks(InputActionMap map, string primaryActionName, string[] fallbackActionNames)
        {
            InputAction action = FindAction(map, primaryActionName);
            if (action != null)
                return action;

            if (fallbackActionNames == null)
                return null;

            for (int i = 0; i < fallbackActionNames.Length; i++)
            {
                action = FindAction(map, fallbackActionNames[i]);
                if (action != null)
                    return action;
            }

            return null;
        }

        private void SubscribeInputActions()
        {
            if (inputSubscribed)
                return;

            if (toggleActiveStackAction != null)
                toggleActiveStackAction.performed += OnToggleActiveStackPerformed;
            if (nextPageAction != null)
                nextPageAction.performed += OnNextPagePerformed;
            if (previousPageAction != null)
                previousPageAction.performed += OnPreviousPagePerformed;
            if (transferPageAction != null)
                transferPageAction.performed += OnTransferPagePerformed;

            inputSubscribed = true;
        }

        private void UnsubscribeInputActions()
        {
            if (!inputSubscribed)
                return;

            if (toggleActiveStackAction != null)
                toggleActiveStackAction.performed -= OnToggleActiveStackPerformed;
            if (nextPageAction != null)
                nextPageAction.performed -= OnNextPagePerformed;
            if (previousPageAction != null)
                previousPageAction.performed -= OnPreviousPagePerformed;
            if (transferPageAction != null)
                transferPageAction.performed -= OnTransferPagePerformed;

            inputSubscribed = false;
        }

        private void OnToggleActiveStackPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!IsStacksInputActive()) return;
            ToggleActiveStack();
        }

        private void OnNextPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!IsStacksInputActive()) return;
            SelectNextActiveStackPage();
        }

        private void OnPreviousPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!IsStacksInputActive()) return;
            SelectPreviousActiveStackPage();
        }

        private void OnTransferPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!IsStacksInputActive()) return;
            TransferSelectedPageFromActiveStack();
        }

        private bool IsStacksInputActive()
        {
            var controller = FindController();
            if (controller == null) return false;
            if (controller.ActiveWorkbench != this) return false;
            return catalogState == CatalogState.DrawerInspect;
        }

        private void EnsureCatalogDataBuilt()
        {
            if (drawerContents.Count > 0)
                return;

            RebuildDrawerContentsFromDatabases();
        }

        private void EnsureRuntimeStacks()
        {
            if (itemsFactory == null)
                return;

            if (selectedPagesStack == null)
            {
                selectedPagesStack = itemsFactory.CreatePaperStack();
                if (selectedPagesHolder != null)
                    selectedPagesHolder.AttachExternalContainer(selectedPagesStack);
            }

            if (activeDrawerStack == null)
            {
                activeDrawerStack = itemsFactory.CreatePaperStack();
                if (activeDrawerHolder != null)
                    activeDrawerHolder.AttachExternalContainer(activeDrawerStack);
            }
        }

        private void CleanupRuntimeStacks()
        {
            if (selectedPagesHolder != null)
                selectedPagesHolder.DetachCurrentContainer();
            if (activeDrawerHolder != null)
                activeDrawerHolder.DetachCurrentContainer();

            if (selectedPagesStack != null)
                Object.Destroy(selectedPagesStack.gameObject);
            if (activeDrawerStack != null)
                Object.Destroy(activeDrawerStack.gameObject);

            selectedPagesStack = null;
            activeDrawerStack = null;
        }

        private void TryAbsorbPlayerPaperStack()
        {
            if (handsController == null)
                return;

            var handItem = handsController.GetItem(Enums.HandType.Right);
            if (handItem is not PaperStackItem inputStack)
                return;

            handsController.FreeItem(inputStack);
            var extracted = inputStack.ExtractAllItems();
            Object.Destroy(inputStack.gameObject);

            for (int i = 0; i < extracted.Count; i++)
            {
                var item = extracted[i];
                if (item == null)
                    continue;

                item.gameObject.SetActive(true);

                if (item is MainRecipeItem recipe)
                {
                    if (mainRecipeSlot != null && !mainRecipeSlot.HasRecipe)
                    {
                        mainRecipeSlot.TryAttach(recipe);
                    }
                    else
                    {
                        selectedPagesStack?.TryAddPage(recipe);
                    }

                    continue;
                }

                if (item is CatalogPageItem page)
                {
                    selectedPagesStack?.TryAddPage(page);
                    continue;
                }

                selectedPagesStack?.TryAddPage(item);
            }
        }

        private void OpenDrawer(string drawerId)
        {
            EnsureRuntimeStacks();
            ReturnAllDrawerPagesFromActiveStack();

            activeDrawerId = drawerId;

            if (string.IsNullOrWhiteSpace(drawerId))
                return;

            if (!drawerContents.TryGetValue(drawerId, out var pages) || pages == null || pages.Count == 0)
                return;

            for (int i = pages.Count - 1; i >= 0; i--)
            {
                var page = pages[i];
                if (page == null)
                    continue;

                activeDrawerStack.TryAddPage(page);
            }

            pages.Clear();
        }

        private void ReturnAllDrawerPagesFromActiveStack()
        {
            if (activeDrawerStack == null)
                return;

            var extracted = activeDrawerStack.ExtractAllItems();
            for (int i = 0; i < extracted.Count; i++)
            {
                if (extracted[i] is CatalogPageItem page)
                {
                    AddPageBackToSourceDrawer(page);
                }
            }
        }

        private void ReturnAllSelectedPagesToSourceDrawers()
        {
            if (selectedPagesStack == null)
                return;

            var extracted = selectedPagesStack.ExtractAllItems();
            for (int i = 0; i < extracted.Count; i++)
            {
                if (extracted[i] is CatalogPageItem page)
                {
                    AddPageBackToSourceDrawer(page);
                }
                else if (extracted[i] != null)
                {
                    Object.Destroy(extracted[i].gameObject);
                }
            }
        }

        private void ReturnPaperStackToPlayerIfNeeded()
        {
            if (handsController == null || itemsFactory == null)
                return;

            var outputStack = itemsFactory.CreatePaperStack();

            var recipe = mainRecipeSlot != null ? mainRecipeSlot.Detach() : null;
            if (recipe != null)
            {
                outputStack.TryAdd(recipe);
            }

            if (selectedPagesStack != null)
            {
                var extracted = selectedPagesStack.ExtractAllItems();
                for (int i = 0; i < extracted.Count; i++)
                {
                    var item = extracted[i];
                    if (item == null)
                        continue;

                    outputStack.TryAdd(item);
                }
            }

            if (outputStack.Count == 0)
            {
                Object.Destroy(outputStack.gameObject);
                return;
            }

            if (!handsController.GiveItem(outputStack))
            {
                Debug.LogWarning("CatalogWorkbench: failed to return paper stack to player hands.");
                Object.Destroy(outputStack.gameObject);
            }
        }

        private void AddPageBackToSourceDrawer(CatalogPageItem page)
        {
            if (page == null) return;
            if (string.IsNullOrWhiteSpace(page.SourceDrawerId))
            {
                Object.Destroy(page.gameObject);
                return;
            }

            if (!drawerContents.TryGetValue(page.SourceDrawerId, out var pages))
            {
                pages = new List<CatalogPageItem>();
                drawerContents[page.SourceDrawerId] = pages;
            }

            pages.Add(page);
        }

        private void RebuildDrawerContentsFromDatabases()
        {
            drawerContents.Clear();
            EnsureReferences();

            var linker = Linker.Instance;
            if (itemsFactory == null || linker == null)
                return;

            for (int i = 0; i < drawerConfigs.Length; i++)
            {
                var cfg = drawerConfigs[i];
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.DrawerId))
                    continue;

                var pages = new List<CatalogPageItem>();
                drawerContents[cfg.DrawerId] = pages;

                switch (cfg.PageKind)
                {
                    case CatalogPageItem.CatalogPageKind.MistResistance:
                        if (linker.DBMistResistance != null)
                        {
                            var data = linker.DBMistResistance.GetAll();
                            for (int j = 0; j < data.Length; j++)
                            {
                                pages.Add(itemsFactory.CreateCatalogPage(cfg.PageKind, data[j].Id, cfg.DrawerId, data[j].ResourceType));
                            }
                        }
                        break;

                    case CatalogPageItem.CatalogPageKind.FaceCover:
                        if (linker.DBFaceCover != null)
                        {
                            var data = linker.DBFaceCover.GetAll();
                            for (int j = 0; j < data.Length; j++)
                            {
                                pages.Add(itemsFactory.CreateCatalogPage(cfg.PageKind, data[j].Id, cfg.DrawerId));
                            }
                        }
                        break;

                    case CatalogPageItem.CatalogPageKind.District:
                        if (linker.DBDistrict != null)
                        {
                            var data = linker.DBDistrict.GetAll();
                            for (int j = 0; j < data.Length; j++)
                            {
                                pages.Add(itemsFactory.CreateCatalogPage(cfg.PageKind, data[j].Id, cfg.DrawerId));
                            }
                        }
                        break;

                    case CatalogPageItem.CatalogPageKind.Faction:
                        if (linker.DBFaction != null)
                        {
                            var data = linker.DBFaction.GetAll();
                            for (int j = 0; j < data.Length; j++)
                            {
                                pages.Add(itemsFactory.CreateCatalogPage(cfg.PageKind, data[j].Id, cfg.DrawerId));
                            }
                        }
                        break;
                }
            }
        }

        private void SetActiveStack(StackSide side)
        {
            activeStackSide = side;
            if (side == StackSide.Right)
            {
                onRightStackActivated?.Invoke();
                return;
            }

            onLeftStackActivated?.Invoke();
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
