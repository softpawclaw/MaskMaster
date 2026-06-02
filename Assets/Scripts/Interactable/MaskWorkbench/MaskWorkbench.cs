using System;
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

        [Header("Production Sockets")]
        [SerializeField] private MaskBlankWorkpiece blankWorkpiecePrefab;
        [SerializeField] private Transform blankSocket;
        [SerializeField] private Transform maskSocket;
        [SerializeField] private MaskWorkpieceSelectorView partSelector;

        [Header("Inlay Selection")]
        [SerializeField] private DBInlayVisual dbInlayVisual;
        [SerializeField] private MaskInlaySelectionCursor inlayCursorPrefab;
        [SerializeField] private GameObject clearSocketPreviewPrefab;

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

        [Header("Mini Games")]
        [SerializeField] private MaskMiniGameSystem miniGameSystem;
        [SerializeField] private string cutSizeMiniGameConfigId = "MG_Cut_Size_Default";
        [SerializeField] private string shapeMiniGameConfigId = "MG_Shape_Default";
        [SerializeField] private string inlayMiniGameConfigPrefix = "MG_Inlay_";
        [SerializeField] private string inlayMiniGameConfigSuffix = "_Default";

        [Header("Completion")]
        [SerializeField] private bool autoExitOnCraftComplete = true;

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
        private MaskBlankWorkpiece runtimeBlankWorkpiece;
        private MaskItem runtimeCraftMask;
        private MaskInlaySelectionCursor runtimeInlayCursor;

        private readonly Queue<KeyValuePair<ResourceType, List<MaskItem.PlannedInlay>>> pendingInlayMiniGames = new();
        private KeyValuePair<ResourceType, List<MaskItem.PlannedInlay>> activeInlayMiniGameGroup;
        private MaskWorkbenchState stateBeforeMiniGame = MaskWorkbenchState.CraftSurfaceInspect;
        private System.Action pendingMiniGameComplete;
        private InputAction advanceProductionAction;
        private InputAction navigateProductionAction;
        private InputAction selectProductionAction;
        private InputAction scrollInlayAction;
        private bool inputSubscribed;

        public event Action<MaskWorkbench> ProductionStarted;
        public event Action<MaskWorkbench> ProductionCompleted;

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
                runtimeBlankWorkpiece = session.BlankWorkpiece != null ? session.BlankWorkpiece : runtimeBlankWorkpiece;
                runtimeCraftMask = session.CraftMask != null ? session.CraftMask : runtimeCraftMask;
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
            if (state == MaskWorkbenchState.MiniGame)
            {
                Debug.Log($"{name}: cannot leave craft surface while mini-game is active.");
                return;
            }

            PlayerWorkbenchModeController controller = FindController();
            if (controller == null) return;

            controller.SwitchWorkbenchActionMap(controller.WorkbenchActionMapName);
            base.HandleCancelFromLockedView();
        }

        public override void HandleCancelFromOverview()
        {
            if (state == MaskWorkbenchState.MiniGame)
            {
                Debug.Log($"{name}: cannot leave workbench while mini-game is active.");
                return;
            }

            base.HandleCancelFromOverview();
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
                    miniGameSystem?.Confirm();
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
            runtimeBlankWorkpiece = CreateBlankWorkpieceFromBlank(blank);
            runtimeCraftMask = CreateCraftMask();
            runtimeCraftMask?.BeginCraftDataTracking(session.MainRecipe);
            runtimeCraftMask?.SetSourceBlank(blank.Type);
            session.MarkStarted(blank, runtimeBlankWorkpiece, runtimeCraftMask);
            ProductionStarted?.Invoke(this);
            DestroyConsumedBlankResource();

            RemoveFirstRecipePageOfKind(CatalogPageKind.MistResistance);
            SetState(MaskWorkbenchState.SizeSelection);
            RefreshRuntimeViews();
        }

        private void ConfirmSizeSelection()
        {
            MaskMiniGameRequest request = new(
                MaskMiniGameKind.CutSegment,
                runtimeBlankWorkpiece != null ? runtimeBlankWorkpiece.SelectedSegment : MaskSegment.Middle);

            if (ShouldAutoCompleteSizeCut())
            {
                AddExpectedAndActualMaxScore(request);
                CompleteSizeSelectionAfterMiniGame();
                return;
            }

            AddExpectedMaxScore(request);
            RunMiniGameOrComplete(request, CompleteSizeSelectionAfterMiniGame);
        }

        private void CompleteSizeSelectionAfterMiniGame()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.FaceCover);
            runtimeBlankWorkpiece?.ApplyMarkedCuts();
            runtimeCraftMask?.ApplySizeFromBlank(runtimeBlankWorkpiece);
            SetState(MaskWorkbenchState.FormSelection);
            RefreshRuntimeViews();
        }

        private void ConfirmFormSelection()
        {
            MaskMiniGameRequest request = new(
                MaskMiniGameKind.ShapeSegment,
                runtimeCraftMask != null ? runtimeCraftMask.SelectedSegment : MaskSegment.Middle);

            AddExpectedMaxScore(request);
            RunMiniGameOrComplete(request, CompleteFormSelectionAfterMiniGame);
        }

        private void CompleteFormSelectionAfterMiniGame()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.District);
            runtimeCraftMask?.SolidifySelectedShapes();
            DestroyRuntimeBlankWorkpiece();
            SetState(MaskWorkbenchState.InlaySelection);
            BeginInlaySelectionCursor();
            RefreshRuntimeViews();
        }

        private void ConfirmInlaySelection()
        {
            DestroyRuntimeInlayCursor();
            BuildPendingInlayMiniGames();

            if (pendingInlayMiniGames.Count <= 0)
            {
                CompleteInlaySelectionAfterMiniGames();
                return;
            }

            RunNextInlayMiniGameOrCompleteMask();
        }

        private void CompleteInlaySelectionAfterMiniGames()
        {
            RemoveFirstRecipePageOfKind(CatalogPageKind.Faction);
            CompleteInlaySelectionAndMask();
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

            if (runtimeCraftMask == null)
                runtimeCraftMask = itemsFactory.CreateMask(targetMaskData, actualMaskData);
            else
                runtimeCraftMask.Init(targetMaskData, actualMaskData);

            AttachCraftMaskToTable(runtimeCraftMask);
            DestroyRuntimeBlankWorkpiece();
            runtimeCraftMask.HideCraftHelpers();
            runtimeCraftMask.LogFinalCraftDataDump();
            session.MarkCompleted(runtimeCraftMask);
            ProductionCompleted?.Invoke(this);

            SetState(MaskWorkbenchState.Completed);
            RefreshRuntimeViews();
            TryAutoExitAfterCraftComplete();
        }

        private void TryAutoExitAfterCraftComplete()
        {
            if (!autoExitOnCraftComplete)
                return;

            PlayerWorkbenchModeController controller = FindController();
            if (controller == null)
                return;

            if (controller.ActiveWorkbench != this)
                return;

            controller.ExitWorkbench(ExitDuration);
        }

        private bool ShouldAutoCompleteSizeCut()
        {
            if (runtimeBlankWorkpiece == null || session.MainRecipe == null)
                return false;

            return session.MainRecipe.MaskSize == MaskSize.Large && !runtimeBlankWorkpiece.HasAnyMarkedCuts();
        }

        private void AddExpectedMaxScore(MaskMiniGameRequest request)
        {
            float maxScore = GetMaxMiniGameScore(request);
            runtimeCraftMask?.AddExpectedQualityPoints(maxScore);
        }

        private void AddExpectedAndActualMaxScore(MaskMiniGameRequest request)
        {
            float maxScore = GetMaxMiniGameScore(request);
            runtimeCraftMask?.AddExpectedQualityPoints(maxScore);
            string configId = ResolveMiniGameConfigId(request);
            runtimeCraftMask?.RecordAutoCompletedMiniGame(request.WithConfigAndAnchor(configId, null), maxScore);
        }

        private float GetMaxMiniGameScore(MaskMiniGameRequest request)
        {
            if (miniGameSystem == null)
                miniGameSystem = FindFirstObjectByType<MaskMiniGameSystem>();

            if (miniGameSystem == null)
                return 0f;

            return miniGameSystem.GetMaxScore(ResolveMiniGameConfigId(request));
        }

        private void RunMiniGameOrComplete(MaskMiniGameRequest request, System.Action onComplete)
        {
            if (request.Kind == MaskMiniGameKind.None)
            {
                onComplete?.Invoke();
                return;
            }

            if (miniGameSystem == null)
                miniGameSystem = FindFirstObjectByType<MaskMiniGameSystem>();

            if (miniGameSystem == null)
            {
                Debug.LogError($"{name}: cannot run mini-game {request.Kind}. MaskMiniGameSystem is not assigned/found.");
                return;
            }

            if (runtimeCraftMask == null)
            {
                Debug.LogError($"{name}: cannot run mini-game {request.Kind}. Runtime craft mask is null.");
                return;
            }

            Transform anchor = runtimeCraftMask.GetRandomMiniGameAnchor();
            if (anchor == null)
            {
                Debug.LogError($"{name}: cannot run mini-game {request.Kind}. MaskItem returned null mini-game anchor.");
                return;
            }

            string configId = ResolveMiniGameConfigId(request);
            MaskMiniGameRequest configuredRequest = request.WithConfigAndAnchor(configId, anchor);

            pendingMiniGameComplete = onComplete;
            stateBeforeMiniGame = state;
            SetState(MaskWorkbenchState.MiniGame);

            miniGameSystem.Run(configuredRequest, OnMiniGameComplete);
        }

        private void OnMiniGameComplete(MaskMiniGameResult result)
        {
            runtimeCraftMask?.RecordMiniGameResult(result);
            Debug.Log($"{name}: mini-game {result.Kind} ({result.ConfigId}) -> {result.Outcome}, score={result.Score}, t={result.CursorT:0.000}");

            System.Action callback = pendingMiniGameComplete;
            pendingMiniGameComplete = null;

            SetState(stateBeforeMiniGame);
            callback?.Invoke();
        }

        private string ResolveMiniGameConfigId(MaskMiniGameRequest request)
        {
            switch (request.Kind)
            {
                case MaskMiniGameKind.CutSegment:
                    return cutSizeMiniGameConfigId;
                case MaskMiniGameKind.ShapeSegment:
                    return shapeMiniGameConfigId;
                case MaskMiniGameKind.InstallInlay:
                    return $"{inlayMiniGameConfigPrefix}{request.ResourceType}{inlayMiniGameConfigSuffix}";
                default:
                    return null;
            }
        }

        private void BuildPendingInlayMiniGames()
        {
            pendingInlayMiniGames.Clear();

            if (runtimeCraftMask == null)
                return;

            Dictionary<ResourceType, List<MaskItem.PlannedInlay>> groups = runtimeCraftMask.BuildPlannedInlayGroups();
            foreach (KeyValuePair<ResourceType, List<MaskItem.PlannedInlay>> group in groups)
            {
                pendingInlayMiniGames.Enqueue(group);
            }
        }

        private void RunNextInlayMiniGameOrCompleteMask()
        {
            if (pendingInlayMiniGames.Count <= 0)
            {
                CompleteInlaySelectionAfterMiniGames();
                return;
            }

            activeInlayMiniGameGroup = pendingInlayMiniGames.Dequeue();
            MaskMiniGameRequest request = new(MaskMiniGameKind.InstallInlay, MaskSegment.Middle, activeInlayMiniGameGroup.Key);
            AddExpectedMaxScore(request);
            RunMiniGameOrComplete(
                request,
                () =>
                {
                    runtimeCraftMask?.ApplyInlayGroup(activeInlayMiniGameGroup.Key, activeInlayMiniGameGroup.Value);
                    RunNextInlayMiniGameOrCompleteMask();
                });
        }

        private void HandleProductionNavigation(Vector2 value)
        {
            if (!IsCraftSurfaceInputActive())
                return;

            if (value.sqrMagnitude < 0.2f)
                return;

            if (Mathf.Abs(value.y) >= Mathf.Abs(value.x))
            {
                if (state == MaskWorkbenchState.SizeSelection && runtimeBlankWorkpiece != null)
                {
                    if (value.y > 0f) runtimeBlankWorkpiece.SelectPreviousSegment();
                    else runtimeBlankWorkpiece.SelectNextSegment();
                    RefreshPartSelector();
                }
                else if ((state == MaskWorkbenchState.FormSelection || state == MaskWorkbenchState.InlaySelection) && runtimeCraftMask != null)
                {
                    if (value.y > 0f) runtimeCraftMask.SelectPreviousSegment();
                    else runtimeCraftMask.SelectNextSegment();
                    RefreshPartSelector();
                    RefreshInlayCursorAnchor();
                }
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
                        runtimeCraftMask?.SelectNextShape();
                    else
                        runtimeCraftMask?.SelectPreviousShape();
                    break;

                case MaskWorkbenchState.InlaySelection:
                    if (value.x > 0f)
                        runtimeCraftMask?.SelectNextSocket();
                    else
                        runtimeCraftMask?.SelectPreviousSocket();
                    RefreshInlayCursorAnchor();
                    break;
            }
        }

        private void HandleInlayScroll(float scrollValue)
        {
            if (!IsCraftSurfaceInputActive() || state != MaskWorkbenchState.InlaySelection)
                return;

            if (runtimeInlayCursor == null)
                BeginInlaySelectionCursor();

            if (runtimeInlayCursor == null || !runtimeInlayCursor.HasOptions)
                return;

            if (scrollValue > 0f)
                runtimeInlayCursor.ShowPrevious();
            else if (scrollValue < 0f)
                runtimeInlayCursor.ShowNext();
        }

        private void BeginInlaySelectionCursor()
        {
            if (runtimeCraftMask == null)
                return;

            DestroyRuntimeInlayCursor();

            if (dbInlayVisual == null)
                dbInlayVisual = FindFirstObjectByType<DBInlayVisual>();

            runtimeInlayCursor = inlayCursorPrefab != null
                ? Instantiate(inlayCursorPrefab, transform)
                : new GameObject("Runtime_MaskInlaySelectionCursor").AddComponent<MaskInlaySelectionCursor>();

            runtimeInlayCursor.Init(dbInlayVisual, CollectUniqueTrayInlayTypes(), clearSocketPreviewPrefab);
            RefreshInlayCursorAnchor();
        }

        private void RefreshInlayCursorAnchor()
        {
            if (runtimeInlayCursor == null || runtimeCraftMask == null || state != MaskWorkbenchState.InlaySelection)
                return;

            runtimeInlayCursor.AttachTo(runtimeCraftMask.GetSelectedSocketAnchor());
        }

        private void DestroyRuntimeInlayCursor()
        {
            if (runtimeInlayCursor == null)
                return;

            Destroy(runtimeInlayCursor.gameObject);
            runtimeInlayCursor = null;
        }

        private List<ResourceType> CollectUniqueTrayInlayTypes()
        {
            List<ResourceType> result = new();
            List<ResourceItem> inlays = CollectTrayInlays();

            for (int i = 0; i < inlays.Count; i++)
            {
                ResourceType type = inlays[i] != null ? inlays[i].Type : ResourceType.None;
                if (type == ResourceType.None || result.Contains(type))
                    continue;

                result.Add(type);
            }

            return result;
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

        private MaskBlankWorkpiece CreateBlankWorkpieceFromBlank(ResourceItem blank)
        {
            ResourceType blankType = blank != null ? blank.Type : ResourceType.None;
            Transform socket = blankSocket != null ? blankSocket : transform;

            MaskBlankWorkpiece workpiece = blankWorkpiecePrefab != null
                ? Instantiate(blankWorkpiecePrefab, socket)
                : new GameObject("Runtime_MaskBlankWorkpiece").AddComponent<MaskBlankWorkpiece>();

            workpiece.transform.SetParent(socket, false);
            workpiece.transform.localPosition = workpieceLocalPosition;
            workpiece.transform.localRotation = Quaternion.Euler(workpieceLocalEuler);
            workpiece.transform.localScale = workpieceLocalScale;
            workpiece.Init(blankType);
            workpiece.gameObject.SetActive(true);

            return workpiece;
        }

        private MaskItem CreateCraftMask()
        {
            if (itemsFactory == null || session.MainRecipe == null)
                return null;

            MaskItem mask = itemsFactory.CreateMaskForCraft(session.MainRecipe.MaskData);
            AttachCraftMaskToTable(mask);
            mask.SetWorkbenchViewMode(MaskWorkbenchState.CraftSurfaceInspect);
            return mask;
        }

        private void DestroyConsumedBlankResource()
        {
            if (runtimeBlankResource == null)
                return;

            Object.Destroy(runtimeBlankResource.gameObject);
            runtimeBlankResource = null;
        }


        private void DestroyRuntimeBlankWorkpiece()
        {
            if (runtimeBlankWorkpiece == null)
                return;

            Object.Destroy(runtimeBlankWorkpiece.gameObject);
            runtimeBlankWorkpiece = null;
        }

        private void AttachCraftMaskToTable(MaskItem mask)
        {
            if (mask == null)
                return;

            Transform socket = maskSocket != null ? maskSocket : transform;
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

            if (runtimeCraftMask == null)
                runtimeCraftMask = session.CompletedMask;

            if (runtimeCraftMask == null)
                return false;

            MaskItem mask = runtimeCraftMask;
            runtimeCraftMask = null;

            if (!handsController.GiveItem(mask))
            {
                Debug.LogWarning($"{name}: failed to give completed mask to player hands. Returning it to table.");
                runtimeCraftMask = mask;
                AttachCraftMaskToTable(mask);
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

            DestroyRuntimeBlankWorkpiece();
            DestroyRuntimeInlayCursor();
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
            MaskWorkbenchState visualState = GetVisualStateForRuntimeObjects();

            if (state != MaskWorkbenchState.InlaySelection)
                DestroyRuntimeInlayCursor();
            else if (runtimeInlayCursor == null)
                BeginInlaySelectionCursor();

            runtimeBlankWorkpiece?.SetViewMode(GetBlankVisualStateForRuntimeObjects());
            runtimeCraftMask?.SetWorkbenchViewMode(GetMaskVisualStateForRuntimeObjects());
            RefreshPartSelector();
            RefreshInlayCursorAnchor();
        }

        private MaskWorkbenchState GetVisualStateForRuntimeObjects()
        {
            return state == MaskWorkbenchState.MiniGame ? stateBeforeMiniGame : state;
        }

        private MaskWorkbenchState GetBlankVisualStateForRuntimeObjects()
        {
            MaskWorkbenchState visualState = GetVisualStateForRuntimeObjects();

            if (visualState == MaskWorkbenchState.Overview && session.HasStarted && !session.IsCompleted)
            {
                if (runtimeBlankWorkpiece != null)
                    return MaskWorkbenchState.FormSelection;
            }

            return visualState;
        }

        private MaskWorkbenchState GetMaskVisualStateForRuntimeObjects()
        {
            MaskWorkbenchState visualState = GetVisualStateForRuntimeObjects();

            if (visualState == MaskWorkbenchState.Overview && session.HasStarted && !session.IsCompleted)
            {
                if (savedProductionState == MaskWorkbenchState.InlaySelection)
                    return MaskWorkbenchState.Completed;

                return MaskWorkbenchState.CraftSurfaceInspect;
            }

            return visualState;
        }

        private void RefreshPartSelector()
        {
            if (partSelector == null)
                return;

            if (state == MaskWorkbenchState.SizeSelection && runtimeBlankWorkpiece != null)
            {
                partSelector.ShowAt(runtimeBlankWorkpiece.GetSelectedSelectionAnchor());
                return;
            }

            if ((state == MaskWorkbenchState.FormSelection || state == MaskWorkbenchState.InlaySelection) && runtimeCraftMask != null)
            {
                partSelector.ShowAt(runtimeCraftMask.GetSelectedSelectionAnchor());
                return;
            }

            partSelector.Hide();
        }

        private void EnsureReferences()
        {
            if (itemsFactory == null)
                itemsFactory = FindFirstObjectByType<ItemsFactory>();

            if (dbInlayVisual == null)
                dbInlayVisual = FindFirstObjectByType<DBInlayVisual>();

            if (handsController == null)
                handsController = FindFirstObjectByType<PlayerHandsController>();

            if (playerInput == null && handsController != null)
                playerInput = handsController.GetComponent<PlayerInput>();

            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();

            if (miniGameSystem == null)
                miniGameSystem = FindFirstObjectByType<MaskMiniGameSystem>();
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

            if (state != MaskWorkbenchState.InlaySelection)
                DestroyRuntimeInlayCursor();

            runtimeBlankWorkpiece?.SetViewMode(GetBlankVisualStateForRuntimeObjects());
            runtimeCraftMask?.SetWorkbenchViewMode(GetMaskVisualStateForRuntimeObjects());
            RefreshPartSelector();
            RefreshInlayCursorAnchor();

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

            if (state == MaskWorkbenchState.MiniGame)
            {
                miniGameSystem?.Confirm();
                return;
            }

            if (state == MaskWorkbenchState.SizeSelection && runtimeBlankWorkpiece != null)
            {
                runtimeBlankWorkpiece.ToggleSelectedSegmentCutMark();
                RefreshPartSelector();
                return;
            }

            if (state == MaskWorkbenchState.InlaySelection && runtimeCraftMask != null)
            {
                if (runtimeInlayCursor == null)
                    BeginInlaySelectionCursor();

                runtimeCraftMask.ApplyInlayCursorToSelectedSocket(runtimeInlayCursor);
                RefreshPartSelector();
                RefreshInlayCursorAnchor();
            }
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
