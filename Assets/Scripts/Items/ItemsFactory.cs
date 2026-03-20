using DB;
using UnityEngine;

namespace Items
{
    public class ItemsFactory : MonoBehaviour
    {
        [SerializeField] private ItemBase recipeMain;
        [SerializeField] private ItemBase recipePart;
        [SerializeField] private PaperStackItem paperStackItem;

        public PaperStackItem GetPaperStackItem()
        {
            return Instantiate<PaperStackItem>(paperStackItem);
        }

        public ItemBase GetRecipe(DBMask.MaskData maskData, bool isMain)
        {
            //TODO instance! + initialize! + items pool?
            
            if (isMain)
            {
                return Instantiate<ItemBase>(recipeMain);
            }

            return Instantiate<ItemBase>(recipePart);
        }
    }
}