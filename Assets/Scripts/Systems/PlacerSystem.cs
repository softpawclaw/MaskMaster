using System;
using UnityEngine;

namespace Systems
{
    public class PlacerSystem : ExecuteSystemBase
    {
        [Serializable]
        public struct PlacerData
        {
            public string Id;
            public GameObject Target;
            public Vector3 TargetPosition;
            public Vector3 TargetRotation;
        }

        [SerializeField] private PlacerData[] config;

        public override void Execute(string id, Action completeAction)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("PlacerSystem: Execute called with null or empty id.");
                completeAction?.Invoke();
                return;
            }

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id != id)
                    continue;

                if (config[i].Target == null)
                {
                    Debug.LogError($"PlacerSystem: Target is null for id '{id}'.");
                    completeAction?.Invoke();
                    return;
                }
                
                config[i].Target.transform.SetPositionAndRotation(config[i].TargetPosition
                    , Quaternion.Euler(config[i].TargetRotation));

                completeAction?.Invoke();
                return;
            }

            Debug.LogError($"PlacerSystem: Config with id '{id}' was not found.");
            completeAction?.Invoke();
        }
    }
}