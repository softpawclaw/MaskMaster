using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public class PolylineMaskMiniGameView : MaskMiniGamePathViewBase
    {
        [Header("Polyline Path")]
        [SerializeField] private RectTransform[] points;
        [SerializeField] private bool preserveZoneImageThickness = true;
        [SerializeField] private float fallbackZoneThickness = 18f;
        [SerializeField] private int zoneApproximationSegments = 8;

        private Vector2[] localPoints;
        private float[] cumulativeLengths;
        private float totalLength;
        private RectTransform OwnRect => transform as RectTransform;

        public override void Init(MaskMiniGameConfig config)
        {
            RebuildPathCache();
            base.Init(config);
        }

        protected override Vector2 GetLocalPoint(float t)
        {
            if (localPoints == null || localPoints.Length == 0)
                return Vector2.zero;

            if (localPoints.Length == 1 || totalLength <= 0.0001f)
                return localPoints[0];

            float distance = Mathf.Clamp01(t) * totalLength;
            for (int i = 0; i < cumulativeLengths.Length - 1; i++)
            {
                float a = cumulativeLengths[i];
                float b = cumulativeLengths[i + 1];
                if (distance > b && i < cumulativeLengths.Length - 2)
                    continue;

                float segmentT = Mathf.InverseLerp(a, b, distance);
                return Vector2.LerpUnclamped(localPoints[i], localPoints[i + 1], segmentT);
            }

            return localPoints[localPoints.Length - 1];
        }

        protected override void ApplyZoneVisual(RectTransform zoneRect, MaskMiniGameZone zone, int index, int count)
        {
            if (zoneRect == null)
                return;

            // Для ломаной один Image не может повторить всю кривую без меша/лайна.
            // Поэтому MVP-визуал зоны — короткая плашка в середине диапазона, повернутая по касательной.
            float midT = Mathf.Clamp01((zone.Min + zone.Max) * 0.5f);
            Vector2 mid = GetLocalPoint(midT);
            Vector2 tangent = GetLocalTangent(midT);
            float length = EstimateArcLength(zone.Min, zone.Max, Mathf.Max(2, zoneApproximationSegments));
            float angle = tangent.sqrMagnitude > 0.0001f ? Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg : 0f;

            zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
            zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            zoneRect.pivot = new Vector2(0.5f, 0.5f);
            zoneRect.anchoredPosition = mid;
            zoneRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            float thickness = preserveZoneImageThickness ? Mathf.Max(1f, zoneRect.sizeDelta.y) : Mathf.Max(1f, fallbackZoneThickness);
            zoneRect.sizeDelta = new Vector2(Mathf.Max(1f, length), thickness);
        }

        private void RebuildPathCache()
        {
            RectTransform owner = OwnRect;
            if (owner == null || points == null || points.Length == 0)
            {
                localPoints = new[] { Vector2.zero };
                cumulativeLengths = new[] { 0f };
                totalLength = 0f;
                return;
            }

            int validCount = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null)
                    validCount++;
            }

            if (validCount == 0)
            {
                localPoints = new[] { Vector2.zero };
                cumulativeLengths = new[] { 0f };
                totalLength = 0f;
                return;
            }

            localPoints = new Vector2[validCount];
            int write = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null)
                    continue;
                localPoints[write++] = RectWorldToLocal(owner, points[i]);
            }

            cumulativeLengths = new float[localPoints.Length];
            cumulativeLengths[0] = 0f;
            totalLength = 0f;
            for (int i = 1; i < localPoints.Length; i++)
            {
                totalLength += Vector2.Distance(localPoints[i - 1], localPoints[i]);
                cumulativeLengths[i] = totalLength;
            }
        }

        private float EstimateArcLength(float minT, float maxT, int steps)
        {
            minT = Mathf.Clamp01(minT);
            maxT = Mathf.Clamp01(maxT);
            if (maxT < minT)
            {
                float tmp = minT;
                minT = maxT;
                maxT = tmp;
            }

            Vector2 prev = GetLocalPoint(minT);
            float result = 0f;
            for (int i = 1; i <= steps; i++)
            {
                float t = Mathf.Lerp(minT, maxT, i / (float)steps);
                Vector2 next = GetLocalPoint(t);
                result += Vector2.Distance(prev, next);
                prev = next;
            }
            return result;
        }
    }
}
