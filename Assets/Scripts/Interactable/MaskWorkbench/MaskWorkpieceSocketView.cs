using DB;
using Enums;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class MaskWorkpieceSocketView : MonoBehaviour
    {
        [SerializeField] private MaskSocket socket = MaskSocket.None;
        [SerializeField] private Transform selectionAnchor;
        [SerializeField] private Transform inlayRoot;
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private GameObject selectedVisual;
        [SerializeField] private GameObject plannedVisual;

        private ResourceType plannedResourceType = ResourceType.None;
        private GameObject runtimeInlayVisual;
        private DBInlayVisual.InlayVisualData plannedVisualData;
        private bool isSolid;

        public MaskSocket Socket => socket;
        public Transform SelectionAnchor => selectionAnchor != null ? selectionAnchor : transform;
        public Transform InlayRoot => inlayRoot != null ? inlayRoot : transform;
        public ResourceType PlannedResourceType => plannedResourceType;
        public bool HasPlannedInlay => plannedResourceType != ResourceType.None && runtimeInlayVisual != null;
        public bool IsSolid => isSolid;

        private void Reset()
        {
            selectionAnchor = transform;
            inlayRoot = transform;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Refresh(bool selected, bool planned)
        {
            if (emptyVisual != null)
                emptyVisual.SetActive(!planned);

            if (selectedVisual != null)
                selectedVisual.SetActive(selected);

            if (plannedVisual != null)
                plannedVisual.SetActive(planned);
        }

        public void SetPlannedInlay(ResourceType resourceType, DBInlayVisual.InlayVisualData visualData)
        {
            ClearPlannedInlay();

            if (resourceType == ResourceType.None)
                return;

            plannedResourceType = resourceType;
            plannedVisualData = visualData;
            isSolid = false;

            GameObject prefab = visualData.GetPrefab();
            if (prefab == null)
                return;

            runtimeInlayVisual = Instantiate(prefab, InlayRoot);
            ResetRuntimeVisualTransform(runtimeInlayVisual.transform);
            ApplyMaterial(runtimeInlayVisual, visualData.GetPreviewMaterial());
            runtimeInlayVisual.SetActive(true);
        }

        public void ClearPlannedInlay()
        {
            plannedResourceType = ResourceType.None;
            plannedVisualData = default;
            isSolid = false;

            if (runtimeInlayVisual != null)
                Destroy(runtimeInlayVisual);

            runtimeInlayVisual = null;
        }

        public void SolidifyPlannedInlay()
        {
            if (plannedResourceType == ResourceType.None)
                return;

            if (runtimeInlayVisual == null)
            {
                GameObject prefab = plannedVisualData.GetPrefab();
                if (prefab == null)
                    return;

                runtimeInlayVisual = Instantiate(prefab, InlayRoot);
                ResetRuntimeVisualTransform(runtimeInlayVisual.transform);
                runtimeInlayVisual.SetActive(true);
            }

            ApplyMaterial(runtimeInlayVisual, plannedVisualData.GetSolidMaterial());
            isSolid = true;
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

        private static void ResetRuntimeVisualTransform(Transform target)
        {
            if (target == null)
                return;

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }
    }
}
