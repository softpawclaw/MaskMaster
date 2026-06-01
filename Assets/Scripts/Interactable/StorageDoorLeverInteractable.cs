using DB;
using Enums;
using Global;
using Helpers;
using Items;
using Player;
using Systems;
using UnityEngine;

namespace Interactable
{
    public class StorageDoorLeverInteractable : Interactable
    {
        private enum LeverMode
        {
            EnterStorage = 0,
            ExitStorage = 1
        }

        [Header("Lever")]
        [SerializeField] private LeverMode mode = LeverMode.EnterStorage;
        [SerializeField] private StorageDoorController storageDoorController;

        [Header("Fail Dialogues")]
        [SerializeField] private string[] failDialogIds;

        [Header("Debug")]
        [SerializeField] private bool logValidation = true;

        private UISystem uiSystem;

        private void Awake()
        {
            if (storageDoorController == null)
                storageDoorController = GetComponentInParent<StorageDoorController>();
        }

        private void Start()
        {
            TryResolveLinks();
        }

        protected override void OnInteract(GameObject interactor)
        {
            TryResolveLinks();

            var hands = interactor != null ? interactor.GetComponent<PlayerHandsController>() : null;
            if (hands == null)
            {
                Debug.LogWarning($"{name}: PlayerHandsController not found on interactor.");
                CompleteInteraction(interactor);
                return;
            }

            bool isValid = mode == LeverMode.EnterStorage
                ? ValidatePaperStack(hands, out string message)
                : ValidateTray(hands, out message);

            if (logValidation && !string.IsNullOrWhiteSpace(message))
                Debug.Log($"{name}: {message}");

            if (!isValid)
            {
                PlayFailDialogueOrComplete(interactor);
                return;
            }

            if (storageDoorController == null)
            {
                Debug.LogWarning($"{name}: StorageDoorController is not assigned.");
                CompleteInteraction(interactor);
                return;
            }

            storageDoorController.OpenDoor();
            CompleteInteraction(interactor);
        }

        private void TryResolveLinks()
        {
            if (uiSystem == null && Linker.Instance != null)
                uiSystem = Linker.Instance.UISystem;
        }

        private bool ValidatePaperStack(PlayerHandsController hands, out string message)
        {
            message = string.Empty;

            var stack = hands.GetFirstItemInHands<PaperStackItem>();
            if (stack == null)
            {
                message = "Storage entry denied: player has no paper stack.";
                return false;
            }

            MainRecipeItem recipe = FindMainRecipe(stack);
            if (recipe == null)
            {
                message = "Storage entry denied: paper stack has no main recipe.";
                return false;
            }

            int catalogPageCount = CountCatalogPages(stack);
            if (catalogPageCount < 4)
            {
                message = $"Storage entry denied: incomplete paper stack. Catalog pages={catalogPageCount}/4.";
                return false;
            }

            message = $"Storage entry allowed: paper stack has main recipe and catalog pages={catalogPageCount}/4.";
            return true;
        }

        private bool ValidateTray(PlayerHandsController hands, out string message)
        {
            message = string.Empty;

            var tray = hands.GetTrayInHands();
            if (tray == null)
            {
                message = "Storage exit denied: player has no tray.";
                return false;
            }

            MainRecipeItem recipe = FindMainRecipeInHands(hands);
            if (recipe == null)
            {
                message = "Storage exit denied: player has no main recipe to validate tray against.";
                return false;
            }

            int blankCount = 0;
            int inlayCount = 0;

            var items = tray.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not ResourceItem resource)
                    continue;

                if (ResourceTypeHelper.IsBlank(resource.Type))
                    blankCount++;
                else if (ResourceTypeHelper.IsInlay(resource.Type))
                    inlayCount++;
            }

            int requiredInlayCount = GetRequiredInlayCount(recipe);

            if (blankCount != 1)
            {
                message = $"Storage exit denied: expected exactly one blank, actual={blankCount}.";
                return false;
            }

            if (inlayCount < requiredInlayCount)
            {
                message = $"Storage exit denied: not enough inlays. required={requiredInlayCount}, actual={inlayCount}.";
                return false;
            }

            message = $"Storage exit allowed: blank={blankCount}, inlays={inlayCount}/{requiredInlayCount}.";
            return true;
        }

        private static MainRecipeItem FindMainRecipeInHands(PlayerHandsController hands)
        {
            var direct = hands.GetFirstItemInHands<MainRecipeItem>();
            if (direct != null)
                return direct;

            var stack = hands.GetFirstItemInHands<PaperStackItem>();
            return FindMainRecipe(stack);
        }

        private static MainRecipeItem FindMainRecipe(PaperStackItem stack)
        {
            if (stack == null)
                return null;

            var items = stack.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is MainRecipeItem recipe)
                    return recipe;
            }

            return null;
        }

        private static int CountCatalogPages(PaperStackItem stack)
        {
            if (stack == null)
                return 0;

            int count = 0;
            var items = stack.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is CatalogPageItem)
                    count++;
            }

            return count;
        }

        private static int GetRequiredInlayCount(MainRecipeItem recipe)
        {
            if (recipe == null)
                return 0;

            DBMaskCombination.MaskSegmentResource[] inlays = recipe.GetExpectedInlays();
            if (inlays == null)
                return 0;

            int count = 0;
            for (int i = 0; i < inlays.Length; i++)
            {
                if (inlays[i].ResourceType != ResourceType.None)
                    count++;
            }

            return count;
        }

        private void PlayFailDialogueOrComplete(GameObject interactor)
        {
            string dialogueId = GetRandomDialogueId(failDialogIds);
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                CompleteInteraction(interactor);
                return;
            }

            if (uiSystem == null)
            {
                Debug.LogWarning($"{name}: UISystem is not linked.");
                CompleteInteraction(interactor);
                return;
            }

            uiSystem.Execute(dialogueId, () => CompleteInteraction(interactor));
        }

        private static string GetRandomDialogueId(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0)
                return null;

            if (dialogueIds.Length == 1)
                return dialogueIds[0];

            return dialogueIds[Random.Range(0, dialogueIds.Length)];
        }
    }
}
