using System.Collections;
using DB;
using Enums;
using Global;
using Interactable.Workbench;
using Items;
using Player;
using Systems;
using UnityEngine;

namespace Interactable
{
    /// <summary>
    /// Order window is now a small workbench-like interaction:
    /// player enters a fixed view, shutter/ticket actions play, dialogues run,
    /// and player exits manually with cancel/back.
    /// </summary>
    public class OrderWindowInteractable : WorkbenchInteractableBase
    {
        [Header("Fallback Dialogues")]
        [SerializeField] private string[] noOneAtWindowDialogIds;
        [SerializeField] private string[] requestWaitingDialogIds;

        [Header("Shutter")]
        [SerializeField] private Animator shutterAnimator;
        [SerializeField] private string openShutterTrigger = "Open";
        [SerializeField] private string closeShutterTrigger = "Close";
        [SerializeField] private float openShutterDelay = 0.35f;
        [SerializeField] private float closeShutterDelay = 0.25f;
        [SerializeField] private bool shutterOpenOnStart = false;

        [Header("Debug")]
        [SerializeField] private bool logWindowFlow = true;

        private DBMask.MaskData currentMask;

        private PlayerHandsController playerHandsController;
        private UISystem uiSystem;
        private QuestSystem questSystem;
        private OrdersSystem ordersSystem;
        private ItemsFactory itemsFactory;
        private DelayedDialogSystem delayedDialogSystem;
        private MaskEvaluationSystem maskEvaluationSystem;
        private OrderTicketSystem orderTicketSystem;

        private PlayerWorkbenchModeController activeController;
        private Coroutine flowRoutine;
        private bool flowInProgress;
        private bool shutterIsOpen;
        private bool closeShutterOnExit = true;
        private bool currentOrderFinalized;

        public void Link()
        {
            uiSystem = Linker.Instance.UISystem;
            questSystem = Linker.Instance.QuestSystem;
            ordersSystem = Linker.Instance.OrdersSystem;
            playerHandsController = Linker.Instance.PlayerHandsController;
            itemsFactory = Linker.Instance.ItemsFactory;
            delayedDialogSystem = Linker.Instance.DelayedDialogSystem;
            maskEvaluationSystem = Linker.Instance.MaskEvaluationSystem;
            orderTicketSystem = Linker.Instance.OrderTicketSystem;

            shutterIsOpen = shutterOpenOnStart;

            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
        }

        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentMask = targetMask;
            currentOrderFinalized = false;

            if (logWindowFlow)
                Debug.Log($"OrderWindowInteractable: order selected. OR_Id={currentMask.OR_Id}, ClientId={currentMask.ClientId}");
        }

        public override void OnWorkbenchEntered(PlayerWorkbenchModeController controller)
        {
            base.OnWorkbenchEntered(controller);

            activeController = controller;
            playerHandsController ??= controller.GetComponent<PlayerHandsController>();

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(WindowFlowRoutine(controller.gameObject));
        }

        public override void HandleCancelFromOverview()
        {
            if (flowInProgress)
                return;

            if (activeController == null)
            {
                base.HandleCancelFromOverview();
                return;
            }

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(ExitWindowRoutine());
        }

        public override void HandleFocusInteract(WorkbenchFocusTarget target)
        {
            // MVP: order window does not use focus targets yet.
        }

        private IEnumerator WindowFlowRoutine(GameObject interactor)
        {
            flowInProgress = true;

            EnsurePlayerHands(interactor);
            yield return EnsureShutterOpen();

            if (currentOrderFinalized)
            {
                closeShutterOnExit = true;
                yield return PlayRandomDialogue(noOneAtWindowDialogIds, "noOneAtWindowDialogIds");
                FinishFlow();
                yield break;
            }

            switch (questSystem.CurrentState)
            {
                case QuestState.Start:
                    yield return NewOrderFlow();
                    break;

                case QuestState.Await:
                    closeShutterOnExit = true;
                    yield return PlayRandomDialogue(noOneAtWindowDialogIds, "noOneAtWindowDialogIds");
                    break;

                case QuestState.Request:
                    yield return CustomerRequestFlow();
                    break;

                case QuestState.MaskAwait:
                    yield return MaskAwaitFlow(interactor);
                    break;

                case QuestState.Success:
                case QuestState.Failure:
                    yield return FinalOrderDialogFlow();
                    break;

                default:
                    closeShutterOnExit = true;
                    yield return PlayRandomDialogue(noOneAtWindowDialogIds, "noOneAtWindowDialogIds");
                    break;
            }

            FinishFlow();
        }

        private IEnumerator NewOrderFlow()
        {
            closeShutterOnExit = true;

            yield return PlayQuestDialogSequence();

            string questId = questSystem.CurrentQuestId;
            if (orderTicketSystem != null && orderTicketSystem.TryAssignFreeTicket(questId, out int ticketNumber))
            {
                orderTicketSystem.PlaceTicketAtWindowSpotInstant(ticketNumber);
                yield return orderTicketSystem.MoveTicketFromWindowToCustomer(ticketNumber);
            }
            else
            {
                Debug.LogWarning("OrderWindowInteractable: OrderTicketSystem missing or no free ticket. Order will continue without visible ticket.");
            }

            GiveRecipeToPlayer();
            questSystem.ChangeQuestState();
        }

        private IEnumerator CustomerRequestFlow()
        {
            closeShutterOnExit = false;

            yield return PlayQuestDialogSequence();

            string questId = questSystem.CurrentQuestId;
            if (orderTicketSystem != null && orderTicketSystem.TryGetTicketForQuest(questId, out int ticketNumber))
            {
                yield return orderTicketSystem.MoveTicketFromCustomerToWindow(ticketNumber);
            }
            else
            {
                Debug.LogWarning($"OrderWindowInteractable: no ticket found for quest {questId}.");
            }

            questSystem.ChangeQuestState();
        }

        private IEnumerator MaskAwaitFlow(GameObject interactor)
        {
            MaskItem mask = TryGetMaskFromHands();

            if (mask == null)
            {
                closeShutterOnExit = false;
                yield return PlayRandomDialogue(requestWaitingDialogIds, "requestWaitingDialogIds");
                yield break;
            }

            closeShutterOnExit = true;

            playerHandsController.FreeItem(mask);

            QuestState resultState = QuestState.Success;
            if (maskEvaluationSystem != null)
            {
                var result = maskEvaluationSystem.Evaluate(mask);
                resultState = result.IsSuccess ? QuestState.Success : QuestState.Failure;
            }
            else
            {
                Debug.LogWarning("OrderWindowInteractable: MaskEvaluationSystem is not linked. Falling back to Success.");
            }

            Destroy(mask.gameObject);

            questSystem.SetQuestState(resultState);

            yield return PlayQuestDialogSequence();

            FinishCurrentOrder();
        }

        private IEnumerator FinalOrderDialogFlow()
        {
            closeShutterOnExit = true;
            yield return PlayQuestDialogSequence();
            FinishCurrentOrder();
        }

        private void FinishCurrentOrder()
        {
            if (currentOrderFinalized)
                return;

            currentOrderFinalized = true;

            string questId = questSystem.CurrentQuestId;
            orderTicketSystem?.ReturnTicketToRack(questId);

            bool hasMoreOrdersToday = ordersSystem.HasMoreOrdersToday();

            ordersSystem.CompleteCurrentOrder();

            if (hasMoreOrdersToday)
            {
                delayedDialogSystem?.ScheduleBell();
            }
        }

        private void GiveRecipeToPlayer()
        {
            if (playerHandsController == null || itemsFactory == null)
            {
                Debug.LogWarning("OrderWindowInteractable: cannot give recipe, hands or factory missing.");
                return;
            }

            playerHandsController.OnItemTaken += GiveRecipeDelayed;

            PaperStackItem paperStack = itemsFactory.CreatePaperStack();
            playerHandsController.GiveItem(paperStack);
        }

        private void GiveRecipeDelayed()
        {
            playerHandsController.OnItemTaken -= GiveRecipeDelayed;

            if (itemsFactory == null)
                return;

            MainRecipeItem recipe = itemsFactory.CreateMainRecipe(currentMask);
            playerHandsController.GiveItem(recipe);
        }

        private IEnumerator PlayQuestDialogSequence()
        {
            string[] dialogs = questSystem.GetDialogs();
            if (dialogs == null || dialogs.Length == 0)
                yield break;

            for (int i = 0; i < dialogs.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(dialogs[i]))
                    continue;

                yield return PlayDialogue(dialogs[i]);
            }
        }

        private IEnumerator PlayRandomDialogue(string[] dialogueIds, string fieldName)
        {
            string dialogueId = GetRandomDialogueId(dialogueIds);

            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                Debug.LogWarning($"OrderWindowInteractable: {fieldName} is empty.");
                yield break;
            }

            yield return PlayDialogue(dialogueId);
        }

        private IEnumerator PlayDialogue(string dialogueId)
        {
            if (uiSystem == null)
            {
                Debug.LogWarning("OrderWindowInteractable: UISystem is not linked.");
                yield break;
            }

            bool complete = false;
            uiSystem.Execute(dialogueId, () => complete = true);

            while (!complete)
                yield return null;
        }

        private IEnumerator EnsureShutterOpen()
        {
            if (shutterIsOpen)
                yield break;

            if (shutterAnimator != null && !string.IsNullOrWhiteSpace(openShutterTrigger))
                shutterAnimator.SetTrigger(openShutterTrigger);

            shutterIsOpen = true;

            if (openShutterDelay > 0f)
                yield return new WaitForSeconds(openShutterDelay);
        }

        private IEnumerator EnsureShutterClosed()
        {
            if (!shutterIsOpen)
                yield break;

            if (shutterAnimator != null && !string.IsNullOrWhiteSpace(closeShutterTrigger))
                shutterAnimator.SetTrigger(closeShutterTrigger);

            shutterIsOpen = false;

            if (closeShutterDelay > 0f)
                yield return new WaitForSeconds(closeShutterDelay);
        }

        private IEnumerator ExitWindowRoutine()
        {
            flowInProgress = true;

            if (closeShutterOnExit)
                yield return EnsureShutterClosed();

            flowInProgress = false;

            if (activeController != null)
                activeController.ExitWorkbench(ExitDuration);
        }

        private void FinishFlow()
        {
            flowInProgress = false;
            flowRoutine = null;
        }

        private void EnsurePlayerHands(GameObject interactor)
        {
            if (playerHandsController != null)
                return;

            playerHandsController = interactor != null ? interactor.GetComponent<PlayerHandsController>() : null;

            if (playerHandsController == null && Linker.Instance != null)
                playerHandsController = Linker.Instance.PlayerHandsController;
        }

        private MaskItem TryGetMaskFromHands()
        {
            if (playerHandsController == null)
                return null;

            ItemBase right = playerHandsController.GetItem(HandType.Right);
            if (right is MaskItem rightMask)
                return rightMask;

            ItemBase left = playerHandsController.GetItem(HandType.Left);
            if (left is MaskItem leftMask)
                return leftMask;

            return null;
        }

        private string GetRandomDialogueId(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0)
                return null;

            if (dialogueIds.Length == 1)
                return dialogueIds[0];

            return dialogueIds[Random.Range(0, dialogueIds.Length)];
        }
    }
}
