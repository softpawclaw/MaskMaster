using UnityEngine;
using UnityEngine.UI;

namespace Interactable.MaskWorkbench
{
    public class CircularMaskMiniGameView : MaskMiniGamePathViewBase
    {
        [Header("Circular Path")]
        [SerializeField] private RectTransform centerPoint;
        [SerializeField] private float radius = 80f;
        [SerializeField] private float startAngle = 0f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private bool useImageFillForZones = true;
        [SerializeField] private float fallbackZoneThickness = 18f;

        private RectTransform OwnRect => transform as RectTransform;

        protected override Vector2 GetLocalPoint(float t)
        {
            float direction = clockwise ? -1f : 1f;
            float angle = (startAngle + direction * Mathf.Clamp01(t) * 360f) * Mathf.Deg2Rad;
            Vector2 center = GetCenterLocal();
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        protected override Vector2 GetLocalTangent(float t)
        {
            float direction = clockwise ? -1f : 1f;
            float angle = (startAngle + direction * Mathf.Clamp01(t) * 360f) * Mathf.Deg2Rad;
            Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * direction;
            return tangent;
        }

        protected override void ApplyZoneVisual(RectTransform zoneRect, MaskMiniGameZone zone, int index, int count)
        {
            if (zoneRect == null)
                return;

            Image image = zoneRect.GetComponent<Image>();
            if (image == null)
                return;

            image.type = Image.Type.Simple;
        }

        private Vector2 GetCenterLocal()
        {
            RectTransform owner = OwnRect;
            if (owner != null && centerPoint != null)
                return RectWorldToLocal(owner, centerPoint);
            return Vector2.zero;
        }
    }
}
