using UnityEngine;

namespace Interactable.Table
{
    public class MaskCraftActionInteractable : Interactable
    {
        [SerializeField] private MaskCraftTable table;

        protected override void OnInteract(GameObject interactor)
        {
            if (table != null)
            {
                table.TryCraft(interactor);
            }

            CompleteInteraction(interactor);
        }
    }
}