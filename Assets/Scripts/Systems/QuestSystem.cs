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
        public QuestState CurrentState { get; private set; }

        public string CurrentOrderId => currentQuest.OR_Id;
        public string CurrentQuestId => currentQuest.Id;

        public void Link()
        {
            ordersSystem = Linker.Instance.OrdersSystem;
            ordersSystem.OnOrderChosen += OnOrderChosenSignature;
        }

        public void ChangeQuestState()
        {
            switch (CurrentState)
            {
                case QuestState.Start:
                    CurrentState = QuestState.Await;
                    break;

                case QuestState.Await:
                    CurrentState = QuestState.Request;
                    break;

                case QuestState.Request:
                    CurrentState = QuestState.Success;
                    break;
            }

            Debug.Log($"QuestState changed. quest: {currentQuest.Id}, order: {currentQuest.OR_Id}, state: {CurrentState}");
        }

        public string[] GetDialogs()
        {
            for (int i = 0; i < currentQuest.States.Length; i++)
            {
                if (currentQuest.States[i].State == CurrentState)
                {
                    return currentQuest.States[i].DI_Id;
                }
            }

            Debug.LogError($"No dialogs found. quest: {currentQuest.Id}, order: {currentQuest.OR_Id}, state: {CurrentState}");
            return null;
        }

        private void OnOrderChosenSignature(DBQuest.QuestData targetQuest, DBMask.MaskData targetMask)
        {
            currentQuest = targetQuest;
            CurrentState = QuestState.Start;
        }
    }
}