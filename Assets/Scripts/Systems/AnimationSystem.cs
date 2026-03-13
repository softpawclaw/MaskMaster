using System;
using UnityEngine;

namespace Systems
{
    public class AnimationSystem : ExecuteSystemBase
    {
        [Serializable]
        public struct AnimationData
        {
            public string Id;
            public string AnimKey;
            public AnimationTarget AnimTarget;

            public AnimationData(string id, string animKey, AnimationTarget  animTarget)
            {
                Id = id;
                AnimKey = animKey;
                AnimTarget = animTarget;
            }
        }
        
        [SerializeField] private AnimationData[] config;
        
        public override void Execute(string id, Action completeAction)
        {
            for (int i = 0; i < config.Length; i++)
            {
                if (id == config[i].Id)
                {
                    config[i].AnimTarget.PlayTrigger(config[i].AnimKey, completeAction);
                    break;
                }
            }
        }
    }
}