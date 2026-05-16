using System;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    [Serializable]
    public class MaskMiniGameConfig
    {
        [Header("Identity")]
        public string ConfigId = "MG_Default";
        public MaskMiniGamePathViewBase ViewPrefab;

        [Header("Movement")]
        [Min(0.01f)] public float CursorSpeed = 0.65f;
        [Range(0.001f, 1f)] public float CursorSize01 = 0.08f;
        public bool PingPong = true;
        public float StartT = 0f;

        [Header("Hit")]
        public MaskMiniGameCursorHitMode HitMode = MaskMiniGameCursorHitMode.BestOverlap;
        public MaskMiniGameZone[] Zones =
        {
            new MaskMiniGameZone { Min = 0.00f, Max = 0.35f, Outcome = MaskMiniGameOutcome.Bad, Score = 0.5f, Color = new Color(0.85f, 0.18f, 0.12f, 0.85f) },
            new MaskMiniGameZone { Min = 0.35f, Max = 0.65f, Outcome = MaskMiniGameOutcome.Good, Score = 1.0f, Color = new Color(0.25f, 0.95f, 0.35f, 0.85f) },
            new MaskMiniGameZone { Min = 0.65f, Max = 1.00f, Outcome = MaskMiniGameOutcome.Normal, Score = 0.75f, Color = new Color(0.95f, 0.78f, 0.18f, 0.85f) }
        };
    }
}
