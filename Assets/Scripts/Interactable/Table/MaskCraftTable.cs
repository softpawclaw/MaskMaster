using System.Collections.Generic;
using DB;
using Enums;
using Global;
using Helpers;
using Items;
using UnityEngine;

namespace Interactable.Table
{
    public class MaskCraftTable : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private ComplexItemPlaceHolder recipeHolder;
        [SerializeField] private ComplexItemPlaceHolder trayHolder;

        [Header("Controls")]
        [SerializeField] private GameObject craftActionRoot;

        [Header("Output")]
        [SerializeField] private MaskOutputInteractable outputInteractable;

        private ItemsFactory itemsFactory;

        public void Link()
        {
            itemsFactory = Linker.Instance.ItemsFactory;

            if (recipeHolder != null)
                recipeHolder.OnContentChanged += RefreshState;

            if (trayHolder != null)
                trayHolder.OnContentChanged += RefreshState;

            if (outputInteractable != null)
                outputInteractable.OnItemChanged += RefreshState;

            RefreshState();
        }

        public void RefreshState()
        {
            bool canCraft = CanCraft();
            bool hasOutputItem = outputInteractable != null && outputInteractable.HasItem;

            if (craftActionRoot != null)
                craftActionRoot.SetActive(canCraft);

            if (outputInteractable != null)
                outputInteractable.gameObject.SetActive(hasOutputItem);
        }

        public bool CanCraft()
        {
            if (outputInteractable != null && outputInteractable.HasItem)
                return false;

            if (!TryGetMainRecipe(out var recipe))
                return false;

            if (!TryGetTrayResources(out var trayResources))
                return false;

            return HasRequiredTopLevelResources(recipe.MaskData, trayResources);
        }

        public bool TryCraft(GameObject interactor)
        {
            if (!TryGetMainRecipe(out var recipe))
            {
                Debug.Log("MaskCraftTable: no main recipe in recipe holder.");
                RefreshState();
                return false;
            }

            if (!TryGetTrayResources(out var trayResources))
            {
                Debug.Log("MaskCraftTable: no resources in tray holder.");
                RefreshState();
                return false;
            }

            if (!HasRequiredTopLevelResources(recipe.MaskData, trayResources))
            {
                Debug.Log("MaskCraftTable: tray resources do not match required top-level recipe groups.");
                RefreshState();
                return false;
            }

            var actualMaskData = BuildActualMaskData(recipe.MaskData, trayResources);

            for (int i = 0; i < trayResources.Count; i++)
            {
                if (trayResources[i] == null) continue;
                Destroy(trayResources[i].gameObject);
            }

            // MVP-заглушка:
            // после успешного крафта принудительно вычищаем holder рецептов,
            // удаляя и paper stack, и вложенные recipe pages/children.
            if (recipeHolder != null)
            {
                recipeHolder.EmergencyClearAndDestroy();
            }

            var mask = itemsFactory.CreateMask(recipe.MaskData, actualMaskData);
            outputInteractable.SetItem(mask);

            RefreshState();
            return true;
        }

        private bool TryGetMainRecipe(out MainRecipeItem recipe)
        {
            recipe = null;

            if (recipeHolder == null)
                return false;

            var recipes = recipeHolder.GetComponentsInChildren<MainRecipeItem>(true);
            if (recipes == null || recipes.Length == 0)
                return false;

            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i] == null)
                    continue;

                recipe = recipes[i];
                return true;
            }

            return false;
        }

        private bool TryGetTrayResources(out List<ResourceItem> trayResources)
        {
            trayResources = new List<ResourceItem>();

            if (trayHolder == null)
                return false;

            var tray = trayHolder.CurrentItem as TrayItem;
            if (tray == null)
                return false;

            var resources = trayHolder.GetComponentsInChildren<ResourceItem>(true);
            if (resources == null || resources.Length == 0)
                return false;

            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i] == null)
                    continue;

                trayResources.Add(resources[i]);
            }

            return trayResources.Count > 0;
        }

        private bool HasRequiredTopLevelResources(DBMask.MaskData targetMaskData, List<ResourceItem> trayResources)
        {
            if (trayResources == null || trayResources.Count == 0)
                return false;

            int requiredBlankCount = targetMaskData.Material != ResourceType.None ? 1 : 0;
            int requiredInlayCount = targetMaskData.Sockets != null ? targetMaskData.Sockets.Length : 0;

            int actualBlankCount = 0;
            int actualInlayCount = 0;

            for (int i = 0; i < trayResources.Count; i++)
            {
                var resource = trayResources[i];
                if (resource == null)
                    continue;

                var type = resource.Type;

                if (ResourceTypeHelper.IsBlank(type))
                {
                    actualBlankCount++;
                    continue;
                }

                if (ResourceTypeHelper.IsInlay(type))
                {
                    actualInlayCount++;
                }
            }

            if (actualBlankCount != requiredBlankCount)
                return false;

            if (actualInlayCount != requiredInlayCount)
                return false;

            return true;
        }

        private DBMask.MaskData BuildActualMaskData(DBMask.MaskData targetMaskData, List<ResourceItem> trayResources)
        {
            ResourceType actualMaterial = ResourceType.None;

            int socketCount = targetMaskData.Sockets != null ? targetMaskData.Sockets.Length : 0;
            var actualSockets = new DBMask.MaskSocketResource[socketCount];

            int socketWriteIndex = 0;

            for (int i = 0; i < trayResources.Count; i++)
            {
                var resource = trayResources[i];
                if (resource == null) continue;

                var resourceType = resource.Type;

                if (ResourceTypeHelper.IsBlank(resourceType))
                {
                    if (actualMaterial == ResourceType.None)
                        actualMaterial = resourceType;

                    continue;
                }

                if (!ResourceTypeHelper.IsInlay(resourceType))
                    continue;

                if (socketWriteIndex >= actualSockets.Length)
                    continue;

                actualSockets[socketWriteIndex] = new DBMask.MaskSocketResource(
                    targetMaskData.Sockets[socketWriteIndex].Socket,
                    resourceType
                );

                socketWriteIndex++;
            }

            return new DBMask.MaskData(
                targetMaskData.Id,
                targetMaskData.OR_Id,
                targetMaskData.Size,
                actualMaterial,
                actualSockets
            );
        }

        private void OnDestroy()
        {
            if (recipeHolder != null)
                recipeHolder.OnContentChanged -= RefreshState;

            if (trayHolder != null)
                trayHolder.OnContentChanged -= RefreshState;

            if (outputInteractable != null)
                outputInteractable.OnItemChanged -= RefreshState;
        }
    }
}