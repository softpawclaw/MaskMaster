using System;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    [Serializable]
    public struct MaskMiniGameZone
    {
        [Range(0f, 1f)] public float Min;
        [Range(0f, 1f)] public float Max;
        public MaskMiniGameOutcome Outcome;
        public float Score;
        public Color Color;

        public float Length => Mathf.Max(0f, Max - Min);

        public static MaskMiniGameZone Bad(float min, float max)
        {
            return new MaskMiniGameZone
            {
                Min = min,
                Max = max,
                Outcome = MaskMiniGameOutcome.Bad,
                Score = 0.5f,
                Color = new Color(0.85f, 0.18f, 0.12f, 0.85f)
            };
        }

        public static MaskMiniGameZone Normal(float min, float max)
        {
            return new MaskMiniGameZone
            {
                Min = min,
                Max = max,
                Outcome = MaskMiniGameOutcome.Normal,
                Score = 0.75f,
                Color = new Color(0.95f, 0.78f, 0.18f, 0.85f)
            };
        }

        public static MaskMiniGameZone Good(float min, float max)
        {
            return new MaskMiniGameZone
            {
                Min = min,
                Max = max,
                Outcome = MaskMiniGameOutcome.Good,
                Score = 1f,
                Color = new Color(0.25f, 0.95f, 0.35f, 0.85f)
            };
        }
    }
}
