using UnityEngine;

namespace Items
{
    public class MainRecipeDisplaySlot : MonoBehaviour
    {
        [SerializeField] private Transform recipeSocket;

        private MainRecipeItem currentRecipe;

        public MainRecipeItem CurrentRecipe => currentRecipe;
        public bool HasRecipe => currentRecipe != null;

        public bool TryAttach(MainRecipeItem recipe)
        {
            if (recipe == null) return false;
            if (currentRecipe != null) return false;

            currentRecipe = recipe;
            var socket = recipeSocket != null ? recipeSocket : transform;
            recipe.gameObject.SetActive(true);
            recipe.transform.SetParent(socket);
            recipe.transform.position = socket.position;
            recipe.transform.rotation = socket.rotation;
            recipe.SetWorldRenderLayer();
            recipe.RefreshVisuals();
            return true;
        }

        public MainRecipeItem Detach()
        {
            var recipe = currentRecipe;
            currentRecipe = null;
            return recipe;
        }

        public void RefreshVisual()
        {
            if (currentRecipe == null) return;
            currentRecipe.gameObject.SetActive(true);
            currentRecipe.RefreshVisuals();
        }

        public void ClearAndDestroy()
        {
            if (currentRecipe != null)
            {
                Destroy(currentRecipe.gameObject);
                currentRecipe = null;
            }
        }
    }
}
