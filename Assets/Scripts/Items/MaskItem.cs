using System;
using System.Collections.Generic;
using DB;
using Enums;
using Interactable.MaskWorkbench;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Финальный предмет маски и одновременно runtime-носитель собираемого визуала на столе.
    /// Стол управляет процессом, но данные/выбранные формы остаются внутри самого MaskItem.
    /// </summary>
    public class MaskItem : ItemBase, IMaskSocketSelectionOwner
    {
        [Serializable]
        public struct PlannedInlay
        {
            public MaskSegment Segment;
            public MaskSocket Socket;
            public ResourceType ResourceType;
            public MaskWorkpieceSocketView SocketView;
        }

        private class SegmentRuntime
        {
            public bool IsPresent = true;
            public int ShapeIndex;
        }

        [Header("Mask Result")]
        [SerializeField] private string orderId;
        [SerializeField] private DBMask.MaskData targetMaskData;
        [SerializeField] private DBMask.MaskData actualMaskData;

        [Header("Craft Runtime Data")]
        [SerializeField] private ResourceType sourceBlankType = ResourceType.None;
        [SerializeField] private MaskSize selectedSize = MaskSize.None;

        [Header("Craft Quality")]
        [SerializeField] private float expectedQualityPoints;
        [SerializeField] private float actualQualityPoints;

        [Header("Mini Game Anchors")]
        [SerializeField] private List<Transform> miniGameAnchors = new();

        [Header("Craft Visual")]
        [SerializeField] private List<MaskWorkpieceSegmentView> segmentViews = new();

        private readonly Dictionary<MaskSegment, SegmentRuntime> segments = new();
        private readonly List<PlannedInlay> plannedInlays = new();
        private MaskSegment selectedSegment = MaskSegment.Middle;
        private int selectedSocketIndex;
        private int lastMiniGameAnchorIndex = -1;
        private MaskWorkbenchState currentWorkbenchViewMode = MaskWorkbenchState.CraftSurfaceInspect;

        public string OrderId => orderId;
        public DBMask.MaskData TargetMaskData => targetMaskData;
        public DBMask.MaskData ActualMaskData => actualMaskData;
        public ResourceType SourceBlankType => sourceBlankType;
        public MaskSize SelectedSize => selectedSize;
        public MaskSegment SelectedSegment => selectedSegment;
        public int SelectedSocketIndex => selectedSocketIndex;
        public IReadOnlyList<PlannedInlay> PlannedInlays => plannedInlays;
        public float ExpectedQualityPoints => expectedQualityPoints;
        public float ActualQualityPoints => actualQualityPoints;

        private static readonly MaskSegment[] SegmentOrder =
        {
            MaskSegment.Upper,
            MaskSegment.Middle,
            MaskSegment.Lower
        };

        public void Init(DBMask.MaskData targetMask, DBMask.MaskData actualMask)
        {
            targetMaskData = targetMask;
            actualMaskData = actualMask;
            orderId = targetMask.OR_Id;
            EnsureRuntimeState();
            EnsureConfiguredViews();
            currentWorkbenchViewMode = MaskWorkbenchState.Completed;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public void InitForCraft(DBMask.MaskData targetMask)
        {
            targetMaskData = targetMask;
            actualMaskData = targetMask;
            orderId = targetMask.OR_Id;
            sourceBlankType = ResourceType.None;
            selectedSize = MaskSize.None;
            plannedInlays.Clear();
            segments.Clear();
            EnsureRuntimeState();
            EnsureConfiguredViews();
            selectedSegment = IsSegmentPresent(MaskSegment.Middle) ? MaskSegment.Middle : FindFirstPresentSegment();
            selectedSocketIndex = 0;
            expectedQualityPoints = 0f;
            actualQualityPoints = 0f;
            lastMiniGameAnchorIndex = -1;
            currentWorkbenchViewMode = MaskWorkbenchState.CraftSurfaceInspect;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public void SetSourceBlank(ResourceType blankType)
        {
            sourceBlankType = blankType;
        }

        public void ApplySizeFromBlank(MaskBlankWorkpiece blank)
        {
            if (blank == null)
                return;

            SetSourceBlank(blank.SourceBlankType);
            ApplySegmentPresence(blank.BuildPresenceSnapshot());
        }

        public void ApplySegmentPresence(bool[] upperMiddleLower)
        {
            EnsureRuntimeState();

            SetSegmentPresent(MaskSegment.Upper, upperMiddleLower != null && upperMiddleLower.Length > 0 && upperMiddleLower[0]);
            SetSegmentPresent(MaskSegment.Middle, upperMiddleLower == null || upperMiddleLower.Length <= 1 || upperMiddleLower[1]);
            SetSegmentPresent(MaskSegment.Lower, upperMiddleLower != null && upperMiddleLower.Length > 2 && upperMiddleLower[2]);

            if (!HasAnyPresentSegment())
                SetSegmentPresent(MaskSegment.Middle, true);

            selectedSize = ResolveSizeFromPresence();

            if (!IsSegmentPresent(selectedSegment))
                selectedSegment = FindFirstPresentSegment();

            selectedSocketIndex = 0;
            currentWorkbenchViewMode = MaskWorkbenchState.FormSelection;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public bool IsSegmentPresent(MaskSegment segment)
        {
            EnsureRuntimeState();
            return segments.TryGetValue(segment, out SegmentRuntime runtime) && runtime.IsPresent;
        }

        public void SelectPreviousSegment() => SelectSegmentDelta(-1);
        public void SelectNextSegment() => SelectSegmentDelta(1);

        public void SelectSegmentDelta(int delta)
        {
            int index = Array.IndexOf(SegmentOrder, selectedSegment);
            if (index < 0) index = 1;

            for (int step = 0; step < SegmentOrder.Length; step++)
            {
                index += delta;
                if (index < 0) index = SegmentOrder.Length - 1;
                if (index >= SegmentOrder.Length) index = 0;

                if (!IsSegmentPresent(SegmentOrder[index]))
                    continue;

                selectedSegment = SegmentOrder[index];
                selectedSocketIndex = 0;
                RefreshCraftVisual(currentWorkbenchViewMode);
                return;
            }
        }

        public void SelectPreviousShape() => ChangeShape(-1);
        public void SelectNextShape() => ChangeShape(1);

        public void ChangeShape(int delta)
        {
            EnsureRuntimeState();

            if (!segments.TryGetValue(selectedSegment, out SegmentRuntime runtime) || !runtime.IsPresent)
                return;

            MaskWorkpieceSegmentView view = FindView(selectedSegment);
            int variantCount = view != null ? view.ShapeVariantCount : 1;
            if (variantCount <= 0)
                variantCount = 1;

            runtime.ShapeIndex += delta;
            if (runtime.ShapeIndex < 0) runtime.ShapeIndex = variantCount - 1;
            if (runtime.ShapeIndex >= variantCount) runtime.ShapeIndex = 0;

            selectedSocketIndex = 0;
            currentWorkbenchViewMode = MaskWorkbenchState.FormSelection;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public void SolidifySelectedShapes()
        {
            EnsureRuntimeState();
            EnsureConfiguredViews();

            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null || !IsSegmentPresent(view.Segment))
                    continue;

                view.SetBlankMeshVisible(false);
                view.SolidifyShape(GetShapeIndex(view.Segment));
            }
        }

        public void SetWorkbenchViewMode(MaskWorkbenchState mode)
        {
            currentWorkbenchViewMode = mode;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }
        public void AddCraftQualityPoints(float points)
        {
            actualQualityPoints += Mathf.Max(0f, points);
        }

        public void SetExpectedQualityPoints(float points)
        {
            expectedQualityPoints = Mathf.Max(0f, points);
        }

        public Transform GetRandomMiniGameAnchor()
        {
            miniGameAnchors.RemoveAll(anchor => anchor == null);

            if (miniGameAnchors.Count == 0)
            {
                Debug.LogError($"{name}: mini-game anchor list is empty. Add transforms to MaskItem -> Mini Game Anchors.");
                return null;
            }

            if (miniGameAnchors.Count == 1)
            {
                lastMiniGameAnchorIndex = 0;
                return miniGameAnchors[0];
            }

            int index = UnityEngine.Random.Range(0, miniGameAnchors.Count);
            if (index == lastMiniGameAnchorIndex)
                index = (index + 1) % miniGameAnchors.Count;

            lastMiniGameAnchorIndex = index;
            return miniGameAnchors[index];
        }


        public Transform GetSelectedSelectionAnchor()
        {
            MaskWorkpieceSegmentView view = FindView(selectedSegment);
            return view != null ? view.SelectionAnchor : transform;
        }

        public void HideCraftHelpers()
        {
            currentWorkbenchViewMode = MaskWorkbenchState.Completed;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public void SelectPreviousSocket() => ChangeSocket(-1);
        public void SelectNextSocket() => ChangeSocket(1);

        public void ChangeSocket(int delta)
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(selectedSegment);
            if (sockets.Count == 0)
                return;

            selectedSocketIndex += delta;
            if (selectedSocketIndex < 0) selectedSocketIndex = sockets.Count - 1;
            if (selectedSocketIndex >= sockets.Count) selectedSocketIndex = 0;

            currentWorkbenchViewMode = MaskWorkbenchState.InlaySelection;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public MaskWorkpieceSocketView GetSelectedSocketView()
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(selectedSegment);
            if (sockets.Count == 0)
                return null;

            selectedSocketIndex = Mathf.Clamp(selectedSocketIndex, 0, sockets.Count - 1);
            return sockets[selectedSocketIndex];
        }

        public Transform GetSelectedSocketAnchor()
        {
            MaskWorkpieceSocketView socketView = GetSelectedSocketView();
            return socketView != null ? socketView.SelectionAnchor : GetSelectedSelectionAnchor();
        }

        public MaskSocket GetSelectedSocket()
        {
            MaskWorkpieceSocketView socketView = GetSelectedSocketView();
            return socketView != null ? socketView.Socket : MaskSocket.None;
        }

        public bool IsSocketCurrentlySelected(MaskWorkpieceSocketView socketView)
        {
            return socketView != null && ReferenceEquals(GetSelectedSocketView(), socketView);
        }

        public bool HasPlannedInlay(MaskWorkpieceSocketView socketView)
        {
            if (socketView == null)
                return false;

            if (socketView.HasPlannedInlay)
                return true;

            for (int i = 0; i < plannedInlays.Count; i++)
            {
                PlannedInlay planned = plannedInlays[i];
                if (ReferenceEquals(planned.SocketView, socketView) && planned.ResourceType != ResourceType.None)
                    return true;
            }

            return false;
        }

        public bool ApplyInlayCursorToSelectedSocket(MaskInlaySelectionCursor cursor)
        {
            if (cursor == null || !cursor.HasOptions)
                return false;

            if (cursor.IsClearSelected)
                return ClearSelectedSocket();

            return PlanInlayOnSelectedSocket(cursor.SelectedResourceType, cursor.SelectedVisualData);
        }

        public bool PlanInlayOnSelectedSocket(ResourceType resourceType, DBInlayVisual.InlayVisualData visualData)
        {
            if (!Helpers.ResourceTypeHelper.IsInlay(resourceType))
                return false;

            MaskWorkpieceSocketView socketView = GetSelectedSocketView();
            if (socketView == null || socketView.Socket == MaskSocket.None)
                return false;

            socketView.SetPlannedInlay(resourceType, visualData);

            for (int i = 0; i < plannedInlays.Count; i++)
            {
                PlannedInlay planned = plannedInlays[i];
                if (!ReferenceEquals(planned.SocketView, socketView))
                    continue;

                planned.ResourceType = resourceType;
                plannedInlays[i] = planned;
                RefreshCraftVisual(MaskWorkbenchState.InlaySelection);
                Debug.Log($"{name}: updated planned inlay {resourceType} at {selectedSegment}/{socketView.Socket}.");
                return true;
            }

            plannedInlays.Add(new PlannedInlay
            {
                Segment = selectedSegment,
                Socket = socketView.Socket,
                ResourceType = resourceType,
                SocketView = socketView
            });

            RefreshCraftVisual(MaskWorkbenchState.InlaySelection);
            Debug.Log($"{name}: planned inlay {resourceType} at {selectedSegment}/{socketView.Socket}.");
            return true;
        }

        public bool ClearSelectedSocket()
        {
            MaskWorkpieceSocketView socketView = GetSelectedSocketView();
            if (socketView == null)
                return false;

            socketView.ClearPlannedInlay();

            for (int i = plannedInlays.Count - 1; i >= 0; i--)
            {
                PlannedInlay planned = plannedInlays[i];
                if (ReferenceEquals(planned.SocketView, socketView))
                    plannedInlays.RemoveAt(i);
            }

            RefreshCraftVisual(MaskWorkbenchState.InlaySelection);
            Debug.Log($"{name}: cleared planned inlay at {selectedSegment}/{socketView.Socket}.");
            return true;
        }

        public Dictionary<ResourceType, List<PlannedInlay>> BuildPlannedInlayGroups()
        {
            Dictionary<ResourceType, List<PlannedInlay>> result = new();

            for (int i = plannedInlays.Count - 1; i >= 0; i--)
            {
                PlannedInlay planned = plannedInlays[i];
                MaskWorkpieceSocketView socketView = planned.SocketView;

                if (socketView == null || !socketView.HasPlannedInlay || !Helpers.ResourceTypeHelper.IsInlay(socketView.PlannedResourceType))
                {
                    plannedInlays.RemoveAt(i);
                    continue;
                }

                planned.Segment = FindSegmentForSocketView(socketView, planned.Segment);
                planned.Socket = socketView.Socket;
                planned.ResourceType = socketView.PlannedResourceType;
                plannedInlays[i] = planned;

                if (!result.TryGetValue(planned.ResourceType, out List<PlannedInlay> group))
                {
                    group = new List<PlannedInlay>();
                    result.Add(planned.ResourceType, group);
                }

                group.Add(planned);
            }

            return result;
        }

        public void ApplyInlayGroup(ResourceType resourceType, IReadOnlyList<PlannedInlay> group)
        {
            if (group == null)
                return;

            for (int i = 0; i < group.Count; i++)
            {
                PlannedInlay planned = group[i];
                MaskWorkpieceSocketView socketView = planned.SocketView != null
                    ? planned.SocketView
                    : FindSocketView(planned.Segment, planned.Socket);
                if (socketView == null)
                    continue;

                socketView.SolidifyPlannedInlay();
            }

            Debug.Log($"{name}: installed inlay group {resourceType}, count={group.Count}.");
        }

        private void RefreshCraftVisual(MaskWorkbenchState mode)
        {
            EnsureRuntimeState();
            EnsureConfiguredViews();

            bool formMode = mode == MaskWorkbenchState.FormSelection;
            bool inlayMode = mode == MaskWorkbenchState.InlaySelection;
            bool completed = mode == MaskWorkbenchState.Completed;

            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null)
                    continue;

                bool present = IsSegmentPresent(view.Segment);
                view.EnsureRootActive();
                view.SetBlankMeshVisible(false);
                view.SetMarkedForCut(false);

                bool showShape = present && (formMode || inlayMode || completed);
                bool showSockets = inlayMode || completed;
                bool solid = inlayMode || completed;
                view.RefreshShape(GetShapeIndex(view.Segment), showShape, showSockets, this, solid, inlayMode);
            }
        }

        private void EnsureRuntimeState()
        {
            if (segments.Count > 0)
                return;

            segments.Add(MaskSegment.Upper, new SegmentRuntime());
            segments.Add(MaskSegment.Middle, new SegmentRuntime());
            segments.Add(MaskSegment.Lower, new SegmentRuntime());
        }

        private void EnsureConfiguredViews()
        {
            segmentViews.RemoveAll(view => view == null);
            if (segmentViews.Count > 0)
                return;

            GetComponentsInChildren(true, segmentViews);
            segmentViews.RemoveAll(view => view == null);
        }

        private void SetSegmentPresent(MaskSegment segment, bool present)
        {
            EnsureRuntimeState();
            if (segments.TryGetValue(segment, out SegmentRuntime runtime))
                runtime.IsPresent = present;
        }

        private int GetShapeIndex(MaskSegment segment)
        {
            return segments.TryGetValue(segment, out SegmentRuntime runtime) ? runtime.ShapeIndex : 0;
        }

        private MaskWorkpieceSegmentView FindView(MaskSegment segment)
        {
            EnsureConfiguredViews();
            for (int i = 0; i < segmentViews.Count; i++)
            {
                if (segmentViews[i] != null && segmentViews[i].Segment == segment)
                    return segmentViews[i];
            }
            return null;
        }


        private IReadOnlyList<MaskWorkpieceSocketView> GetActiveSocketViewsForSegment(MaskSegment segment)
        {
            MaskWorkpieceSegmentView view = FindView(segment);
            if (view == null)
                return Array.Empty<MaskWorkpieceSocketView>();

            MaskWorkpieceShapeVariant variant = view.GetShapeVariant(GetShapeIndex(segment));
            if (variant == null || variant.Sockets == null)
                return Array.Empty<MaskWorkpieceSocketView>();

            return variant.Sockets;
        }

        private MaskWorkpieceSocketView FindSocketView(MaskSegment segment, MaskSocket socket)
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(segment);
            for (int i = 0; i < sockets.Count; i++)
            {
                if (sockets[i] != null && sockets[i].Socket == socket)
                    return sockets[i];
            }

            return null;
        }
        private MaskSegment FindSegmentForSocketView(MaskWorkpieceSocketView socketView, MaskSegment fallback)
        {
            if (socketView == null)
                return fallback;

            EnsureConfiguredViews();
            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null)
                    continue;

                MaskWorkpieceShapeVariant variant = view.GetShapeVariant(GetShapeIndex(view.Segment));
                if (variant == null || variant.Sockets == null)
                    continue;

                IReadOnlyList<MaskWorkpieceSocketView> sockets = variant.Sockets;
                for (int s = 0; s < sockets.Count; s++)
                {
                    if (ReferenceEquals(sockets[s], socketView))
                        return view.Segment;
                }
            }

            return fallback;
        }

        private bool HasAnyPresentSegment()
        {
            foreach (SegmentRuntime runtime in segments.Values)
            {
                if (runtime.IsPresent)
                    return true;
            }
            return false;
        }

        private MaskSegment FindFirstPresentSegment()
        {
            for (int i = 0; i < SegmentOrder.Length; i++)
            {
                if (IsSegmentPresent(SegmentOrder[i]))
                    return SegmentOrder[i];
            }
            return MaskSegment.Middle;
        }

        private MaskSize ResolveSizeFromPresence()
        {
            bool upper = IsSegmentPresent(MaskSegment.Upper);
            bool middle = IsSegmentPresent(MaskSegment.Middle);
            bool lower = IsSegmentPresent(MaskSegment.Lower);

            if (upper && middle && lower)
                return MaskSize.Large;
            if (middle && lower && !upper)
                return MaskSize.Medium;
            if (upper && middle && !lower)
                return MaskSize.Medium;
            if (middle && !upper && !lower)
                return MaskSize.Small;

            // На случай будущих нестандартных рецептов: не падаем, а оставляем ближайший безопасный MVP-вариант.
            return MaskSize.Large;
        }
    }
}
