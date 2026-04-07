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

        private DBMask.MaskData currentMask;

        private PlayerHandsController playerHandsController = null;
        private UISystem uiSystem = null;
        private QuestSystem questSystem = null;
        private OrdersSystem ordersSystem = null;
        private ItemsFactory itemsFactory = null;
        private DelayedDialogSystem delayedDialogSystem = null;

        private int currentDialog = 0;

        // false -> в Request сначала просто показываем prompt-диалог
        // true  -> следующий интеракт в Request уже пытается принять маску
        private bool requestPromptShown = false;

        // true -> текущий заказ уже завершён через это окно, повторно его закрывать нельзя
        private bool currentOrderFinalized = false;

        public void Link()
        {
            uiSystem = Linker.Instance.UISystem;
            questSystem = Linker.Instance.QuestSystem;
            ordersSystem = Linker.Instance.OrdersSystem;
            playerHandsController = Linker.Instance.PlayerHandsController;
            itemsFactory = Linker.Instance.ItemsFactory;
            delayedDialogSystem = Linker.Instance.DelayedDialogSystem;

            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
            OnNextDialog += OnNextDialogSignature;
        }

        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentMask = targetMask;
            currentDialog = 0;
            requestPromptShown = false;
            currentOrderFinalized = false;
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

            if (currentOrderFinalized)
            {
                CompleteInteraction(interactor);
                return;
            }

            currentDialog = 0;

            if (questSystem.CurrentState == QuestState.Request && requestPromptShown)
            {
                TryCompleteRequestFlow(interactor);
                return;
            }

            if (questSystem.CurrentState == QuestState.Success)
            {
                CompleteInteraction(interactor);
                return;
            }

            OnNextDialog?.Invoke();
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
                    FinishSuccessFlow();
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

            questSystem.ChangeQuestState(); // Start -> Await
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
            CompleteInteraction(playerHandsController.gameObject);
        }

        private void TryCompleteRequestFlow(GameObject interactor)
        {
            var mask = TryGetMaskFromHands();

            if (mask == null)
            {
                Debug.Log("OrderWindowInteractable: no mask in hands.");
                CompleteInteraction(interactor);
                return;
            }

            if (mask.OrderId != questSystem.CurrentOrderId)
            {
                Debug.Log($"OrderWindowInteractable: wrong mask. Expected {questSystem.CurrentOrderId}, got {mask.OrderId}");
                CompleteInteraction(interactor);
                return;
            }

            playerHandsController.FreeItem(mask);
            Destroy(mask.gameObject);

            questSystem.ChangeQuestState(); // Request -> Success

            currentDialog = 0;
            OnNextDialog?.Invoke();
        }

        private void FinishSuccessFlow()
        {
            if (currentOrderFinalized)
            {
                CompleteInteraction(playerHandsController.gameObject);
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