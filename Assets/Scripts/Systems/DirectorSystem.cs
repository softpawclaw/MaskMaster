using System;

using Enums;
using Global;
using UnityEngine;

namespace Systems
{
    public class DirectorSystem : MonoBehaviour
    {
        [Serializable]
        public struct DirectorData
        {
            public int Day;
            public EDayState DayState;
            public string[] Sequence;
        }

        public event Action OnSequencePartCompleted;
        
        [SerializeField] private DirectorData[] config;
        
        private DaySystem _daySystem = null;
        private UISystem _uiSystem = null;
        private AnimationSystem _animationSystem = null;
        private InteractionSystem _interactionSystem = null;
        private PlacerSystem _placerSystem = null;
        private InputControlSystem _inputControlSystem = null;
        
        private DirectorData _currentData;
        private int _activeSequencePart = 0;
        
        public void Link()
        {
            _daySystem = Linker.Instance.DaySystem;
            _uiSystem = Linker.Instance.UISystem;
            _animationSystem = Linker.Instance.AnimationSystem;
            _interactionSystem = Linker.Instance.InteractionSystem;
            _placerSystem = Linker.Instance.PlacerSystem;
            _inputControlSystem =  Linker.Instance.InputControlSystem;
            
            _daySystem.OnDayStateChangedDelegate += OnDayStateChanged;
            OnSequencePartCompleted += OnSequencePartCompletedSignature;
        }

        private void OnDayStateChanged(EDayState eDayState, int currentDay)
        {
            var hasChanges = false;
            
            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Day == currentDay && config[i].DayState == eDayState)
                {
                    _currentData = config[i];
                    hasChanges = true;
                    break;
                }
            }

            if (hasChanges)
            {
                RunSequence();
            }
        }

        private void RunSequence()
        {
            if (_currentData.Sequence.Length > _activeSequencePart)
            {
                var id = _currentData.Sequence[_activeSequencePart];
                var prefix = id[..3];
                
                switch (prefix)
                {
                    case "DI_":
                        _uiSystem.Execute(id, OnSequencePartCompleted);
                        break;
                    case "AN_":
                        _animationSystem.Execute(id, OnSequencePartCompleted);
                        break;
                    case "IN_":
                        _interactionSystem.Execute(id, OnSequencePartCompleted);
                        break;
                    case "DA_":
                        _daySystem.Execute(id, OnSequencePartCompleted);
                        break;
                    case "PL_":
                        _placerSystem.Execute(id, OnSequencePartCompleted);
                        break;
                    case "IC_":
                        _inputControlSystem.Execute(id, OnSequencePartCompleted);
                        break;
                    default:
                        Debug.LogError($"{this.name} id prefix does not exist!");
                        break;
                }
            }
            else
            {
                CompleteSequence();
            }
        }

        private void OnSequencePartCompletedSignature()
        {
            _activeSequencePart++;
            RunSequence();
        }
        
        private void CompleteSequence()
        {
            _activeSequencePart = 0;
            Debug.Log("Complete sequence");
        }
    }
}