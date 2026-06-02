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
        private DialogueHistorySystem dialogueHistorySystem = null;
        
        public void Link()
        {
            daySystem = Linker.Instance.DaySystem;
            dialogueHistorySystem = Linker.Instance.DialogueHistorySystem;
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
            for (int i = 0; i < config.Length; i++)
            {
                if (id == config[i].Id)
                {
                    DialogueData dialogue = config[i];

                    Action wrappedCompleteAction = () =>
                    {
                        dialogueHistorySystem?.MarkShown(dialogue.Id);
                        completeAction?.Invoke();
                    };

                    if (id.StartsWith("DI_F"))
                    {
                        controllerUI.ShowFader($"{dialogue.Text} {daySystem.CurrentDay}", wrappedCompleteAction);
                    }
                    else
                    {
                        controllerUI.ShowDialog(dialogue.Text, dialogue.Delay, wrappedCompleteAction);
                    }
                    
                    break;
                }
            }
        }
    }
}