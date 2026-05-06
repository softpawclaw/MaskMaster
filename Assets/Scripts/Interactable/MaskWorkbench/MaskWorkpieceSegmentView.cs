using System.Collections.Generic;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class MaskWorkpieceSegmentView : MonoBehaviour
    {
        [SerializeField] private MaskSegment segment = MaskSegment.Middle;
        [SerializeField] private GameObject root;
        [SerializeField] private Transform selectionAnchor;
        [SerializeField] private GameObject markedForCutVisual;
        [SerializeField] private MeshRenderer blankMeshRenderer;
        [SerializeField] private Material shapePreviewMaterial;
        [SerializeField] private Material shapeSolidMaterial;
        [SerializeField] private List<MaskWorkpieceShapeVariant> shapeVariants = new();

        public MaskSegment Segment => segment;
        public Transform SelectionAnchor => selectionAnchor != null ? selectionAnchor : transform;
        public IReadOnlyList<MaskWorkpieceShapeVariant> ShapeVariants => shapeVariants;

        private void Reset()
        {
            root = gameObject;
            selectionAnchor = transform;
            CacheBlankMeshRenderer();
            CollectShapeVariantsFromChildren();
        }

        private void Awake()
        {
            CacheBlankMeshRenderer();
        }

        private void OnValidate()
        {
            if (root == null)
                root = gameObject;

            CacheBlankMeshRenderer();
        }

        public void ConfigureFallback(MaskSegment fallbackSegment)
        {
            segment = fallbackSegment;
            if (root == null) root = gameObject;
            if (selectionAnchor == null) selectionAnchor = transform;
            CacheBlankMeshRenderer();
        }

        private void CacheBlankMeshRenderer()
        {
            if (blankMeshRenderer != null)
                return;

            if (root != null)
                blankMeshRenderer = root.GetComponent<MeshRenderer>();

            if (blankMeshRenderer == null)
                blankMeshRenderer = GetComponent<MeshRenderer>();
        }

        public void CollectShapeVariantsFromChildren()
        {
            shapeVariants.Clear();
            GetComponentsInChildren(true, shapeVariants);
        }

        public int ShapeVariantCount => shapeVariants != null && shapeVariants.Count > 0 ? shapeVariants.Count : 1;

        public MaskWorkpieceShapeVariant GetShapeVariant(int index)
        {
            if (shapeVariants == null || shapeVariants.Count == 0)
                return null;

            index = Mathf.Clamp(index, 0, shapeVariants.Count - 1);
            return shapeVariants[index];
        }

        public void EnsureRootActive()
        {
            // MaskPartRoot is a container for cut markers, shape variants and sockets.
            // Do not disable it while switching workbench stages, otherwise child variants cannot be displayed.
            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void SetBlankMeshVisible(bool visible)
        {
            EnsureRootActive();
            CacheBlankMeshRenderer();

            if (blankMeshRenderer != null)
                blankMeshRenderer.enabled = visible;
        }

        public void SetRootVisible(bool visible)
        {
            // Legacy entry point: keep the root alive and only toggle the blank mesh renderer.
            SetBlankMeshVisible(visible);
        }

        public void SetMarkedForCut(bool marked)
        {
            if (markedForCutVisual != null)
                markedForCutVisual.SetActive(marked);
        }

        public void RefreshShape(int selectedShapeIndex, bool showShape, bool showSockets, MaskWorkpiece owner, bool solidShape = false)
        {
            EnsureRootActive();

            if (shapeVariants == null || shapeVariants.Count == 0)
                return;

            selectedShapeIndex = Mathf.Clamp(selectedShapeIndex, 0, shapeVariants.Count - 1);

            for (int i = 0; i < shapeVariants.Count; i++)
            {
                MaskWorkpieceShapeVariant variant = shapeVariants[i];
                if (variant == null)
                    continue;

                bool active = showShape && i == selectedShapeIndex;
                variant.SetActive(active);

                if (active)
                {
                    Material material = solidShape ? shapeSolidMaterial : shapePreviewMaterial;
                    variant.ApplyVisualMaterial(material);
                }

                IReadOnlyList<MaskWorkpieceSocketView> sockets = variant.Sockets;
                for (int s = 0; s < sockets.Count; s++)
                {
                    MaskWorkpieceSocketView socket = sockets[s];
                    if (socket == null)
                        continue;

                    socket.SetVisible(active && showSockets);
                    if (active)
                    {
                        bool selected = owner != null && owner.IsSocketCurrentlySelected(Segment, socket.Socket);
                        bool planned = owner != null && owner.HasPlannedInlay(Segment, socket.Socket);
                        socket.Refresh(selected, planned);
                    }
                }
            }
        }
        public void SolidifyShape(int selectedShapeIndex)
        {
            EnsureRootActive();

            if (shapeVariants == null || shapeVariants.Count == 0)
                return;

            selectedShapeIndex = Mathf.Clamp(selectedShapeIndex, 0, shapeVariants.Count - 1);

            for (int i = 0; i < shapeVariants.Count; i++)
            {
                MaskWorkpieceShapeVariant variant = shapeVariants[i];
                if (variant == null)
                    continue;

                bool active = i == selectedShapeIndex;
                variant.SetActive(active);

                if (active)
                    variant.ApplyVisualMaterial(shapeSolidMaterial);
            }
        }

    }
}
