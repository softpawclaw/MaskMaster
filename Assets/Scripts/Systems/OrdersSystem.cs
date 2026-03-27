using System;
using System.Collections;
using DB;
using Global;
using UnityEngine;

namespace Systems
{
    public class OrdersSystem : MonoBehaviour
    {
        [Serializable]
        public struct OrderData
        {
            public int Day;
            public string[] Orders;
        }

        public event Action<DBQuest.QuestData, DBMask.MaskData> OnOrderChosen;
        public event Action OnOrdersCompleteDelegate;

        [SerializeField] private OrderData[] config;

        private int currentOrder = 0;
        private string[] currentOrders;

        private float endDayDelay = 10f;

        private WorkDaySystem workDaySystem;
        private DBQuest dbQuest;
        private DBMask dbMask;

        private DBQuest.QuestData activeOrderQuest;
        private DBMask.MaskData activeOrderMask;

        public void Link()
        {
            workDaySystem = Linker.Instance.WorkDaySystem;
            dbQuest = Linker.Instance.DBQuest;
            dbMask = Linker.Instance.DBMask;

            workDaySystem.OnWorkStartDelegate += OnWorkStartSignature;
        }

        public void CompleteCurrentOrder()
        {
            currentOrder++;
            StartWork();
        }

        public bool HasMoreOrdersToday()
        {
            if (currentOrders == null || currentOrders.Length == 0)
                return false;

            return currentOrder + 1 < currentOrders.Length;
        }

        private void OnWorkStartSignature(int day)
        {
            currentOrder = 0;
            currentOrders = null;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Day == day)
                {
                    currentOrders = config[i].Orders;
                    StartWork();
                    return;
                }
            }

            Debug.LogWarning($"OrdersSystem: no orders configured for day {day}");
            StartCoroutine(EndWorkDelayed());
        }

        private void StartWork()
        {
            if (currentOrders == null || currentOrders.Length == 0)
            {
                StartCoroutine(EndWorkDelayed());
                return;
            }

            if (currentOrder < currentOrders.Length)
            {
                if (dbQuest.TryGetQuestDataByOrderId(currentOrders[currentOrder], out activeOrderQuest) &&
                    dbMask.TryGetMaskDataByOrderId(currentOrders[currentOrder], out activeOrderMask))
                {
                    Debug.Log($"OrdersSystem: starting order {currentOrder + 1}/{currentOrders.Length}. OR_Id={currentOrders[currentOrder]}");
                    OnOrderChosen?.Invoke(activeOrderQuest, activeOrderMask);
                }
                else
                {
                    Debug.LogError($"OrdersSystem: failed to resolve order data for OR_Id={currentOrders[currentOrder]}");
                }
            }
            else
            {
                StartCoroutine(EndWorkDelayed());
            }
        }

        private IEnumerator EndWorkDelayed()
        {
            yield return new WaitForSeconds(endDayDelay);
            EndWork();
        }

        private void EndWork()
        {
            Debug.Log("OrdersSystem: all orders for the day are complete.");
            OnOrdersCompleteDelegate?.Invoke();
        }
    }
}