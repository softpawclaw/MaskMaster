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
        
        //plug
        private float endDayDelay = 10f;
        
        private WorkDaySystem workDaySystem;
        private UISystem uiSystem;
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

        private void OnWorkStartSignature(int day)
        {
            currentOrder = 0;
            
            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Day == day)
                {
                    currentOrders = config[i].Orders;
                    StartWork();
                    return;
                }
            }
        }

        private void StartWork()
        {
            if (currentOrder < currentOrders.Length)
            {
                if (dbQuest.TryGetQuestDataByOrderId(currentOrders[currentOrder], out activeOrderQuest) &&
                    dbMask.TryGetMaskDataByOrderId(currentOrders[currentOrder], out activeOrderMask))
                {
                    OnOrderChosen?.Invoke(activeOrderQuest, activeOrderMask);
                }
            }
            else
            {
                StartCoroutine(EndWorkDelayed());
            }
        }
        
        //called after mask is placed on a storing shelf
        private void OnOrderCompleteSignature()
        {
            currentOrder++;
            StartWork();
        }

        private IEnumerator EndWorkDelayed()
        {
            yield return new WaitForSeconds(endDayDelay);
            
            EndWork();
        }

        private void EndWork()
        {
            OnOrdersCompleteDelegate?.Invoke();
        }
    }
}