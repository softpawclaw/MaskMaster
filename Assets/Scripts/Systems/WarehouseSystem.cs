using System.Collections.Generic;
using DB;
using Enums;
using Global;
using UnityEngine;

namespace Systems
{
    public class WarehouseSystem : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DBWarehouse dbWarehouse;
        [SerializeField] private bool refreshOnDayStart = true;

        [Header("Boxes")]
        [SerializeField] private Interactable.WarehouseBox[] boxes;

        private readonly Dictionary<string, Interactable.WarehouseBox> boxesById = new();
        private bool subscribedToDaySystem;

        private void Awake()
        {
            BuildBoxIndex();
        }

        private void OnEnable()
        {
            TrySubscribeToDaySystem();
        }

        private void Start()
        {
            TrySubscribeToDaySystem();
        }

        private void OnDisable()
        {
            var linker = Linker.Instance;
            if (subscribedToDaySystem && linker != null && linker.DaySystem != null)
                linker.DaySystem.OnDayStateChangedDelegate -= OnDayStateChanged;

            subscribedToDaySystem = false;
        }


        private void TrySubscribeToDaySystem()
        {
            if (subscribedToDaySystem)
                return;

            var linker = Linker.Instance;
            if (linker == null || linker.DaySystem == null)
                return;

            linker.DaySystem.OnDayStateChangedDelegate += OnDayStateChanged;
            subscribedToDaySystem = true;
        }

        public void RefreshForCurrentDay()
        {
            var linker = Linker.Instance;
            int day = linker != null && linker.DaySystem != null ? linker.DaySystem.CurrentDay : 0;
            RefreshForDay(day);
        }

        public void RefreshForDay(int day)
        {
            if (dbWarehouse == null)
                dbWarehouse = Linker.Instance != null ? Linker.Instance.DBWarehouse : null;

            ResetAllBoxes();

            if (dbWarehouse == null)
            {
                Debug.LogError("WarehouseSystem: DBWarehouse is not assigned.");
                return;
            }

            if (!dbWarehouse.TryGetData(day, out DBWarehouse.WarehouseDayData data))
            {
                Debug.LogWarning($"WarehouseSystem: no warehouse config for day {day}. All boxes were reset.");
                return;
            }

            ApplyData(data);
        }

        private void OnDayStateChanged(EDayState state, int day)
        {
            if (!refreshOnDayStart)
                return;

            if (state == EDayState.Start)
                RefreshForDay(day);
        }

        private void BuildBoxIndex()
        {
            boxesById.Clear();

            if (boxes == null)
                return;

            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];
                if (box == null)
                {
                    Debug.LogWarning($"WarehouseSystem: box at index {i} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(box.BoxId))
                {
                    Debug.LogWarning($"WarehouseSystem: box at index {i} has empty BoxId.");
                    continue;
                }

                if (boxesById.ContainsKey(box.BoxId))
                {
                    Debug.LogError($"WarehouseSystem: duplicate BoxId '{box.BoxId}'. Only first box will be used.");
                    continue;
                }

                boxesById.Add(box.BoxId, box);
            }
        }

        private void ResetAllBoxes()
        {
            BuildBoxIndex();

            foreach (var pair in boxesById)
                pair.Value.ResetBox();
        }

        private void ApplyData(DBWarehouse.WarehouseDayData data)
        {
            var usedBoxIds = new HashSet<string>();

            if (data.Resources == null)
                return;

            for (int i = 0; i < data.Resources.Length; i++)
            {
                ResourceType type = data.Resources[i].ResourceType;
                string[] boxIds = data.Resources[i].BoxIds;

                if (boxIds == null || boxIds.Length == 0)
                {
                    Debug.LogWarning($"WarehouseSystem: day {data.Day}, resource '{type}' has empty BoxIds.");
                    continue;
                }

                Sprite icon = ResolveIcon(type);

                for (int j = 0; j < boxIds.Length; j++)
                {
                    string boxId = boxIds[j];
                    if (string.IsNullOrWhiteSpace(boxId))
                    {
                        Debug.LogWarning($"WarehouseSystem: day {data.Day}, resource '{type}' has empty BoxId at index {j}.");
                        continue;
                    }

                    if (!boxesById.TryGetValue(boxId, out var box) || box == null)
                    {
                        Debug.LogError($"WarehouseSystem: day {data.Day}, resource '{type}' references missing box '{boxId}'.");
                        continue;
                    }

                    if (!usedBoxIds.Add(boxId))
                    {
                        Debug.LogError($"WarehouseSystem: day {data.Day}, box '{boxId}' is configured more than once. Last init will overwrite previous state.");
                    }

                    box.Init(type, icon);
                }
            }
        }

        private Sprite ResolveIcon(ResourceType type)
        {
            if (type == ResourceType.None)
                return null;

            var linker = Linker.Instance;
            if (linker == null)
            {
                Debug.LogWarning($"WarehouseSystem: Linker is not available, cannot resolve icon for '{type}'.");
                return null;
            }

            if (linker.DBInlayVisual != null && linker.DBInlayVisual.TryGetData(type, out var inlayData))
                return inlayData.Icon;

            if (linker.DBMistResistance != null)
            {
                var allMist = linker.DBMistResistance.GetAll();
                for (int i = 0; i < allMist.Length; i++)
                {
                    if (allMist[i].ResourceType == type)
                        return allMist[i].Image;
                }
            }

            Debug.LogWarning($"WarehouseSystem: icon for resource '{type}' was not found in DBInlayVisual or DBMistResistance.");
            return null;
        }
    }
}
