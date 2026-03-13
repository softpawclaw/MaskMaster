using Enums;
using Systems;
using UnityEngine;

namespace Global
{
    public class Campaign : MonoBehaviour
    {
        [SerializeField] private int totalDays = 0;
        
        private DaySystem daySystem = null;
        
        public void Link()
        {
            daySystem = Linker.Instance.DaySystem;
            daySystem.OnDayStateChangedDelegate += OnDayStateChangedSignature;
        }
        
        private void Start()
        {
            StartDay();
        }

        private void StartDay()
        {
            daySystem.StartDay();
        }

        private void OnDayStateChangedSignature(EDayState eDayState, int endedDayNum)
        {
            if (eDayState != EDayState.Complete) return;
            
            Debug.Log($"{this.name} Day {endedDayNum} completed.");
            
            if (totalDays > endedDayNum)
            {
                StartDay();
            }
            else
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            Debug.Log("Ending game");
        }
    }
}