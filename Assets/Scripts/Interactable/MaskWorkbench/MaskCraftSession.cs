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
        public MaskWorkpiece Workpiece { get; private set; }
        public MaskItem CompletedMask { get; private set; }

        public bool HasStarted { get; private set; }
        public bool IsCompleted => CompletedMask != null;

        public ResourceType ActualMaterial => Workpiece != null ? Workpiece.SourceBlankType : BlankResource != null ? BlankResource.Type : ResourceType.None;

        public void Init(MainRecipeItem mainRecipe, PaperStackItem recipePagesStack, TrayItem tray)
        {
            MainRecipe = mainRecipe;
            RecipePagesStack = recipePagesStack;
            Tray = tray;
            BlankResource = null;
            Workpiece = null;
            CompletedMask = null;
            HasStarted = false;
        }

        public void MarkStarted(ResourceItem blankResource, MaskWorkpiece workpiece)
        {
            BlankResource = blankResource;
            Workpiece = workpiece;
            HasStarted = true;
        }

        public DBMask.MaskData BuildActualMaskData()
        {
            if (MainRecipe == null)
                return default;

            DBMask.MaskData target = MainRecipe.MaskData;

            // MVP: пока форма/размер/инкрустации не собираются честно.
            // Фиксируем только реальную болванку через MistResistanceId пока не можем,
            // поэтому возвращаем структуру заказа как есть. Реальное сравнение добавим
            // на этапе полноценной сборки actualMaskData.
            return target;
        }

        public void MarkCompleted(MaskItem completedMask)
        {
            CompletedMask = completedMask;
        }

        public void ClearRuntimeLinks()
        {
            MainRecipe = null;
            RecipePagesStack = null;
            Tray = null;
            BlankResource = null;
            Workpiece = null;
            CompletedMask = null;
            HasStarted = false;
        }
    }
}