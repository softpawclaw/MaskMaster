using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class LinearMaskMiniGameView : MaskMiniGamePathViewBase
    {
        [Header("Linear Path")]
        [SerializeField] private RectTransform track;
        [SerializeField] private RectTransform startPoint;
        [SerializeField] private RectTransform endPoint;
        [SerializeField] private Vector2 defaultSize = new(300f, 50f);
        [SerializeField] private bool preserveZoneImageThickness = true;
        [SerializeField] private float fallbackZoneThickness = 18f;

        private RectTransform OwnRect => transform as RectTransform;

        public void Configure(RectTransform trackRect, RectTransform start, RectTransform end, RectTransform cursorRect, UnityEngine.UI.Image[] zones)
        {
            track = trackRect;
            startPoint = start;
            endPoint = end;
            cursor = cursorRect;
            zoneImages = zones;
        }

        public override void Init(MaskMiniGameConfig config)
        {
            RectTransform rect = OwnRect;
            if (rect != null && rect.sizeDelta.sqrMagnitude < 1f)
                rect.sizeDelta = defaultSize;

            base.Init(config);
        }

        protected override Vector2 GetLocalPoint(float t)
        {
            Vector2 a;
            Vector2 b;
            GetLinePoints(out a, out b);
            return Vector2.LerpUnclamped(a, b, Mathf.Clamp01(t));
        }

        protected override void ApplyZoneVisual(RectTransform zoneRect, MaskMiniGameZone zone, int index, int count)
        {
            if (zoneRect == null)
                return;

            Vector2 a = GetLocalPoint(zone.Min);
            Vector2 b = GetLocalPoint(zone.Max);
            Vector2 mid = (a + b) * 0.5f;
            Vector2 dir = b - a;
            float length = dir.magnitude;
            float angle = length > 0.0001f ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;

            zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
            zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            zoneRect.pivot = new Vector2(0.5f, 0.5f);
            zoneRect.anchoredPosition = mid;
            zoneRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            float thickness = preserveZoneImageThickness ? Mathf.Max(1f, zoneRect.sizeDelta.y) : Mathf.Max(1f, fallbackZoneThickness);
            zoneRect.sizeDelta = new Vector2(length, thickness);
        }

        private void GetLinePoints(out Vector2 start, out Vector2 end)
        {
            RectTransform owner = OwnRect;

            if (owner != null && startPoint != null && endPoint != null)
            {
                start = RectWorldToLocal(owner, startPoint);
                end = RectWorldToLocal(owner, endPoint);
                return;
            }

            RectTransform source = track != null ? track : owner;
            if (source == null)
            {
                start = Vector2.left * defaultSize.x * 0.5f;
                end = Vector2.right * defaultSize.x * 0.5f;
                return;
            }

            Rect rect = source.rect;
            Vector3 leftWorld = source.TransformPoint(new Vector3(rect.xMin, rect.center.y, 0f));
            Vector3 rightWorld = source.TransformPoint(new Vector3(rect.xMax, rect.center.y, 0f));

            if (owner != null && owner != source)
            {
                start = owner.InverseTransformPoint(leftWorld);
                end = owner.InverseTransformPoint(rightWorld);
            }
            else
            {
                start = new Vector2(rect.xMin, rect.center.y);
                end = new Vector2(rect.xMax, rect.center.y);
            }
        }
    }
}
