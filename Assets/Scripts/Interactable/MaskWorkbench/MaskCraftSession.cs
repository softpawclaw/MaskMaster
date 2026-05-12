using DB;
using Enums;
using Items;

namespace Interactable.MaskWorkbench
{
    public class MaskCraftSession
    {
        public MainRecipeItem MainRecipe { get; private set; }
        public PaperStackItem RecipePagesStack { get; private set; }
        public TrayItem Tray { get; private set; }
        public ResourceItem BlankResource { get; private set; }
        public MaskBlankWorkpiece BlankWorkpiece { get; private set; }
        public MaskItem CraftMask { get; private set; }
        public MaskItem CompletedMask { get; private set; }

        public bool HasStarted { get; private set; }
        public bool IsCompleted => CompletedMask != null;

        public ResourceType ActualMaterial => CraftMask != null ? CraftMask.SourceBlankType : BlankResource != null ? BlankResource.Type : ResourceType.None;

        public void Init(MainRecipeItem mainRecipe, PaperStackItem recipePagesStack, TrayItem tray)
        {
            MainRecipe = mainRecipe;
            RecipePagesStack = recipePagesStack;
            Tray = tray;
            BlankResource = null;
            BlankWorkpiece = null;
            CraftMask = null;
            CompletedMask = null;
            HasStarted = false;
        }

        public void MarkStarted(ResourceItem blankResource, MaskBlankWorkpiece blankWorkpiece, MaskItem craftMask)
        {
            BlankResource = blankResource;
            BlankWorkpiece = blankWorkpiece;
            CraftMask = craftMask;
            HasStarted = true;
        }

        public DBMask.MaskData BuildActualMaskData()
        {
            if (MainRecipe == null)
                return default;

            // MVP: реальные ResourceType/Size/Form уже живут в MaskItem.
            // Структуру DBMask.MaskData не ломаем до отдельного прохода по сравнению качества/форм.
            return MainRecipe.MaskData;
        }

        public void MarkCompleted(MaskItem completedMask)
        {
            CompletedMask = completedMask;
            CraftMask = completedMask;
        }

        public void ClearRuntimeLinks()
        {
            MainRecipe = null;
            RecipePagesStack = null;
            Tray = null;
            BlankResource = null;
            BlankWorkpiece = null;
            CraftMask = null;
            CompletedMask = null;
            HasStarted = false;
        }
    }
}
