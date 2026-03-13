using System;
using Enums;
using UnityEngine;

namespace Systems
{
    public class DaySystem : ExecuteSystemBase
    {
        [Serializable]
        public struct DayData
        {
            public string Id;
            public EDayState DayState;

            public DayData(string id, EDayState dayState)
            {
                Id = id;
                DayState = dayState;
            }
        }
        
        [SerializeField] private DayData[] config;
        
        public event Action<EDayState, int> OnDayStateChangedDelegate;

        public int CurrentDay { private set; get; } = 0;
        private Day currentDay = null;
        
        public override void Execute(string id, Action completeAction)
        {
            for (int i = 0; i < config.Length; i++)
            {
                if (id == config[i].Id)
                {
                    if (currentDay != null)
                    {
                        completeAction?.Invoke();
                        currentDay.UpdateDayState(config[i].DayState);
                    }
                    break;
                }
            }
        }
        
        public void StartDay()
        {
            CurrentDay += 1;
            Debug.Log($"{this.name} Current day set to {CurrentDay}");
            
            if (currentDay != null)
            {
                currentDay.OnDayStateChangedDelegate -= OnDayChangedSignature;
            }
            
            currentDay = new Day();
            currentDay.OnDayStateChangedDelegate += OnDayChangedSignature;
            currentDay.UpdateDayState(EDayState.Start);
        }

        private void OnDayChangedSignature(EDayState state)
        {
            OnDayStateChangedDelegate?.Invoke(state, CurrentDay);
        }
    }
}