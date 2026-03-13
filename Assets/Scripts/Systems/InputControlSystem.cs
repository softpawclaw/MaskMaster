using System;
using Global;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    public class InputControlSystem : ExecuteSystemBase
    {
        [Serializable]
        public struct InputControlData
        {
            public string Id;

            [Header("Player Control")]
            public bool EnablePlayerController;

            [Header("Input")]
            public bool SwitchActionMap;
            public string ActionMap;

            [Header("Cursor")]
            public bool ChangeCursor;
            public bool CursorVisible;
            public CursorLockMode CursorLockMode;
        }
        
        [SerializeField] private InputControlData[] config;

        private PlayerController playerController; 
        private PlayerInput playerInput;
        
        public void Link()
        {
            playerInput = Linker.Instance.PlayerInput;
            playerController = Linker.Instance.PlayerController;
        }

        public override void Execute(string id, Action completeAction)
        {
            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id != id)
                    continue;

                Apply(config[i]);
                completeAction?.Invoke();
                return;
            }

            Debug.LogError($"InputControlSystem: config '{id}' not found.");
            completeAction?.Invoke();
        }

        private void Apply(InputControlData data)
        {
            if (playerController != null)
                playerController.enabled = data.EnablePlayerController;

            if (playerInput != null && data.SwitchActionMap && !string.IsNullOrEmpty(data.ActionMap))
                playerInput.SwitchCurrentActionMap(data.ActionMap);

            if (data.ChangeCursor)
            {
                Cursor.visible = data.CursorVisible;
                Cursor.lockState = data.CursorLockMode;
            }
        }
    }
}