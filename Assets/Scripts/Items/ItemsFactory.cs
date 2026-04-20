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
