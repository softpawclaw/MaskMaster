using System;
using Enums;
using UnityEngine;

namespace DB
{
    public class DBQuest : MonoBehaviour
    {
        [Serializable]
        public struct QuestStateData
        {
            public QuestState State;
            public string[] DI_Id;

            public QuestStateData(QuestState state, string[] diId)
            {
                State = state;
                DI_Id = diId;
            }
        }
        
        [Serializable]
        public struct QuestData
        {
            public string Id;
            public string OR_Id;
            public QuestStateData[] States;

            public QuestData(string id, string orId, QuestStateData[] states)
            {
                Id = id;
                OR_Id = orId;
                States = states;
            }
        }

        [SerializeField] private QuestData[] config;
        
        public bool TryGetQuestDataByOrderId(string orId, out QuestData result)
        {
            result = default;

            if (string.IsNullOrEmpty(orId))
            {
                Debug.LogError("[QuestConfig] OR_Id is null or empty.");
                return false;
            }

            if (config == null || config.Length == 0)
            {
                Debug.LogError("[QuestConfig] Quest config array is null or empty.");
                return false;
            }

            for (int i = 0; i < config.Length; i++)
            {
                if (string.IsNullOrEmpty(config[i].OR_Id))
                {
                    Debug.LogWarning($"[QuestConfig] QuestData at index {i} has empty OR_Id. QuestId: {config[i].Id}");
                    continue;
                }

                if (config[i].OR_Id == orId)
                {
                    result = config[i];
                    return true;
                }
            }

            Debug.LogError($"[QuestConfig] QuestData not found for OR_Id: {orId}");
            return false;
        }
    }
}