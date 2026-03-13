using System;
using Global;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems
{
    public class UISystem : ExecuteSystemBase
    {
        [Serializable]
        public struct DialogueData
        {
            public string Id;
            public string Text;
            public float Delay;

            public DialogueData(string id, string text, float delay)
            {
                Id = id;
                Text = text;
                Delay = delay;
            }
        }
        
        [FormerlySerializedAs("dialogWindowUI")] [SerializeField] private ControllerUI controllerUI;
        [SerializeField] private DialogueData[] config;

        private DaySystem daySystem = null;
        
        public void Link()
        {
            daySystem = Linker.Instance.DaySystem;
        }

        private void Awake()
        {
            controllerUI = GetComponentInChildren<ControllerUI>();

            if (controllerUI == null)
            {
                Debug.LogError($"{this.name} dialogWindowUI is missing!");
            }
        }

        public override void Execute(string id, Action completeAction)
        {
            var prefix = id[..4];
            
            for (int i = 0; i < config.Length; i++)
            {
                if (id == config[i].Id)
                {
                    if (prefix == "DI_F")
                    {
                        controllerUI.ShowFader($"{config[i].Text} {daySystem.CurrentDay}", completeAction);
                    }
                    else
                    {
                        controllerUI.ShowDialog(config[i].Text, config[i].Delay, completeAction);
                    }
                    
                    break;
                }
            }
        }
    }
}