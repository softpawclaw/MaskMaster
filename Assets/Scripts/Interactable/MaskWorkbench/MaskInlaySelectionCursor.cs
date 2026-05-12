using System;
using System.Collections.Generic;
using DB;
using Enums;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Временный "ходячий" preview-объект этапа инкрустации.
    /// Он создаётся при входе в InlaySelection, собирает уникальные ресурсы с подноса,
    /// показывает текущий вариант в выбранном сокете и при Select копирует выбор в сокет маски.
    /// </summary>
    public class MaskInlaySelectionCursor : MonoBehaviour
    {
        [Serializable]
        private struct RuntimeOption
        {
            public bool IsClear;
            public ResourceType ResourceType;
            public DBInlayVisual.InlayVisualData VisualData;
            public GameObject PreviewInstance;
        }

        [SerializeField] private GameObject clearSocketPreviewPrefab;

        private readonly List<RuntimeOption> options = new();
        private Transform currentAnchor;
        private int selectedIndex;

        public bool HasOptions => options.Count > 0;
        public bool IsClearSelected => HasOptions && options[selectedIndex].IsClear;
        public ResourceType SelectedResourceType => HasOptions ? options[selectedIndex].ResourceType : ResourceType.None;
        public DBInlayVisual.InlayVisualData SelectedVisualData => HasOptions ? options[selectedIndex].VisualData : default;

        public void Init(DBInlayVisual dbInlayVisual, IEnumerable<ResourceType> resourceTypes, GameObject clearPreviewPrefab)
        {
            ClearOptions();
            clearSocketPreviewPrefab = clearPreviewPrefab;
            selectedIndex = 0;

            AddClearOption();

            if (dbInlayVisual != null && resourceTypes != null)
            {
                foreach (ResourceType resourceType in resourceTypes)
                {
                    if (resourceType == ResourceType.None)
                        continue;

                    if (!dbInlayVisual.TryGetData(resourceType, out DBInlayVisual.InlayVisualData visualData))
                        continue;

                    AddResourceOption(resourceType, visualData);
                }
            }

            RefreshActivePreview();
        }

        public void AttachTo(Transform anchor)
        {
            currentAnchor = anchor != null ? anchor : transform;
            transform.SetParent(currentAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            RefreshActivePreview();
        }

        public void ShowNext()
        {
            if (options.Count == 0)
                return;

            selectedIndex++;
            if (selectedIndex >= options.Count)
                selectedIndex = 0;

            RefreshActivePreview();
        }

        public void ShowPrevious()
        {
            if (options.Count == 0)
                return;

            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = options.Count - 1;

            RefreshActivePreview();
        }

        private void AddClearOption()
        {
            RuntimeOption option = new()
            {
                IsClear = true,
                ResourceType = ResourceType.None,
                VisualData = default,
                PreviewInstance = clearSocketPreviewPrefab != null ? Instantiate(clearSocketPreviewPrefab, transform) : null
            };

            if (option.PreviewInstance != null)
                ResetPreviewTransform(option.PreviewInstance.transform);

            options.Add(option);
        }

        private void AddResourceOption(ResourceType resourceType, DBInlayVisual.InlayVisualData visualData)
        {
            GameObject prefab = visualData.GetPrefab();

            RuntimeOption option = new()
            {
                IsClear = false,
                ResourceType = resourceType,
                VisualData = visualData,
                PreviewInstance = prefab != null ? Instantiate(prefab, transform) : null
            };

            if (option.PreviewInstance != null)
            {
                ResetPreviewTransform(option.PreviewInstance.transform);
                ApplyMaterial(option.PreviewInstance, visualData.GetPreviewMaterial());
            }

            options.Add(option);
        }

        private void RefreshActivePreview()
        {
            for (int i = 0; i < options.Count; i++)
            {
                GameObject preview = options[i].PreviewInstance;
                if (preview == null)
                    continue;

                preview.SetActive(i == selectedIndex);
                if (i == selectedIndex)
                    ResetPreviewTransform(preview.transform);
            }
        }

        private void ClearOptions()
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].PreviewInstance != null)
                    Destroy(options[i].PreviewInstance);
            }

            options.Clear();
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int m = 0; m < materials.Length; m++)
                    materials[m] = material;

                renderer.sharedMaterials = materials;
            }
        }

        private static void ResetPreviewTransform(Transform target)
        {
            if (target == null)
                return;

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }
    }
}
