using UnityEngine;
using UnityEngine.UI;

namespace Interactable.MaskWorkbench
{
    public abstract class MaskMiniGamePathViewBase : MonoBehaviour
    {
        [Header("Path Cursor")]
        [SerializeField] protected RectTransform cursor;
        [SerializeField] protected bool rotateCursorAlongPath = true;
        [SerializeField] protected bool keepCursorPrefabSize = true;
        [SerializeField] protected float fallbackCursorPixelSize = 24f;

        [Header("Zone Images")]
        [SerializeField] protected Image[] zoneImages;
        [SerializeField] protected bool recolorZoneImages = true;

        private MaskMiniGameConfig activeConfig;

        public virtual void Init(MaskMiniGameConfig config)
        {
            activeConfig = config;
            RefreshZones(config);
            SetCursorT(Mathf.Clamp01(config != null ? config.StartT : 0f));
        }

        public void SetCursorT(float t)
        {
            t = Mathf.Clamp01(t);
            if (cursor == null)
                return;

            cursor.anchoredPosition = GetLocalPoint(t);

            if (rotateCursorAlongPath)
            {
                Vector2 tangent = GetLocalTangent(t);
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    cursor.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            if (!keepCursorPrefabSize)
            {
                float size = Mathf.Max(1f, fallbackCursorPixelSize);
                cursor.sizeDelta = new Vector2(size, size);
            }
        }

        protected virtual void RefreshZones(MaskMiniGameConfig config)
        {
            if (config == null || config.Zones == null || zoneImages == null)
                return;

            int count = Mathf.Min(config.Zones.Length, zoneImages.Length);
            for (int i = 0; i < zoneImages.Length; i++)
            {
                if (zoneImages[i] != null)
                    zoneImages[i].gameObject.SetActive(i < count);
            }

            for (int i = 0; i < count; i++)
            {
                if (zoneImages[i] == null)
                    continue;

                zoneImages[i].gameObject.SetActive(true);
                if (recolorZoneImages)
                    zoneImages[i].color = config.Zones[i].Color;

                ApplyZoneVisual(zoneImages[i].rectTransform, config.Zones[i], i, count);
            }
        }

        protected virtual void ApplyZoneVisual(RectTransform zoneRect, MaskMiniGameZone zone, int index, int count)
        {
            // По умолчанию view не знает, как растянуть зону. Наследники решают сами.
        }

        protected abstract Vector2 GetLocalPoint(float t);

        protected virtual Vector2 GetLocalTangent(float t)
        {
            const float delta = 0.002f;
            float a = Mathf.Clamp01(t - delta);
            float b = Mathf.Clamp01(t + delta);
            if (Mathf.Approximately(a, b))
            {
                a = Mathf.Clamp01(t);
                b = Mathf.Clamp01(t + delta);
            }
            return GetLocalPoint(b) - GetLocalPoint(a);
        }

        protected static Vector2 RectWorldToLocal(RectTransform owner, RectTransform point)
        {
            if (owner == null || point == null)
                return Vector2.zero;

            Vector3 world = point.TransformPoint(point.rect.center);
            Vector3 local = owner.InverseTransformPoint(world);
            return local;
        }
    }
}
