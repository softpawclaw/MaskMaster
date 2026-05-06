using System.Collections.Generic;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class MaskWorkpieceShapeVariant : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private List<MaskWorkpieceSocketView> sockets = new();
        [SerializeField] private List<Renderer> renderers = new();
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();

        public IReadOnlyList<MaskWorkpieceSocketView> Sockets => sockets;

        private void Reset()
        {
            visualRoot = gameObject;
            CollectSocketsFromChildren();
            CollectRenderersFromChildren();
        }

        private void OnValidate()
        {
            if (visualRoot == null)
                visualRoot = gameObject;
        }

        public void CollectSocketsFromChildren()
        {
            sockets.Clear();
            GetComponentsInChildren(true, sockets);
        }

        public void CollectRenderersFromChildren()
        {
            renderers.Clear();
            GetComponentsInChildren(true, renderers);
        }

        public void SetActive(bool active)
        {
            if (visualRoot != null)
                visualRoot.SetActive(active);
            else
                gameObject.SetActive(active);
        }

        public void ApplyVisualMaterial(Material material)
        {
            if (material == null)
            {
                RestoreOriginalMaterials();
                return;
            }

            EnsureRenderersAndOriginalMaterials();

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.sharedMaterial = material;
            }
        }

        public void RestoreOriginalMaterials()
        {
            EnsureRenderersAndOriginalMaterials();

            foreach (KeyValuePair<Renderer, Material[]> pair in originalMaterials)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.sharedMaterials = pair.Value;
            }
        }

        private void EnsureRenderersAndOriginalMaterials()
        {
            if (renderers == null || renderers.Count == 0)
                CollectRenderersFromChildren();

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || originalMaterials.ContainsKey(renderer))
                    continue;

                originalMaterials.Add(renderer, renderer.sharedMaterials);
            }
        }
    }
}
