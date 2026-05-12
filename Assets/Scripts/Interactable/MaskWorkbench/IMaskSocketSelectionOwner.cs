namespace Interactable.MaskWorkbench
{
    public interface IMaskSocketSelectionOwner
    {
        bool IsSocketCurrentlySelected(MaskWorkpieceSocketView socketView);
        bool HasPlannedInlay(MaskWorkpieceSocketView socketView);
    }
}
