namespace Interactable.MaskWorkbench
{
    public readonly struct MaskMiniGameResult
    {
        public readonly string ConfigId;
        public readonly MaskMiniGameKind Kind;
        public readonly MaskMiniGameOutcome Outcome;
        public readonly float Score;
        public readonly float CursorT;

        public MaskMiniGameResult(string configId, MaskMiniGameKind kind, MaskMiniGameOutcome outcome, float score, float cursorT)
        {
            ConfigId = configId;
            Kind = kind;
            Outcome = outcome;
            Score = score;
            CursorT = cursorT;
        }
    }
}
