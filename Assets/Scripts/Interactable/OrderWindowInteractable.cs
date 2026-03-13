using System;
using DB;
using Global;
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
        
        private int currentDialog = 0;

        public void Link()
        {
            uiSystem = Linker.Instance.UISystem;
            questSystem = Linker.Instance.QuestSystem;
            ordersSystem = Linker.Instance.OrdersSystem;
            playerHandsController = Linker.Instance.PlayerHandsController;

            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
            OnNextDialog += OnNextDialogSignature;
        }
        
        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentMask = targetMask;
            currentDialog = 0;
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (playerHandsController == null)
            { 
                interactor.GetComponent<PlayerHandsController>();
            }
            
            OnNextDialog?.Invoke();
        }

        private void OnNextDialogSignature()
        {
            var dialogs = questSystem.GetDialogs();
            
            if (dialogs.Length == 0) return;
            
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
            questSystem.ChangeQuestState();
            playerHandsController.GiveRecipe(currentMask);
        }
    }
}