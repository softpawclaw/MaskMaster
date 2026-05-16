using System;
using UnityEngine;
using UnityEngine.UI;
using MaskMiniGameConfig = Interactable.MaskWorkbench.MaskMiniGameConfig;

namespace Interactable.MaskWorkbench
{
    public class MaskMiniGameSystem : MonoBehaviour
    {
        [Header("World Placement")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool keepConstantScreenSize = true;
        [SerializeField] private float screenSizeMultiplier = 1f;
        [SerializeField] private float fallbackWorldScale = 0.001f;
        [SerializeField] private int sortingOrder = 500;

        [Header("Configs")]
        [SerializeField] private MaskMiniGameConfig fallbackConfig = new();
        [SerializeField] private MaskMiniGameConfig[] configs;

        private Canvas activeCanvas;
        private MaskMiniGamePathViewBase activeView;
        private MaskMiniGameConfig activeConfig;
        private MaskMiniGameRequest activeRequest;
        private Action<MaskMiniGameResult> activeCallback;
        private Transform activeWorldAnchor;
        private float cursorT;
        private int direction = 1;
        private bool running;

        public bool IsRunning => running;

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void Update()
        {
            if (!running || activeConfig == null || activeView == null)
                return;

            TickCursor(Time.deltaTime);
        }

        public void Run(MaskMiniGameRequest request, Action<MaskMiniGameResult> onComplete)
        {
            if (request.WorldAnchor == null)
            {
                Debug.LogError($"{name}: cannot run mini-game '{request.Kind}'. WorldAnchor is null.");
                return;
            }

            StopWithoutCallback();

            activeRequest = request;
            activeWorldAnchor = request.WorldAnchor;
            activeCallback = onComplete;
            activeConfig = FindConfig(request.ConfigId);
            if (activeConfig == null)
                activeConfig = fallbackConfig;

            if (activeConfig == null)
            {
                Debug.LogError($"{name}: cannot run mini-game '{request.Kind}'. No config and no fallback config.");
                return;
            }

            cursorT = Mathf.Clamp01(activeConfig.StartT);
            direction = 1;

            CreateWorldCanvasAndView();
            ApplyInitialWorldPlacement();

            if (activeView != null)
                activeView.Init(activeConfig);

            running = true;
        }

        public void Confirm()
        {
            if (!running || activeConfig == null)
                return;

            MaskMiniGameZone zone = ResolveZone(cursorT, activeConfig);
            MaskMiniGameResult result = new(activeConfig.ConfigId, activeRequest.Kind, zone.Outcome, zone.Score, cursorT);
            Action<MaskMiniGameResult> callback = activeCallback;

            StopWithoutCallback();
            callback?.Invoke(result);
        }

        public void Cancel()
        {
            StopWithoutCallback();
        }

        private void TickCursor(float deltaTime)
        {
            float speed = Mathf.Max(0.01f, activeConfig.CursorSpeed);
            cursorT += direction * speed * deltaTime;

            if (activeConfig.PingPong)
            {
                if (cursorT > 1f)
                {
                    cursorT = 1f - (cursorT - 1f);
                    direction = -1;
                }
                else if (cursorT < 0f)
                {
                    cursorT = -cursorT;
                    direction = 1;
                }
            }
            else
            {
                cursorT = Mathf.Repeat(cursorT, 1f);
            }

            cursorT = Mathf.Clamp01(cursorT);
            activeView.SetCursorT(cursorT);
        }

        private void CreateWorldCanvasAndView()
        {
            GameObject canvasObject = new GameObject(
                "Runtime_MaskMiniGame_WorldCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = Vector3.zero;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one;

            activeCanvas = canvasObject.GetComponent<Canvas>();
            activeCanvas.renderMode = RenderMode.WorldSpace;
            activeCanvas.sortingOrder = sortingOrder;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam != null)
                activeCanvas.worldCamera = cam;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 160f);

            MaskMiniGamePathViewBase prefab = activeConfig.ViewPrefab;
            if (prefab != null)
            {
                activeView = Instantiate(prefab, canvasRect);
            }
            else
            {
                activeView = CreateRuntimeLinearView(canvasRect);
            }

            RectTransform viewRect = activeView.transform as RectTransform;
            if (viewRect != null)
            {
                viewRect.anchorMin = new Vector2(0.5f, 0.5f);
                viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewRect.pivot = new Vector2(0.5f, 0.5f);
                viewRect.anchoredPosition = Vector2.zero;
                viewRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private void ApplyInitialWorldPlacement()
        {
            if (activeCanvas == null || activeWorldAnchor == null)
                return;

            Transform canvasTransform = activeCanvas.transform;

            canvasTransform.position = activeWorldAnchor.position;
            canvasTransform.rotation = Quaternion.identity;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam != null)
                activeCanvas.worldCamera = cam;

            float scale = fallbackWorldScale * screenSizeMultiplier;

            if (cam != null && keepConstantScreenSize)
            {
                float distance = Vector3.Distance(cam.transform.position, canvasTransform.position);
                scale = distance * fallbackWorldScale * screenSizeMultiplier;
            }

            canvasTransform.localScale = Vector3.one * Mathf.Max(0.00001f, scale);
        }

        private MaskMiniGamePathViewBase CreateRuntimeLinearView(RectTransform parent)
        {
            GameObject root = new GameObject("Runtime_LinearMaskMiniGameView", typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(300f, 50f);

            GameObject track = CreateUiImage("Back", rootRect, new Color(0f, 0f, 0f, 0.85f));
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.sizeDelta = new Vector2(300f, 50f);

            GameObject z0 = CreateUiImage("Zone_0", rootRect, Color.white);
            GameObject z1 = CreateUiImage("Zone_1", rootRect, Color.white);
            GameObject z2 = CreateUiImage("Zone_2", rootRect, Color.white);
            GameObject cursor = CreateUiImage("Cursor", rootRect, Color.white);
            cursor.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 64f);

            LinearMaskMiniGameView view = root.AddComponent<LinearMaskMiniGameView>();
            view.Configure(
                trackRect,
                null,
                null,
                cursor.GetComponent<RectTransform>(),
                new[]
                {
                    z0.GetComponent<UnityEngine.UI.Image>(),
                    z1.GetComponent<UnityEngine.UI.Image>(),
                    z2.GetComponent<UnityEngine.UI.Image>()
                });

            Debug.LogWarning($"{name}: config '{activeConfig.ConfigId}' has no ViewPrefab. Runtime fallback view was created, but it is not intended for final visuals.");
            return view;
        }

        private static GameObject CreateUiImage(string objectName, Transform parent, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            return go;
        }

        private MaskMiniGameConfig FindConfig(string configId)
        {
            if (!string.IsNullOrWhiteSpace(configId) && configs != null)
            {
                for (int i = 0; i < configs.Length; i++)
                {
                    if (configs[i] != null && configs[i].ConfigId == configId)
                        return configs[i];
                }
            }

            if (fallbackConfig != null)
            {
                if (string.IsNullOrWhiteSpace(configId) || fallbackConfig.ConfigId == configId)
                    return fallbackConfig;
            }

            return fallbackConfig;
        }

        private MaskMiniGameZone ResolveZone(float t, MaskMiniGameConfig config)
        {
            if (config.Zones == null || config.Zones.Length == 0)
                return MaskMiniGameZone.Bad(0f, 1f);

            if (config.HitMode == MaskMiniGameCursorHitMode.CenterOnly)
                return ResolveCenterOnly(t, config);

            float half = Mathf.Clamp01(config.CursorSize01) * 0.5f;
            float cursorMin = Mathf.Clamp01(t - half);
            float cursorMax = Mathf.Clamp01(t + half);

            MaskMiniGameZone best = config.Zones[0];
            float bestScore = float.NegativeInfinity;
            float bestOverlap = float.NegativeInfinity;

            for (int i = 0; i < config.Zones.Length; i++)
            {
                MaskMiniGameZone zone = config.Zones[i];
                float overlap = Mathf.Max(0f, Mathf.Min(cursorMax, zone.Max) - Mathf.Max(cursorMin, zone.Min));
                if (overlap <= 0f)
                    continue;

                if (config.HitMode == MaskMiniGameCursorHitMode.BestOverlap)
                {
                    if (zone.Score > bestScore || (Mathf.Approximately(zone.Score, bestScore) && overlap > bestOverlap))
                    {
                        best = zone;
                        bestScore = zone.Score;
                        bestOverlap = overlap;
                    }
                }
                else
                {
                    if (overlap > bestOverlap || (Mathf.Approximately(overlap, bestOverlap) && zone.Score > bestScore))
                    {
                        best = zone;
                        bestScore = zone.Score;
                        bestOverlap = overlap;
                    }
                }
            }

            if (bestOverlap > 0f)
                return best;

            return ResolveCenterOnly(t, config);
        }

        private static MaskMiniGameZone ResolveCenterOnly(float t, MaskMiniGameConfig config)
        {
            for (int i = 0; i < config.Zones.Length; i++)
            {
                MaskMiniGameZone zone = config.Zones[i];
                if (t >= zone.Min && t <= zone.Max)
                    return zone;
            }

            return config.Zones[0];
        }

        private void StopWithoutCallback()
        {
            running = false;
            activeCallback = null;
            activeRequest = default;
            activeConfig = null;
            activeWorldAnchor = null;

            if (activeCanvas != null)
                Destroy(activeCanvas.gameObject);

            activeCanvas = null;
            activeView = null;
        }
    }
}