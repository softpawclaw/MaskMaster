using System.Collections.Generic;
using DB;
using Enums;
using Helpers;
using Interactable.Workbench;
using Items;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Новый производственный верстак масок.
    /// MVP-патч: раскладывает рецепт/поднос, даёт прощёлкать основной pipeline
    /// и создаёт готовую маску после финального этапа.
    /// </summary>
    public class MaskWorkbench : WorkbenchInteractableBase
    {
        private const string CraftSurfaceTargetId = "CraftSurface";

        [Header("Runtime Holders")]
        [SerializeField] private MainRecipeDisplaySlot mainRecipeSlot;
        [SerializeField] private ComplexItemPlaceHolder recipePagesHolder;
        [SerializeField] private ComplexItemPlaceHolder trayHolder;

        [Header("Production Workpiece")]
        [SerializeField] private MaskWorkpiece workpiecePrefab;
        [SerializeField] private Transform workpieceSocket;
        [SerializeField] private Transform completedMaskSocket;
        [SerializeField] private Vector3 workpieceLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 workpieceLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 workpieceLocalScale = Vector3.one;

        [Header("Input Maps")]
        [SerializeField] private string craftSurfaceActionMap = "MaskCraftSurface";

        [Header("Input Actions")]
        [SerializeField] private string advanceProductionActionName = "Interact";
        [SerializeField] private string navigateProductionActionName = "Move";
        [SerializeField] private string selectProductionActionName = "Select";
        [SerializeField] private string scrollInlayActionName = "ScrollWheel";

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = true;

        private readonly MaskCraftSession session = new();

        private MaskWorkbenchState state = MaskWorkbenchState.Overview;
        private MaskWorkbenchState savedProductionState = MaskWorkbenchState.CraftSurfaceInspect;
        private WorkbenchFocusTarget activeTarget;

        private ItemsFactory itemsFactory;
        private PlayerHandsController handsController;
        private PlayerInput playerInput;

        private PaperStackItem runtimeRecipePagesStack;
        private TrayItem runtimeTray;
        private ResourceItem runtimeBlankResource;
        private MaskWorkpiece runtimeWorkpiece;
        private MaskItem runtimeCompletedMask;

        private readonly Queue<KeyValuePair<ResourceType, List<MaskWorkpiece.PlannedInlay>>> pendingInlayMiniGames = new();
        private KeyValuePair<ResourceType, List<MaskWorkpiece.PlannedInlay>> activeInlayMiniGameGroup;
        private MaskWorkbenchState stateBeforeMiniGame = MaskWorkbenchState.CraftSurfaceInspect;
        private int selectedTrayInlayIndex;

        private InputAction advanceProductionAction;
        private InputAction navigateProductionAction;
        private InputAction selectProductionAction;
        private InputAction scrollInlayAction;
        private bool inputSubscribed;

        public MaskWorkbenchState State => state;
        public bool HasStarted => session.HasStarted;
        public bool IsCompleted => session.IsCompleted;

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

            if (!session.HasStarted && !session.IsCompleted)
            {
                EnsureRuntimeRecipePagesStack();
                TryAbsorbPlayerLoadout();
                session.Init(mainRecipeSlot != null ? mainRecipeSlot.CurrentRecipe : null, runtimeRecipePagesStack, runtimeTray);
                savedProductionState = MaskWorkbenchState.CraftSurfaceInspect;
            }
            else
            {
                runtimeTray = trayHolder != null ? trayHolder.CurrentItem as TrayItem : runtimeTray;
                runtimeWorkpiece = session.Workpiece != null ? session.Workpiece : runtimeWorkpiece;
            }

            SetState(MaskWorkbenchState.Overview);
            activeTarget = null;

            base.OnWorkbenchEntered(controller);
            controller?.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
            RefreshRuntimeViews();
        }

        public override void OnWorkbenchExited(PlayerWorkbenchModeController controller)
        {
            if (!session.HasStarted)
            {
                ReturnUnstartedLoadoutToPlayer();
                CleanupEmptyRuntimeStack();
            }
            else if (session.IsCompleted)
            {
                if (GiveCompletedMaskToPlayer())
                {
                    CleanupCompletedSessionTrash();
                    session.ClearRuntimeLinks();
                    savedProductionState = MaskWorkbenchState.CraftSurfaceInspect;
                }
                else
                {
                    RefreshRuntimeViews();
                }
            }
            else
            {
                // Производство начато, но не завершено: всё остаётся лежать на верстаке.
                RefreshRuntimeViews();
            }

            activeTarget = null;
            SetState(MaskWorkbenchState.Overview);
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

            if (target.Kind == WorkbenchFocusTarget.FocusKind.Recipe)
            {
                SetState(MaskWorkbenchState.RecipeInspect);
                mainRecipeSlot?.RefreshVisual();
                controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                return;
            }

            if (target.Kind == WorkbenchFocusTarget.FocusKind.Custom && target.TargetId == CraftSurfaceTargetId)
            {
                if (session.IsCompleted)
                    SetState(MaskWorkbenchState.Completed);
                else if (session.HasStarted)
                    SetState(savedProductionState);
                else
                    SetState(MaskWorkbenchState.CraftSurfaceInspect);

                RefreshRuntimeViews();
                controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
                return;
            }

            controller.RequestLockedView(target, target.LockedView, LockedViewDuration);
        }

        public override void HandleCancelFromLockedView()
        {
            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;

            controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            base.HandleCancelFromLockedView();
        }

        public override void OnLockedViewEntered(WorkbenchFocusTarget target)
        {
            base.OnLockedViewEntered(target);

            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;

            if (target != null && target.Kind == WorkbenchFocusTarget.FocusKind.Custom && target.TargetId == CraftSurfaceTargetId)
            {
                controller.SwitchWorkbenchActionMap(craftSurfaceActionMap);
                RefreshInputActionsForCurrentMap();
                SubscribeInputActions();
            }
        }

        public override void OnLockedViewExited(WorkbenchFocusTarget target)
        {
            base.OnLockedViewExited(target);

            PlayerWorkbenchModeController controller = FindController();
            if (controller != null)
                controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);

            if (state == MaskWorkbenchState.RecipeInspect || state == MaskWorkbenchState.CraftSurfaceInspect)
                SetState(MaskWorkbenchState.Overview);

            activeTarget = null;
            RefreshInputActionsForCurrentMap();
            SubscribeInputActions();
        }

        /// <summary>
        /// Главная MVP-кнопка производственного пайплайна.
        /// Сейчас каждый вызов двигает процесс на один жизненный этап.
        /// </summary>
        public void AdvanceProduction()
        {
            if (state == MaskWorkbenchState.RecipeInspect || state == MaskWorkbenchState.Overview)
                return;

            switch (state)
            {
                case MaskWorkbenchState.CraftSurfaceInspect:
                    StartProduction();
                    break;

                case MaskWorkbenchState.SizeSelection:
                    ConfirmSizeSelection();
                    break;

                case MaskWorkbenchState.FormSelection:
                    ConfirmFormSelection();
                    break;

                case MaskWorkbenchState.InlaySelection:
                    ConfirmInlaySelection();
                    break;

                case MaskWorkbenchState.MiniGame:
                    CompleteMiniGameStub(true);
                    break;

                case MaskWorkbenchState.Completed:
                    Debug.Log($"{name}: mask production already completed.");
                    break;
            }
        }

        private void StartProduction()
        {
            EnsureRuntimeRecipePagesStack();

            if (session.MainRecipe == null)
            {
                Debug.LogWarning($"{name}: cannot start production without main recipe.");
                return;
            }

            ResourceItem blank = TakeFirstBlankFromTray();
            if (blank == null)
            {
                Debug.LogWarning($"{name}: cannot start production. Tray has no blank resource.");
                return;
            }

            runtimeBlankResource = blank;
            runtimeWorkpiece = CreateWorkpieceFromBlank(blank);
            session.MarkStarted(blank, runtimeWorkpiece);
            DestroyConsumedBlankResource();

            RemoveFirstRecipePageOfKind(CatalogPageKind.MistResistance);
            SetState(MaskWorkbenchState.SizeSelection);
            RefreshRuntimeViews();
        }

        private void ConfirmSizeSelection()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.FaceCover);
            RunMiniGameOrComplete(
                new MaskMiniGameRequest(MaskMiniGameKind.CutSegment, runtimeWorkpiece != null ? runtimeWorkpiece.SelectedSegment : MaskSegment.Middle),
                () =>
                {
                    runtimeWorkpiece?.ApplyMarkedCuts();
                    SetState(MaskWorkbenchState.FormSelection);
                    RefreshRuntimeViews();
                });
        }

        private void ConfirmFormSelection()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.District);
            RunMiniGameOrComplete(
                new MaskMiniGameRequest(MaskMiniGameKind.ShapeSegment, runtimeWorkpiece != null ? runtimeWorkpiece.SelectedSegment : MaskSegment.Middle),
                () =>
                {
                    runtimeWorkpiece?.SolidifySelectedShapes();
                    SetState(MaskWorkbenchState.InlaySelection);
                    RefreshRuntimeViews();
                });
        }

        private void ConfirmInlaySelection()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.Faction);
            BuildPendingInlayMiniGames();
            RunNextInlayMiniGameOrCompleteMask();
        }

        private void CompleteInlaySelectionAndMask()
        {

            if (itemsFactory == null || session.MainRecipe == null)
            {
                Debug.LogWarning($"{name}: cannot complete production. Missing ItemsFactory or main recipe.");
                return;
            }

            DBMask.MaskData targetMaskData = session.MainRecipe.MaskData;
            DBMask.MaskData actualMaskData = session.BuildActualMaskData();

            runtimeCompletedMask = itemsFactory.CreateMask(targetMaskData, actualMaskData);
            AttachCompletedMaskToTable(runtimeCompletedMask);
            DestroyRuntimeWorkpiece();
            session.MarkCompleted(runtimeCompletedMask);

            SetState(MaskWorkbenchState.Completed);
            RefreshRuntimeViews();
        }

        private void RunMiniGameOrComplete(MaskMiniGameRequest request, System.Action onComplete)
        {
            if (request.Kind == MaskMiniGameKind.None)
            {
                onComplete?.Invoke();
                return;
            }

            // MVP-заглушка: стейт MiniGame уже есть, но сама игра сразу считается успешной.
            stateBeforeMiniGame = state;
            SetState(MaskWorkbenchState.MiniGame);
            Debug.Log($"{name}: mini-game stub {request.Kind} ({request.Segment}, {request.ResourceType}, {request.Socket}) -> success.");
            onComplete?.Invoke();
        }

        private void CompleteMiniGameStub(bool success)
        {
            Debug.Log($"{name}: manual mini-game stub complete, success={success}, previous={stateBeforeMiniGame}.");
        }

        private void BuildPendingInlayMiniGames()
        {
            pendingInlayMiniGames.Clear();

            if (runtimeWorkpiece == null)
                return;

            Dictionary<ResourceType, List<MaskWorkpiece.PlannedInlay>> groups = runtimeWorkpiece.BuildPlannedInlayGroups();
            foreach (KeyValuePair<ResourceType, List<MaskWorkpiece.PlannedInlay>> group in groups)
            {
                pendingInlayMiniGames.Enqueue(group);
            }
        }

        private void RunNextInlayMiniGameOrCompleteMask()
        {
            if (pendingInlayMiniGames.Count <= 0)
            {
                CompleteInlaySelectionAndMask();
                return;
            }

            activeInlayMiniGameGroup = pendingInlayMiniGames.Dequeue();
            RunMiniGameOrComplete(
                new MaskMiniGameRequest(MaskMiniGameKind.InstallInlay, MaskSegment.Middle, activeInlayMiniGameGroup.Key),
                () =>
                {
                    runtimeWorkpiece?.ApplyInlayGroup(activeInlayMiniGameGroup.Key, activeInlayMiniGameGroup.Value);
                    RunNextInlayMiniGameOrCompleteMask();
                });
        }

        private void HandleProductionNavigation(Vector2 value)
        {
            if (!IsCraftSurfaceInputActive() || runtimeWorkpiece == null)
                return;

            if (value.sqrMagnitude < 0.2f)
                return;

            if (Mathf.Abs(value.y) >= Mathf.Abs(value.x))
            {
                if (value.y > 0f)
                    runtimeWorkpiece.SelectPreviousSegment();
                else
                    runtimeWorkpiece.SelectNextSegment();
                return;
            }

            switch (state)
            {
                case MaskWorkbenchState.SizeSelection:
                    // На этапе выбора размера A/D не размечает сегменты.
                    // Разметка/снятие разметки делается отдельным action Select.
                    break;

                case MaskWorkbenchState.FormSelection:
                    if (value.x > 0f)
                        runtimeWorkpiece.SelectNextShape();
                    else
                        runtimeWorkpiece.SelectPreviousShape();
                    break;

                case MaskWorkbenchState.InlaySelection:
                    if (value.x > 0f)
                        runtimeWorkpiece.SelectNextSocket();
                    else
                        runtimeWorkpiece.SelectPreviousSocket();
                    break;
            }
        }

        private void HandleInlayScroll(float scrollValue)
        {
            if (!IsCraftSurfaceInputActive() || state != MaskWorkbenchState.InlaySelection || runtimeWorkpiece == null)
                return;

            List<ResourceItem> inlays = CollectTrayInlays();
            if (inlays.Count == 0)
                return;

            selectedTrayInlayIndex += scrollValue > 0f ? -1 : 1;
            if (selectedTrayInlayIndex < 0) selectedTrayInlayIndex = inlays.Count - 1;
            if (selectedTrayInlayIndex >= inlays.Count) selectedTrayInlayIndex = 0;

            ResourceItem selected = inlays[selectedTrayInlayIndex];
            runtimeWorkpiece.PlanInlayOnSelectedSocket(selected.Type);
        }

        private List<ResourceItem> CollectTrayInlays()
        {
            List<ResourceItem> result = new();

            if (runtimeTray == null)
                runtimeTray = trayHolder != null ? trayHolder.CurrentItem as TrayItem : null;

            if (runtimeTray == null)
                return result;

            IReadOnlyList<ItemBase> items = runtimeTray.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is ResourceItem resource && ResourceTypeHelper.IsInlay(resource.Type))
                    result.Add(resource);
            }

            return result;
        }

        private void TryAbsorbPlayerLoadout()
        {
            if (handsController == null)
                return;

            TryAbsorbPlayerPaperStack();
            TryAbsorbPlayerTray();
        }

        private void TryAbsorbPlayerPaperStack()
        {
            if (itemsFactory == null)
                return;

            if (!handsController.TryTakeFirstItemFromHands(out PaperStackItem inputStack))
                return;

            List<ItemBase> extracted = inputStack.ExtractAllItems();
            Object.Destroy(inputStack.gameObject);

            List<CatalogPageItem> catalogPages = new();
            List<ItemBase> unknownPages = new();

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
                        unknownPages.Add(recipe);
                    }

                    continue;
                }

                if (item is CatalogPageItem catalogPage)
                {
                    catalogPages.Add(catalogPage);
                    continue;
                }

                unknownPages.Add(item);
            }

            catalogPages.Sort(CompareCatalogPagesByProductionOrder);

            EnsureRuntimeRecipePagesStack();
            for (int i = 0; i < catalogPages.Count; i++)
            {
                runtimeRecipePagesStack.TryAddPage(catalogPages[i]);
            }

            for (int i = 0; i < unknownPages.Count; i++)
            {
                runtimeRecipePagesStack.TryAddPage(unknownPages[i]);
            }
        }

        private void TryAbsorbPlayerTray()
        {
            if (handsController.TryTakeFirstItemFromHands(out TrayItem tray))
            {
                runtimeTray = tray;
                if (trayHolder != null)
                    trayHolder.AttachExternalContainer(tray);
            }
        }

        private void ReturnUnstartedLoadoutToPlayer()
        {
            if (handsController == null || itemsFactory == null)
                return;

            PaperStackItem outputStack = BuildOutputPaperStackFromWorkbench();
            if (outputStack != null && !handsController.GiveItem(outputStack))
            {
                Debug.LogWarning($"{name}: failed to return recipe stack to player hands. Returning it to workbench holder.");
                runtimeRecipePagesStack = outputStack;

                if (recipePagesHolder != null)
                    recipePagesHolder.AttachExternalContainer(outputStack);
            }

            TrayItem tray = trayHolder != null ? trayHolder.DetachCurrentContainer() as TrayItem : runtimeTray;
            runtimeTray = null;

            if (tray != null && !handsController.GiveItem(tray))
            {
                Debug.LogWarning($"{name}: failed to return tray to player hands.");
                if (trayHolder != null)
                    trayHolder.AttachExternalContainer(tray);
                runtimeTray = tray;
            }
        }

        private PaperStackItem BuildOutputPaperStackFromWorkbench()
        {
            MainRecipeItem recipe = mainRecipeSlot != null ? mainRecipeSlot.Detach() : null;
            PaperStackItem outputStack = DetachRecipePagesStackFromWorkbench();

            if (outputStack == null && recipe != null)
                outputStack = itemsFactory.CreatePaperStack();

            if (outputStack == null)
                return null;

            if (recipe != null)
            {
                recipe.gameObject.SetActive(true);

                if (!outputStack.TryAddPageOnTop(recipe))
                    outputStack.TryAddPage(recipe);
            }

            if (outputStack.Count == 0)
            {
                Object.Destroy(outputStack.gameObject);
                return null;
            }

            outputStack.gameObject.SetActive(true);
            outputStack.SetWorldRenderLayer();
            return outputStack;
        }

        private PaperStackItem DetachRecipePagesStackFromWorkbench()
        {
            PaperStackItem stack = null;

            if (recipePagesHolder != null)
                stack = recipePagesHolder.DetachCurrentContainer() as PaperStackItem;

            if (stack == null)
                stack = runtimeRecipePagesStack;

            runtimeRecipePagesStack = null;
            return stack;
        }

        private ResourceItem TakeFirstBlankFromTray()
        {
            if (runtimeTray == null)
                runtimeTray = trayHolder != null ? trayHolder.CurrentItem as TrayItem : null;

            if (runtimeTray == null)
                return null;

            IReadOnlyList<ItemBase> items = runtimeTray.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not ResourceItem resource)
                    continue;

                if (!ResourceTypeHelper.IsBlank(resource.Type))
                    continue;

                if (!runtimeTray.TryRemove(resource))
                    return null;

                resource.gameObject.SetActive(true);
                return resource;
            }

            return null;
        }

        private MaskWorkpiece CreateWorkpieceFromBlank(ResourceItem blank)
        {
            ResourceType blankType = blank != null ? blank.Type : ResourceType.None;
            Transform socket = workpieceSocket != null ? workpieceSocket : transform;

            MaskWorkpiece workpiece = workpiecePrefab != null
                ? Instantiate(workpiecePrefab, socket)
                : new GameObject("Runtime_MaskWorkpiece").AddComponent<MaskWorkpiece>();

            workpiece.transform.SetParent(socket, false);
            workpiece.transform.localPosition = workpieceLocalPosition;
            workpiece.transform.localRotation = Quaternion.Euler(workpieceLocalEuler);
            workpiece.transform.localScale = workpieceLocalScale;
            workpiece.Init(blankType);
            workpiece.gameObject.SetActive(true);

            return workpiece;
        }

        private void DestroyConsumedBlankResource()
        {
            if (runtimeBlankResource == null)
                return;

            Object.Destroy(runtimeBlankResource.gameObject);
            runtimeBlankResource = null;
        }


        private void DestroyRuntimeWorkpiece()
        {
            if (runtimeWorkpiece == null)
                return;

            Object.Destroy(runtimeWorkpiece.gameObject);
            runtimeWorkpiece = null;
        }

        private void AttachCompletedMaskToTable(MaskItem mask)
        {
            if (mask == null)
                return;

            Transform socket = completedMaskSocket != null ? completedMaskSocket : transform;
            mask.transform.SetParent(socket);
            mask.transform.localPosition = Vector3.zero;
            mask.transform.localRotation = Quaternion.identity;
            mask.SetWorldRenderLayer();
            mask.gameObject.SetActive(true);
        }

        private bool GiveCompletedMaskToPlayer()
        {
            if (handsController == null)
                return false;

            if (runtimeCompletedMask == null)
                runtimeCompletedMask = session.CompletedMask;

            if (runtimeCompletedMask == null)
                return false;

            MaskItem mask = runtimeCompletedMask;
            runtimeCompletedMask = null;

            if (!handsController.GiveItem(mask))
            {
                Debug.LogWarning($"{name}: failed to give completed mask to player hands. Returning it to table.");
                runtimeCompletedMask = mask;
                AttachCompletedMaskToTable(mask);
                return false;
            }

            return true;
        }

        private void CleanupCompletedSessionTrash()
        {
            mainRecipeSlot?.ClearAndDestroy();

            if (recipePagesHolder != null)
                recipePagesHolder.EmergencyClearAndDestroy();
            runtimeRecipePagesStack = null;

            if (trayHolder != null)
                trayHolder.EmergencyClearAndDestroy();
            runtimeTray = null;

            DestroyConsumedBlankResource();

            DestroyRuntimeWorkpiece();
        }

        private void CleanupEmptyRuntimeStack()
        {
            if (recipePagesHolder != null)
                recipePagesHolder.DetachCurrentContainer();

            if (runtimeRecipePagesStack != null && runtimeRecipePagesStack.Count == 0)
            {
                Object.Destroy(runtimeRecipePagesStack.gameObject);
                runtimeRecipePagesStack = null;
            }
        }

        private void EnsureRuntimeRecipePagesStack()
        {
            if (runtimeRecipePagesStack != null)
                return;

            if (itemsFactory == null)
                return;

            runtimeRecipePagesStack = itemsFactory.CreatePaperStack();
            if (recipePagesHolder != null)
                recipePagesHolder.AttachExternalContainer(runtimeRecipePagesStack);
        }

        private void RemoveFirstRecipePageOfKind(CatalogPageKind kind)
        {
            if (runtimeRecipePagesStack == null)
                return;

            ItemBase removed = runtimeRecipePagesStack.TryRemoveFirst(item =>
                item is CatalogPageItem page && page.PageKind == kind);

            if (removed != null)
                Object.Destroy(removed.gameObject);

            RefreshRuntimeViews();
        }

        private void RefreshRuntimeViews()
        {
            mainRecipeSlot?.RefreshVisual();
            recipePagesHolder?.RefreshCurrentContainerView();
            trayHolder?.RefreshCurrentContainerView();
            runtimeWorkpiece?.SetViewMode(state);
        }

        private void EnsureReferences()
        {
            if (itemsFactory == null)
                itemsFactory = FindFirstObjectByType<ItemsFactory>();

            if (handsController == null)
                handsController = FindFirstObjectByType<PlayerHandsController>();

            if (playerInput == null && handsController != null)
                playerInput = handsController.GetComponent<PlayerInput>();

            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();
        }

        private static int CompareCatalogPagesByProductionOrder(CatalogPageItem a, CatalogPageItem b)
        {
            return GetProductionOrder(a != null ? a.PageKind : CatalogPageKind.None)
                .CompareTo(GetProductionOrder(b != null ? b.PageKind : CatalogPageKind.None));
        }

        private static int GetProductionOrder(CatalogPageKind kind)
        {
            switch (kind)
            {
                case CatalogPageKind.MistResistance:
                    return 0;
                case CatalogPageKind.FaceCover:
                    return 1;
                case CatalogPageKind.District:
                    return 2;
                case CatalogPageKind.Faction:
                    return 3;
                default:
                    return 99;
            }
        }

        private void SetState(MaskWorkbenchState nextState)
        {
            if (state == nextState)
                return;

            state = nextState;

            if (nextState == MaskWorkbenchState.SizeSelection
                || nextState == MaskWorkbenchState.FormSelection
                || nextState == MaskWorkbenchState.InlaySelection
                || nextState == MaskWorkbenchState.Completed)
            {
                savedProductionState = nextState;
            }

            runtimeWorkpiece?.SetViewMode(state);

            if (logStateChanges)
                Debug.Log($"{name}: MaskWorkbench state -> {state}");
        }

        private void RefreshInputActionsForCurrentMap()
        {
            UnsubscribeInputActions();
            advanceProductionAction = null;
            navigateProductionAction = null;
            selectProductionAction = null;
            scrollInlayAction = null;

            if (playerInput == null)
                return;

            InputActionMap currentMap = playerInput.currentActionMap;
            if (currentMap == null)
                return;

            if (!string.IsNullOrWhiteSpace(advanceProductionActionName))
                advanceProductionAction = currentMap.FindAction(advanceProductionActionName, false);

            if (!string.IsNullOrWhiteSpace(navigateProductionActionName))
                navigateProductionAction = currentMap.FindAction(navigateProductionActionName, false);

            if (!string.IsNullOrWhiteSpace(selectProductionActionName))
                selectProductionAction = currentMap.FindAction(selectProductionActionName, false);

            if (!string.IsNullOrWhiteSpace(scrollInlayActionName))
                scrollInlayAction = currentMap.FindAction(scrollInlayActionName, false);
        }

        private void SubscribeInputActions()
        {
            if (inputSubscribed)
                return;

            if (advanceProductionAction != null)
                advanceProductionAction.performed += OnAdvanceProductionPerformed;

            if (navigateProductionAction != null)
                navigateProductionAction.performed += OnNavigateProductionPerformed;

            if (selectProductionAction != null)
                selectProductionAction.performed += OnSelectProductionPerformed;

            if (scrollInlayAction != null)
                scrollInlayAction.performed += OnScrollInlayPerformed;

            inputSubscribed = true;
        }

        private void UnsubscribeInputActions()
        {
            if (!inputSubscribed)
                return;

            if (advanceProductionAction != null)
                advanceProductionAction.performed -= OnAdvanceProductionPerformed;

            if (navigateProductionAction != null)
                navigateProductionAction.performed -= OnNavigateProductionPerformed;

            if (selectProductionAction != null)
                selectProductionAction.performed -= OnSelectProductionPerformed;

            if (scrollInlayAction != null)
                scrollInlayAction.performed -= OnScrollInlayPerformed;

            inputSubscribed = false;
        }

        private void OnAdvanceProductionPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            if (!IsCraftSurfaceInputActive())
                return;

            AdvanceProduction();
        }

        private void OnNavigateProductionPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            Vector2 value = ctx.ReadValue<Vector2>();
            HandleProductionNavigation(value);
        }

        private void OnSelectProductionPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            if (!IsCraftSurfaceInputActive())
                return;

            if (state != MaskWorkbenchState.SizeSelection || runtimeWorkpiece == null)
                return;

            runtimeWorkpiece.ToggleSelectedSegmentCutMark();
        }

        private void OnScrollInlayPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            Vector2 value = ctx.ReadValue<Vector2>();
            HandleInlayScroll(value.y);
        }

        private bool IsCraftSurfaceInputActive()
        {
            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return false;
            if (controller.ActiveWorkbench != this) return false;
            if (playerInput == null || playerInput.currentActionMap == null) return false;
            if (playerInput.currentActionMap.name != craftSurfaceActionMap) return false;

            return state == MaskWorkbenchState.CraftSurfaceInspect
                   || state == MaskWorkbenchState.SizeSelection
                   || state == MaskWorkbenchState.FormSelection
                   || state == MaskWorkbenchState.InlaySelection
                   || state == MaskWorkbenchState.MiniGame
                   || state == MaskWorkbenchState.Completed;
        }
    }
}
