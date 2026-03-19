using DB;
using UnityEngine;

namespace Items
{
    public class ItemsFactory : MonoBehaviour
    {
        [SerializeField] private ItemBase recipeGameObject;

        public ItemBase GetRecipe(DBMask.MaskData maskData)
        {
            //TODO instance! + initialize! + items pool?
            return Instantiate<ItemBase>(recipeGameObject);
        }
    }
}