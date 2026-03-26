using DB;
using Enums;
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

        public CatalogPageItem CreateCatalogPage(ResourceType resourceType)
        {
            var instance = Instantiate(catalogPagePrefab);
            instance.Init(resourceType);
            return instance;
        }

        public MaskItem CreateMask(DBMask.MaskData targetMaskData, DBMask.MaskData actualMaskData)
        {
            var instance = Instantiate(maskItemPrefab);
            instance.Init(targetMaskData, actualMaskData);
            return instance;
        }
    }
}