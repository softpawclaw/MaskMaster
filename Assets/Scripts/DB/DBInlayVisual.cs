using System;
using Enums;
using UnityEngine;

namespace DB
{
    /// <summary>
    /// Общая база визуалов инкрустаций. Сокеты маски не хранят пачки вариантов,
    /// а получают нужный prefab по ResourceType из этой базы.
    ///
    /// Пайплайн использует один prefab инкрустации и переключает его состояние
    /// материалами Preview/Solid.
    /// </summary>
    public class DBInlayVisual : MonoBehaviour
    {
        [Serializable]
        public struct InlayVisualData
        {
            public ResourceType ResourceType;
            public Sprite Icon;
            public GameObject InlayPrefab;
            public Material PreviewMaterial;
            public Material SolidMaterial;

            public GameObject GetPrefab()
            {
                return InlayPrefab;
            }

            public Material GetPreviewMaterial()
            {
                return PreviewMaterial;
            }

            public Material GetSolidMaterial()
            {
                return SolidMaterial != null ? SolidMaterial : PreviewMaterial;
            }
        }

        [SerializeField] private InlayVisualData[] data = Array.Empty<InlayVisualData>();

        public bool TryGetData(ResourceType type, out InlayVisualData result)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].ResourceType != type)
                    continue;

                result = data[i];
                return true;
            }

            result = default;
            return false;
        }
    }
}
