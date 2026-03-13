using DB;
using Enums;
using Global;
using UnityEngine;

namespace Systems
{
    public class QuestSystem : MonoBehaviour
    {
        private OrdersSystem ordersSystem;

        private DBQuest.QuestData currentQuest;
        private QuestState currentState;
        
        public void Link()
        {
            ordersSystem = Linker.Instance.OrdersSystem;
            
            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
        }

        public void ChangeQuestState()
        {
            switch (currentState)
            {
                case QuestState.Start:
                    currentState = QuestState.Await;
                    break;
                case QuestState.Await:
                    currentState = QuestState.Request;
                    break;
                case QuestState.Request:
                    //TODO change to result check of mask validation system 
                    currentState = QuestState.Success;
                    break;
            }

            Debug.Log($"QuestState changed. quest: {currentQuest.Id}, order: {currentQuest.OR_Id}, state: {currentState}");
        }
        
        public string[] GetDialogs()
        {
            for (int i = 0; i < currentQuest.States.Length; i++)
            {
                if (currentQuest.States[i].State == currentState)
                {
                    return currentQuest.States[i].DI_Id;
                }
            }
            
            Debug.LogError($"No dialogs found. quest: {currentQuest.Id}, order: {currentQuest.OR_Id}, state: {currentState}");
            return null;
        }

        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentQuest = targetQuest;
            currentState = QuestState.Start;
        }
    }
}