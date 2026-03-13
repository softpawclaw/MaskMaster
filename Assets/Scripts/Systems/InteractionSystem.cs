using System;
using UnityEngine;

namespace Systems
{
    public class InteractionSystem : ExecuteSystemBase
    {
        [Serializable]
        public struct InteractionData
        {
            public string Id;
            public Interactable.Interactable Interactable;
            public GameObject Interactor;

            public InteractionData(string id, Interactable.Interactable interactable, GameObject interactor)
            {
                Id = id;
                Interactable = interactable;
                Interactor = interactor;
            }
        }
        
        [SerializeField] private InteractionData[] config;
        
        public override void Execute(string id, Action completeAction)
        {
            for (int i = 0; i < config.Length; i++)
            {
                if (id == config[i].Id)
                {
                    config[i].Interactable.Interact(config[i].Interactor, completeAction);
                    break;
                }
            }
        }
    }
}