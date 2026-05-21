using DB;
using Enums;
using Global;
using UnityEngine;

namespace Items
{
    public class ItemsFactory : MonoBehaviour
    {
        [SerializeField] private MainRecipeItem mainRecipePrefab;
        [SerializeField] private CatalogPageItem catalogPagePrefab;
        [SerializeField] private PaperStackItem paperStackItem;
        [SerializeField] private MaskItem maskItemPrefab;
        [SerializeField] private TrayItem trayItemPrefab;

        [System.Serializable]
        private struct ResourcePrefabData
        {
            public ResourceType ResourceType;
            public ResourceItem Prefab;
        }

        [Header("Resources")]
        [SerializeField] private ResourcePrefabData[] resourcePrefabs;

        public ResourceItem CreateResource(ResourceType type)
        {
            if (type == ResourceType.None)
            {
                Debug.LogWarning("ItemsFactory: refused to create ResourceType.None.");
                return null;
            }

            if (resourcePrefabs == null || resourcePrefabs.Length == 0)
            {
                Debug.LogError($"ItemsFactory: resourcePrefabs is empty, cannot create resource '{type}'.");
                return null;
            }

            for (int i = 0; i < resourcePrefabs.Length; i++)
            {
                if (resourcePrefabs[i].ResourceType != type)
                    continue;

                if (resourcePrefabs[i].Prefab == null)
                {
                    Debug.LogError($"ItemsFactory: prefab for resource '{type}' is not assigned.");
                    return null;
                }

                var instance = Instantiate(resourcePrefabs[i].Prefab);
                if (instance.Type != type)
                    Debug.LogWarning($"ItemsFactory: created resource prefab for key '{type}', but prefab has ResourceItem.Type='{instance.Type}'. Check prefab config.");

                return instance;
            }

            Debug.LogError($"ItemsFactory: no resource prefab configured for '{type}'.");
            return null;
        }

        public TrayItem CreateTray()
        {
            if (trayItemPrefab == null)
            {
                Debug.LogError("ItemsFactory: trayItemPrefab is not assigned.");
                return null;
            }

            return Instantiate(trayItemPrefab);
        }

        public PaperStackItem CreatePaperStack()
        {
            return Instantiate(paperStackItem);
        }

        public MainRecipeItem CreateMainRecipe(DBMask.MaskData maskData)
        {
            var instance = Instantiate(mainRecipePrefab);
            instance.Init(maskData);
            return instance;
        }

        public CatalogPageItem CreateCatalogPage(CatalogPageData data)
        {
            var instance = Instantiate(catalogPagePrefab);
            ResolveAndInitCatalogPage(instance, data);
            return instance;
        }

        public MaskItem CreateMask(DBMask.MaskData targetMaskData, DBMask.MaskData actualMaskData)
        {
            var instance = Instantiate(maskItemPrefab);
            instance.Init(targetMaskData, actualMaskData);
            return instance;
        }

        public MaskItem CreateMaskForCraft(DBMask.MaskData targetMaskData)
        {
            var instance = Instantiate(maskItemPrefab);
            instance.InitForCraft(targetMaskData);
            return instance;
        }

        private static void ResolveAndInitCatalogPage(CatalogPageItem instance, CatalogPageData data)
        {
            if (instance == null)
                return;

            Linker linker = Linker.Instance;
            if (linker == null)
            {
                instance.Init(data);
                Debug.LogWarning($"ItemsFactory: Linker.Instance is null while creating catalog page '{data.PageId}'. Initialized with base data only.");
                return;
            }

            switch (data.PageKind)
            {
                case CatalogPageKind.MistResistance:
                    if (linker.DBMistResistance != null && linker.DBMistResistance.TryGetData(data.PageId, out var mistData))
                    {
                        instance.Init(data, mistData);
                        return;
                    }
                    break;

                case CatalogPageKind.FaceCover:
                    if (linker.DBFaceCover != null && linker.DBFaceCover.TryGetData(data.PageId, out var faceCoverData))
                    {
                        instance.Init(data, faceCoverData);
                        return;
                    }
                    break;

                case CatalogPageKind.District:
                    if (linker.DBDistrict != null)
                    {
                        var districtRows = linker.DBDistrict.GetAll();
                        for (int i = 0; i < districtRows.Length; i++)
                        {
                            if (districtRows[i].Id == data.PageId)
                            {
                                instance.Init(data, districtRows[i]);
                                return;
                            }
                        }
                    }
                    break;

                case CatalogPageKind.Faction:
                    if (linker.DBFaction != null)
                    {
                        var factionRows = linker.DBFaction.GetAll();
                        for (int i = 0; i < factionRows.Length; i++)
                        {
                            if (factionRows[i].Id == data.PageId)
                            {
                                instance.Init(data, factionRows[i]);
                                return;
                            }
                        }
                    }
                    break;
            }

            instance.Init(data);
            Debug.LogWarning($"ItemsFactory: failed to resolve catalog page data for page '{data.PageId}' of kind '{data.PageKind}'. Initialized with base data only.");
        }
    }
}
