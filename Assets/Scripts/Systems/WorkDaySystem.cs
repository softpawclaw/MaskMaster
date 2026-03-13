using System;
using Enums;
using Global;
using UnityEngine;

namespace Systems
{
    public class WorkDaySystem : MonoBehaviour
    {
        public event Action<int> OnWorkStartDelegate;
        
        private DaySystem daySystem = null;
        private OrdersSystem OrdersSystem = null;

        public void Link()
        {
            daySystem = Linker.Instance.DaySystem;
            OrdersSystem = Linker.Instance.OrdersSystem;
            
            daySystem.OnDayStateChangedDelegate += OnDayStateChangedSignature;
            OrdersSystem.OnOrdersCompleteDelegate += OnOrdersCompletedSignature;
        }

        private void OnDayStateChangedSignature(EDayState dayState, int currentDay)
        {
            if (dayState != EDayState.Work) return;

            StartWork(currentDay);
        }
        
        private void OnOrdersCompletedSignature()
        {
            daySystem.Execute("DA_End", null);
        }

        private void StartWork(int currentDay)
        {
            OnWorkStartDelegate?.Invoke(currentDay);
        }
    }
}