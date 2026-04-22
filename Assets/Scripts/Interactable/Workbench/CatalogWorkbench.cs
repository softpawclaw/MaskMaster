using System.Collections.Generic;
using DB;
using Global;
using Items;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

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

        [Header("Runtime Holders")]
        [SerializeField] private MainRecipeDisplaySlot mainRecipeSlot;
        [SerializeField] private ComplexItemPlaceHolder selectedPagesHolder;
        [SerializeField] private ComplexItemPlaceHolder activeDrawerHolder;

        [Header("Catalog Drawers")]
        [SerializeField] private CatalogDrawer[] drawers;

        [Header("Input Maps")]
        [SerializeField] private string recipeFocusActionMap = "CatalogRecipe";
        [SerializeField] private string stacksFocusActionMap = "CatalogStacks";

        [Header("Input Actions")]
        [SerializeField] private string toggleActiveStackActionName = "ToggleActiveStack";
        [SerializeField] private string nextPageActionName = "NextPage";
        [SerializeField] private string previousPageActionName = "PreviousPage";
        [SerializeField] private string transferPageActionName = "TransferPage";

        [Header("Stack Highlights")]
        [SerializeField] private GameObject rightStackHighlight;
        [SerializeField] private GameObject leftStackHighlight;
        
        private CatalogState catalogState = CatalogState.Overview;
        private StackSide activeStackSide = StackSide.Right;
        private WorkbenchFocusTarget activeTarget;

        private ItemsFactory itemsFactory;
        private PlayerHandsController handsController;
        private PlayerInput playerInput;
        private DBCatalogPage catalogPageDatabase;

        private InputAction toggleActiveStackAction;
        private InputAction nextPageAction;
        private InputAction previousPageAction;
        private InputAction transferPageAction;
        private bool inputSubscribed;

        private readonly Dictionary<string, CatalogDrawer> drawerMap = new();
        private PaperStackItem selectedPagesStack;
        private PaperStackItem activeDrawerStack;
        private CatalogDrawer activeDrawer;

        private void Awake()
        {
            RefreshStackHighlights();
        }

        private void OnEnable()
        {
            EnsureReferences();
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            RefreshStackHighlights();
        }

        private void OnDisable()
        {
            UnsubscribeInputActions();
        }

        public override void OnWorkbenchEntered(PlayerWorkbenchModeController controller)
        {
            EnsureReferences();
            EnsureDrawersLinked();
            EnsureRuntimeStacks();
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            TryAbsorbPlayerPaperStack();

            base.OnWorkbenchEntered(controller);
            catalogState = CatalogState.Overview;
            activeTarget = null;
            activeDrawer = null;
            SetActiveStack(StackSide.Right);
            RefreshStackHighlights();
            controller?.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            RefreshStackViews();
        }

        public override void OnWorkbenchExited(PlayerWorkbenchModeController controller)
        {
            if (catalogState == CatalogState.DrawerInspect)
            {
                ClearActiveDrawerRuntimePages();
            }

            ReturnPaperStackToPlayerIfNeeded();
            CleanupRuntimeStacks();

            activeTarget = null;
            activeDrawer = null;
            catalogState = CatalogState.Overview;
            SetActiveStack(StackSide.Right);
            RefreshStackHighlights();
            base.OnWorkbenchExited(controller);
        }

        public override void HandleFocusInteract(WorkbenchFocusTarget target)
        {
            if (target == null) return;

            PlayerWorkbenchModeController controller = FindController();
            if (controller == null || !controller.IsInOverview) return;
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
                    mainRecipeSlot?.RefreshVisual();
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    break;

                case WorkbenchFocusTarget.FocusKind.Drawer:
                    catalogState = CatalogState.DrawerInspect;
                    OpenDrawer(target.TargetId);
                    SetActiveStack(StackSide.Right);
                    controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                    RefreshStackHighlights();
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

            if (catalogState == CatalogState.DrawerInspect)
            {
                ClearActiveDrawerRuntimePages();
            }

            catalogState = CatalogState.Overview;
            activeTarget = null;
            activeDrawer = null;

            RefreshStackHighlights();

            controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            RefreshStackViews();
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
            }
            else
            {
                selectedPagesStack?.SelectNext();
            }

            RefreshStackViews();
        }

        public void SelectPreviousActiveStackPage()
        {
            if (catalogState != CatalogState.DrawerInspect)
                return;

            if (activeStackSide == StackSide.Right)
            {
                activeDrawerStack?.SelectPrevious();
            }
            else
            {
                selectedPagesStack?.SelectPrevious();
            }

            RefreshStackViews();
        }

        public void MoveSelectedPageRightToLeft()
        {
            if (activeDrawer == null || activeDrawerStack == null || selectedPagesStack == null)
                return;

            CatalogPageItem page = activeDrawerStack.TryRemoveSelected() as CatalogPageItem;
            if (page == null)
                return;

            if (!selectedPagesStack.TryAddPageOnTop(page))
            {
                activeDrawerStack.TryAddPage(page);
                RefreshAfterMoveRightToLeft();
                return;
            }

            activeDrawer.RemovePage(page.PageId);
            RefreshAfterMoveRightToLeft();
        }

        public void MoveSelectedPageLeftToRight()
        {
            if (selectedPagesStack == null)
                return;

            CatalogPageItem page = selectedPagesStack.TryRemoveSelected() as CatalogPageItem;
            if (page == null)
                return;

            CatalogDrawer sourceDrawer = FindDrawerById(page.SourceDrawerId);
            bool returningToActiveDrawer = activeDrawer != null && sourceDrawer == activeDrawer;

            if (returningToActiveDrawer && activeDrawerStack != null)
            {
                if (activeDrawerStack.TryAddPageOnTop(page))
                {
                    activeDrawer.AddPage(page.PageId);
                }
                else
                {
                    selectedPagesStack.TryAddPageOnTop(page);
                }

                RefreshAfterMoveLeftToRight();
                return;
            }

            if (sourceDrawer != null)
            {
                sourceDrawer.AddPage(page.PageId);
            }

            Object.Destroy(page.gameObject);
            RefreshAfterMoveLeftToRight();
        }
        
        public void ResetCatalogState()
        {
            ClearActiveDrawerRuntimePages();
            ReturnAllSelectedPagesToSourceDrawers();

            if (mainRecipeSlot != null)
            {
                mainRecipeSlot.ClearAndDestroy();
            }

            for (int i = 0; i < drawers.Length; i++)
            {
                if (drawers[i] != null)
                    drawers[i].ResetToInitialState();
            }

            CleanupRuntimeStacks();
            EnsureRuntimeStacks();
            activeDrawer = null;
            activeTarget = null;
            catalogState = CatalogState.Overview;
            SetActiveStack(StackSide.Right);
            RefreshStackViews();
        }

        private void EnsureReferences()
        {
            Linker linker = Linker.Instance;
            if (linker == null) return;

            if (itemsFactory == null)
                itemsFactory = linker.ItemsFactory;

            if (handsController == null)
                handsController = linker.PlayerHandsController;

            if (playerInput == null)
                playerInput = linker.PlayerInput;

            if (catalogPageDatabase == null)
                catalogPageDatabase = linker.DBCatalogPage;
        }

        private void EnsureDrawersLinked()
        {
            EnsureReferences();

            if (drawers == null || drawers.Length == 0)
                drawers = GetComponentsInChildren<CatalogDrawer>(true);

            drawerMap.Clear();
            for (int i = 0; i < drawers.Length; i++)
            {
                CatalogDrawer drawer = drawers[i];
                if (drawer == null || string.IsNullOrWhiteSpace(drawer.DrawerId))
                    continue;

                drawer.Link(catalogPageDatabase);
                drawerMap[drawer.DrawerId] = drawer;
            }
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

            toggleActiveStackAction = FindAction(currentMap, toggleActiveStackActionName);
            nextPageAction = FindAction(currentMap, nextPageActionName);
            previousPageAction = FindAction(currentMap, previousPageActionName);
            transferPageAction = FindAction(currentMap, transferPageActionName);
        }

        private static InputAction FindAction(InputActionMap map, string actionName)
        {
            if (map == null || string.IsNullOrWhiteSpace(actionName))
                return null;

            return map.FindAction(actionName, false);
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
            if (!ctx.performed || !IsStacksInputActive()) return;
            ToggleActiveStack();
        }

        private void OnNextPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed || !IsStacksInputActive()) return;
            SelectNextActiveStackPage();
        }

        private void OnPreviousPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed || !IsStacksInputActive()) return;
            SelectPreviousActiveStackPage();
        }

        private void OnTransferPagePerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed || !IsStacksInputActive()) return;
            TransferSelectedPageFromActiveStack();
        }

        private bool IsStacksInputActive()
        {
            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return false;
            if (controller.ActiveWorkbench != this) return false;
            return catalogState == CatalogState.DrawerInspect;
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

            RefreshStackViews();
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

            ItemBase handItem = handsController.GetItem(Enums.HandType.Right);
            if (handItem is not PaperStackItem inputStack)
                return;

            handsController.FreeItem(inputStack);
            List<ItemBase> extracted = inputStack.ExtractAllItems();
            Object.Destroy(inputStack.gameObject);

            for (int i = 0; i < extracted.Count; i++)
            {
                ItemBase item = extracted[i];
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

                selectedPagesStack?.TryAddPage(item);
            }

            RefreshStackViews();
        }

        private void OpenDrawer(string drawerId)
        {
            EnsureRuntimeStacks();
            ClearActiveDrawerRuntimePages();

            activeDrawer = FindDrawerById(drawerId);
            if (activeDrawer == null)
                return;

            if (!activeDrawer.TryGetPageDataSnapshot(out CatalogPageData[] pages) || pages.Length == 0)
            {
                RefreshStackViews();
                return;
            }

            for (int i = 0; i < pages.Length; i++)
            {
                CatalogPageItem pageItem = itemsFactory.CreateCatalogPage(pages[i]);
                if (pageItem == null)
                    continue;

                activeDrawerStack.TryAddPage(pageItem);
            }

            RefreshStackViews();
        }

        private void ClearActiveDrawerRuntimePages()
        {
            if (activeDrawerStack == null)
            {
                activeDrawer = null;
                return;
            }

            List<ItemBase> extracted = activeDrawerStack.ExtractAllItems();
            for (int i = 0; i < extracted.Count; i++)
            {
                if (extracted[i] != null)
                    Object.Destroy(extracted[i].gameObject);
            }

            activeDrawer = null;
            RefreshStackViews();
        }

        private void ReturnAllSelectedPagesToSourceDrawers()
        {
            if (selectedPagesStack == null)
                return;

            List<ItemBase> extracted = selectedPagesStack.ExtractAllItems();
            for (int i = 0; i < extracted.Count; i++)
            {
                ItemBase item = extracted[i];
                if (item is CatalogPageItem page)
                {
                    CatalogDrawer sourceDrawer = FindDrawerById(page.SourceDrawerId);
                    sourceDrawer?.AddPage(page.PageId);
                    Object.Destroy(page.gameObject);
                }
                else if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            RefreshStackViews();
        }

        private void ReturnPaperStackToPlayerIfNeeded()
        {
            if (handsController == null || itemsFactory == null)
                return;

            MainRecipeItem recipe = mainRecipeSlot != null ? mainRecipeSlot.Detach() : null;

            List<ItemBase> selectedItems = null;
            if (selectedPagesStack != null)
            {
                selectedItems = selectedPagesStack.ExtractAllItems();
            }

            bool hasRecipe = recipe != null;
            bool hasSelectedItems = selectedItems != null && selectedItems.Count > 0;

            if (!hasRecipe && !hasSelectedItems)
                return;

            PaperStackItem outputStack = itemsFactory.CreatePaperStack();
            bool addedAnything = false;

            if (recipe != null)
            {
                recipe.gameObject.SetActive(true);

                if (outputStack.TryAddPage(recipe))
                {
                    addedAnything = true;
                }
                else
                {
                    Debug.LogWarning("CatalogWorkbench: failed to add main recipe to output paper stack.");
                    Object.Destroy(recipe.gameObject);
                }
            }

            if (selectedItems != null)
            {
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    ItemBase item = selectedItems[i];
                    if (item == null)
                        continue;

                    item.gameObject.SetActive(true);

                    if (outputStack.TryAddPage(item))
                    {
                        addedAnything = true;
                    }
                    else
                    {
                        Debug.LogWarning($"CatalogWorkbench: failed to add item '{item.name}' to output paper stack.");
                        Object.Destroy(item.gameObject);
                    }
                }
            }

            if (!addedAnything || outputStack.Count == 0)
            {
                Object.Destroy(outputStack.gameObject);
                return;
            }

            RefreshStackViews();

            if (!handsController.GiveItem(outputStack))
            {
                Debug.LogWarning("CatalogWorkbench: failed to return paper stack to player hands.");
                Object.Destroy(outputStack.gameObject);
            }
        }

        private CatalogDrawer FindDrawerById(string drawerId)
        {
            if (string.IsNullOrWhiteSpace(drawerId))
                return null;

            if (drawerMap.TryGetValue(drawerId, out CatalogDrawer drawer))
                return drawer;

            return null;
        }

        private void SetActiveStack(StackSide side)
        {
            activeStackSide = side;
            RefreshStackHighlights();
        }

        private void RefreshStackViews()
        {
            selectedPagesHolder?.RefreshCurrentContainerView();
            activeDrawerHolder?.RefreshCurrentContainerView();
        }

        private void RefreshAfterMoveRightToLeft()
        {
            activeDrawerHolder?.RefreshCurrentContainerView();
            selectedPagesHolder?.RefreshCurrentContainerView();
        }

        private void RefreshAfterMoveLeftToRight()
        {
            selectedPagesHolder?.RefreshCurrentContainerView();
            activeDrawerHolder?.RefreshCurrentContainerView();
        }
        
        private void RefreshStackHighlights()
        {
            if (rightStackHighlight != null)
                rightStackHighlight.SetActive(false);

            if (leftStackHighlight != null)
                leftStackHighlight.SetActive(false);

            if (catalogState != CatalogState.DrawerInspect)
                return;

            if (activeStackSide == StackSide.Right)
            {
                if (rightStackHighlight != null)
                    rightStackHighlight.SetActive(true);
            }
            else
            {
                if (leftStackHighlight != null)
                    leftStackHighlight.SetActive(true);
            }
        }
    }
}