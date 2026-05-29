using System;
using DB;
using Enums;
using Global;
using Items;
using Player;
using Systems;
using UnityEngine;

namespace Interactable
{
    public class OrderWindowInteractable : Interactable
    {
        public event Action OnNextDialog;

        [Header("Fallback Dialogues")]
        [SerializeField] private string[] noOneAtWindowDialogIds;
        [SerializeField] private string[] requestWaitingDialogIds;

        private DBMask.MaskData currentMask;

        private PlayerHandsController playerHandsController = null;
        private UISystem uiSystem = null;
        private QuestSystem questSystem = null;
        private OrdersSystem ordersSystem = null;
        private ItemsFactory itemsFactory = null;
        private DelayedDialogSystem delayedDialogSystem = null;
        private MaskEvaluationSystem maskEvaluationSystem = null;

        private int currentDialog = 0;
        private bool requestPromptShown = false;
        private bool currentOrderFinalized = false;

        public void Link()
        {
            uiSystem = Linker.Instance.UISystem;
            questSystem = Linker.Instance.QuestSystem;
            ordersSystem = Linker.Instance.OrdersSystem;
            playerHandsController = Linker.Instance.PlayerHandsController;
            itemsFactory = Linker.Instance.ItemsFactory;
            delayedDialogSystem = Linker.Instance.DelayedDialogSystem;
            maskEvaluationSystem = Linker.Instance.MaskEvaluationSystem;

            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
            OnNextDialog += OnNextDialogSignature;
        }

        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentMask = targetMask;
            currentDialog = 0;
            requestPromptShown = false;
            currentOrderFinalized = false;

            Debug.Log($"OrderWindowInteractable: order selected. OR_Id={currentMask.OR_Id}, ClientId={currentMask.ClientId}");
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (playerHandsController == null)
            {
                playerHandsController = interactor.GetComponent<PlayerHandsController>();
            }

            if (playerHandsController == null)
            {
                Debug.LogWarning("OrderWindowInteractable: PlayerHandsController not found.");
                CompleteInteraction(interactor);
                return;
            }

            currentDialog = 0;

            if (currentOrderFinalized)
            {
                PlayNoOneAtWindow(interactor);
                return;
            }

            switch (questSystem.CurrentState)
            {
                case QuestState.Start:
                    OnNextDialog?.Invoke();
                    break;

                case QuestState.Await:
                    PlayNoOneAtWindow(interactor);
                    break;

                case QuestState.Request:
                    OnNextDialog?.Invoke();
                    break;

                case QuestState.MaskAwait:
                    TryCompleteRequestFlow(interactor);
                    break;

                case QuestState.Success:
                case QuestState.Failure:
                    OnNextDialog?.Invoke();
                    break;

                default:
                    PlayNoOneAtWindow(interactor);
                    break;
            }
        }

        private void OnNextDialogSignature()
        {
            var dialogs = questSystem.GetDialogs();

            if (dialogs == null || dialogs.Length == 0)
            {
                CompleteInteraction(playerHandsController != null ? playerHandsController.gameObject : gameObject);
                return;
            }

            if (dialogs.Length > currentDialog)
            {
                uiSystem.Execute(dialogs[currentDialog], OnNextDialog);
                currentDialog++;
            }
            else
            {
                OnDialogComplete();
            }
        }

        private void OnDialogComplete()
        {
            switch (questSystem.CurrentState)
            {
                case QuestState.Start:
                    GiveRecipeFlow();
                    break;

                case QuestState.Request:
                    FinishRequestPromptFlow();
                    break;

                case QuestState.Success:
                case QuestState.Failure:
                    FinishFinalOrderFlow();
                    break;

                default:
                    CompleteInteraction(playerHandsController != null ? playerHandsController.gameObject : gameObject);
                    break;
            }
        }

        private void GiveRecipeFlow()
        {
            playerHandsController.OnItemTaken += GiveRecipeDelayed;

            var paperStack = itemsFactory.CreatePaperStack();
            playerHandsController.GiveItem(paperStack);

            questSystem.ChangeQuestState();
            CompleteInteraction(playerHandsController.gameObject);
        }

        private void GiveRecipeDelayed()
        {
            playerHandsController.OnItemTaken -= GiveRecipeDelayed;

            var recipe = itemsFactory.CreateMainRecipe(currentMask);
            playerHandsController.GiveItem(recipe);
        }

        private void FinishRequestPromptFlow()
        {
            requestPromptShown = true;
            questSystem.ChangeQuestState();
            CompleteInteraction(playerHandsController.gameObject);
        }

        private void TryCompleteRequestFlow(GameObject interactor)
        {
            var mask = TryGetMaskFromHands();

            if (mask == null)
            {
                PlayRequestWaiting(interactor);
                return;
            }

            // Важно: окно принимает любую готовую маску.
            // Соответствие заказу оценивается после сдачи, не на этапе передачи предмета.
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

            currentDialog = 0;
            OnNextDialog?.Invoke();
        }

        private void FinishFinalOrderFlow()
        {
            if (currentOrderFinalized)
            {
                PlayNoOneAtWindow(playerHandsController.gameObject);
                return;
            }

            currentOrderFinalized = true;
            requestPromptShown = false;

            bool hasMoreOrdersToday = ordersSystem.HasMoreOrdersToday();

            ordersSystem.CompleteCurrentOrder();

            if (hasMoreOrdersToday)
            {
                delayedDialogSystem?.ScheduleBell();
            }

            CompleteInteraction(playerHandsController.gameObject);
        }

        private void PlayNoOneAtWindow(GameObject interactor)
        {
            PlayRandomDialogueOrComplete(noOneAtWindowDialogIds, interactor, "noOneAtWindowDialogIds");
        }

        private void PlayRequestWaiting(GameObject interactor)
        {
            PlayRandomDialogueOrComplete(requestWaitingDialogIds, interactor, "requestWaitingDialogIds");
        }

        private void PlayRandomDialogueOrComplete(string[] dialogueIds, GameObject interactor, string fieldName)
        {
            var dialogueId = GetRandomDialogueId(dialogueIds);

            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                Debug.LogWarning($"OrderWindowInteractable: {fieldName} is empty.");
                CompleteInteraction(interactor);
                return;
            }

            if (uiSystem == null)
            {
                Debug.LogWarning("OrderWindowInteractable: UISystem is not linked.");
                CompleteInteraction(interactor);
                return;
            }

            uiSystem.Execute(dialogueId, () => CompleteInteraction(interactor));
        }

        private string GetRandomDialogueId(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0)
                return null;

            if (dialogueIds.Length == 1)
                return dialogueIds[0];

            return dialogueIds[UnityEngine.Random.Range(0, dialogueIds.Length)];
        }

        private MaskItem TryGetMaskFromHands()
        {
            var right = playerHandsController.GetItem(HandType.Right);
            if (right is MaskItem rightMask)
                return rightMask;

            var left = playerHandsController.GetItem(HandType.Left);
            if (left is MaskItem leftMask)
                return leftMask;

            return null;
        }
    }
}
