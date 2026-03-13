using System;
using System.Collections;
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
        
        public event Action OnOrdersCompleteDelegate;
        
        [SerializeField] private OrderData[] config;

        private int currentOrder = 0;
        private string[] currentOrders;
        
        //plug
        private float endDayDelay = 10f;
        
        private WorkDaySystem workDaySystem;
        public void Link()
        {
            workDaySystem = Linker.Instance.WorkDaySystem;
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
                //call order giver
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
            OnOrdersCompleteDelegate?.Invoke();
        }
    }
}