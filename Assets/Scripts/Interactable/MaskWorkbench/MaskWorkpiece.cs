using System;
using System.Collections.Generic;
using Enums;
using Helpers;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Runtime-заготовка на столе. Это не ItemBase и не ресурс с подноса.
    /// Визуал должен собираться руками в prefab: сегменты -> shape variants -> sockets.
    /// </summary>
    public class MaskWorkpiece : MonoBehaviour, IMaskSocketSelectionOwner
    {
        [Serializable]
        public struct PlannedInlay
        {
            public MaskSegment Segment;
            public MaskSocket Socket;
            public ResourceType ResourceType;
        }

        private class SegmentRuntime
        {
            public bool IsPresent = true;
            public bool IsMarkedForCut;
            public int ShapeIndex;
        }

        [Header("Debug Primitive Fallback")]
        [SerializeField] private bool buildPrimitiveFallbackIfEmpty = true;
        [SerializeField] private Vector3 primitiveSegmentScale = new(0.8f, 0.18f, 0.35f);
        [SerializeField] private float primitiveSegmentOffset = 0.22f;

        [Header("Configured Views")]
        [SerializeField] private List<MaskWorkpieceSegmentView> segmentViews = new();
        [SerializeField] private MaskWorkpieceSelectorView segmentSelector;
        [SerializeField] private MaskWorkpieceSelectorView socketSelector;

        private readonly Dictionary<MaskSegment, SegmentRuntime> segments = new();
        private readonly List<PlannedInlay> plannedInlays = new();

        private ResourceType sourceBlankType = ResourceType.None;
        private MaskWorkbenchState viewMode = MaskWorkbenchState.SizeSelection;
        private MaskSegment selectedSegment = MaskSegment.Middle;
        private int selectedSocketIndex;

        public ResourceType SourceBlankType => sourceBlankType;
        public MaskSegment SelectedSegment => selectedSegment;
        public int SelectedSocketIndex => selectedSocketIndex;
        public IReadOnlyList<PlannedInlay> PlannedInlays => plannedInlays;

        private static readonly MaskSegment[] SegmentOrder =
        {
            MaskSegment.Upper,
            MaskSegment.Middle,
            MaskSegment.Lower
        };

        public void Init(ResourceType blankType)
        {
            sourceBlankType = blankType;
            EnsureRuntimeState();
            EnsureConfiguredViews();
            EnsureFallbackVisual();

            selectedSegment = IsSegmentPresent(MaskSegment.Middle) ? MaskSegment.Middle : FindFirstPresentSegment();
            selectedSocketIndex = 0;
            RefreshVisuals();
        }

        public void SetViewMode(MaskWorkbenchState mode)
        {
            viewMode = mode;
            RefreshVisuals();
        }

        public bool IsSegmentPresent(MaskSegment segment)
        {
            EnsureRuntimeState();
            return segments.TryGetValue(segment, out SegmentRuntime runtime) && runtime.IsPresent;
        }

        public bool IsSegmentMarkedForCut(MaskSegment segment)
        {
            EnsureRuntimeState();
            return segments.TryGetValue(segment, out SegmentRuntime runtime) && runtime.IsMarkedForCut;
        }

        public void SelectPreviousSegment()
        {
            SelectSegmentDelta(-1);
        }

        public void SelectNextSegment()
        {
            SelectSegmentDelta(1);
        }

        public void SelectSegmentDelta(int delta)
        {
            int index = Array.IndexOf(SegmentOrder, selectedSegment);
            if (index < 0) index = 1;

            for (int step = 0; step < SegmentOrder.Length; step++)
            {
                index += delta;
                if (index < 0) index = SegmentOrder.Length - 1;
                if (index >= SegmentOrder.Length) index = 0;

                // На этапе разметки размера можно ходить по present-сегментам, даже если они уже помечены на отрезание.
                // На следующих этапах помеченные уже должны быть физически удалены через ApplyMarkedCuts().
                if (!IsSegmentPresent(SegmentOrder[index]))
                    continue;

                selectedSegment = SegmentOrder[index];
                selectedSocketIndex = 0;
                RefreshVisuals();
                return;
            }
        }

        /// <summary>
        /// Разметить/снять разметку на удаление. Визуал сегмента НЕ выключается до ApplyMarkedCuts().
        /// </summary>
        public bool ToggleSelectedSegmentCutMark()
        {
            return ToggleSegmentCutMark(selectedSegment);
        }

        public bool ToggleSegmentCutMark(MaskSegment segment)
        {
            EnsureRuntimeState();

            if (!segments.TryGetValue(segment, out SegmentRuntime runtime))
                return false;

            if (!runtime.IsPresent)
                return false;

            if (segment == MaskSegment.Middle && !runtime.IsMarkedForCut)
            {
                Debug.LogWarning($"{name}: middle segment cannot be marked for cut in MVP.");
                return false;
            }

            bool nextMarked = !runtime.IsMarkedForCut;

            if (nextMarked && CountMarkedForCut() >= 2)
            {
                Debug.LogWarning($"{name}: cannot mark more than two segments for cut in MVP.");
                return false;
            }

            runtime.IsMarkedForCut = nextMarked;
            RefreshVisuals();
            return true;
        }

        public void ApplyMarkedCuts()
        {
            EnsureRuntimeState();

            foreach (KeyValuePair<MaskSegment, SegmentRuntime> pair in segments)
            {
                SegmentRuntime runtime = pair.Value;
                if (!runtime.IsMarkedForCut)
                    continue;

                runtime.IsPresent = false;
                runtime.IsMarkedForCut = false;
            }

            if (!HasAnyPresentSegment())
                segments[MaskSegment.Middle].IsPresent = true;

            if (!IsSegmentPresent(selectedSegment))
                selectedSegment = FindFirstPresentSegment();

            selectedSocketIndex = 0;
            RefreshVisuals();
        }

        public void SelectPreviousShape()
        {
            ChangeShape(-1);
        }

        public void SelectNextShape()
        {
            ChangeShape(1);
        }

        public void ChangeShape(int delta)
        {
            EnsureRuntimeState();

            if (!segments.TryGetValue(selectedSegment, out SegmentRuntime runtime))
                return;

            if (!runtime.IsPresent)
                return;

            MaskWorkpieceSegmentView view = FindView(selectedSegment);
            int variantCount = view != null ? view.ShapeVariantCount : 1;
            if (variantCount <= 0)
                variantCount = 1;

            runtime.ShapeIndex += delta;
            if (runtime.ShapeIndex < 0) runtime.ShapeIndex = variantCount - 1;
            if (runtime.ShapeIndex >= variantCount) runtime.ShapeIndex = 0;

            selectedSocketIndex = 0;
            RefreshVisuals();
        }

        public void SolidifySelectedShapes()
        {
            EnsureRuntimeState();
            EnsureConfiguredViews();

            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null)
                    continue;

                if (!IsSegmentPresent(view.Segment))
                    continue;

                int shapeIndex = GetShapeIndex(view.Segment);
                view.SetBlankMeshVisible(false);
                view.SolidifyShape(shapeIndex);
            }
        }

        public void SelectPreviousSocket()
        {
            ChangeSocket(-1);
        }

        public void SelectNextSocket()
        {
            ChangeSocket(1);
        }

        public void ChangeSocket(int delta)
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(selectedSegment);
            if (sockets.Count == 0)
                return;

            selectedSocketIndex += delta;
            if (selectedSocketIndex < 0) selectedSocketIndex = sockets.Count - 1;
            if (selectedSocketIndex >= sockets.Count) selectedSocketIndex = 0;

            RefreshVisuals();
        }

        public MaskSocket GetSelectedSocket()
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(selectedSegment);
            if (sockets.Count == 0)
                return MaskSocket.None;

            selectedSocketIndex = Mathf.Clamp(selectedSocketIndex, 0, sockets.Count - 1);
            return sockets[selectedSocketIndex] != null ? sockets[selectedSocketIndex].Socket : MaskSocket.None;
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

            // Legacy fallback: старый MaskWorkpiece всё ещё хранит planned по enum-сокету.
            for (int i = 0; i < plannedInlays.Count; i++)
            {
                PlannedInlay planned = plannedInlays[i];
                if (planned.Segment == selectedSegment && planned.Socket == socketView.Socket && planned.ResourceType != ResourceType.None)
                    return true;
            }

            return false;
        }

        public bool PlanInlayOnSelectedSocket(ResourceType resourceType)
        {
            if (!ResourceTypeHelper.IsInlay(resourceType))
                return false;

            MaskSocket socket = GetSelectedSocket();
            if (socket == MaskSocket.None)
                return false;

            for (int i = 0; i < plannedInlays.Count; i++)
            {
                PlannedInlay planned = plannedInlays[i];
                if (planned.Segment != selectedSegment || planned.Socket != socket)
                    continue;

                planned.ResourceType = resourceType;
                plannedInlays[i] = planned;
                RefreshVisuals();
                Debug.Log($"{name}: updated planned inlay {resourceType} at {selectedSegment}/{socket}.");
                return true;
            }

            plannedInlays.Add(new PlannedInlay
            {
                Segment = selectedSegment,
                Socket = socket,
                ResourceType = resourceType
            });

            RefreshVisuals();
            Debug.Log($"{name}: planned inlay {resourceType} at {selectedSegment}/{socket}.");
            return true;
        }

        public Dictionary<ResourceType, List<PlannedInlay>> BuildPlannedInlayGroups()
        {
            Dictionary<ResourceType, List<PlannedInlay>> result = new();

            for (int i = 0; i < plannedInlays.Count; i++)
            {
                PlannedInlay planned = plannedInlays[i];
                if (!ResourceTypeHelper.IsInlay(planned.ResourceType))
                    continue;

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
            int count = group != null ? group.Count : 0;
            Debug.Log($"{name}: installed inlay group {resourceType}, count={count}.");
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

        private void EnsureFallbackVisual()
        {
            if (!buildPrimitiveFallbackIfEmpty || segmentViews.Count > 0)
                return;

            CreatePrimitiveSegment(MaskSegment.Upper, primitiveSegmentOffset);
            CreatePrimitiveSegment(MaskSegment.Middle, 0f);
            CreatePrimitiveSegment(MaskSegment.Lower, -primitiveSegmentOffset);
        }

        private void CreatePrimitiveSegment(MaskSegment segment, float localY)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = $"{segment}_Segment";
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, localY, 0f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = primitiveSegmentScale;

            MaskWorkpieceSegmentView view = root.AddComponent<MaskWorkpieceSegmentView>();
            view.ConfigureFallback(segment);
            segmentViews.Add(view);
        }

        private void RefreshVisuals()
        {
            EnsureRuntimeState();
            EnsureConfiguredViews();

            bool sizeMode = viewMode == MaskWorkbenchState.SizeSelection;
            bool formMode = viewMode == MaskWorkbenchState.FormSelection;
            bool inlayMode = viewMode == MaskWorkbenchState.InlaySelection;
            bool completed = viewMode == MaskWorkbenchState.Completed;
            bool miniGame = viewMode == MaskWorkbenchState.MiniGame;

            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null)
                    continue;

                bool present = IsSegmentPresent(view.Segment);
                bool marked = IsSegmentMarkedForCut(view.Segment);

                // MaskPartRoot must stay active: it is the container for markers, shape variants and sockets.
                // Stage visibility is controlled by the segment MeshRenderer and child variant GameObjects.
                view.EnsureRootActive();

                bool showBlankMesh = present && (sizeMode || formMode);
                bool showShape = present && (formMode || inlayMode || completed);

                view.SetBlankMeshVisible(showBlankMesh);
                view.SetMarkedForCut(present && marked && sizeMode);

                int shapeIndex = GetShapeIndex(view.Segment);
                view.RefreshShape(shapeIndex, showShape, inlayMode, this, inlayMode || completed);
            }

            bool showSegmentSelector = !completed && !miniGame && (sizeMode || formMode || inlayMode) && IsSegmentPresent(selectedSegment);
            if (showSegmentSelector)
                segmentSelector?.ShowAt(GetSegmentSelectionAnchor(selectedSegment));
            else
                segmentSelector?.Hide();

            bool showSocketSelector = inlayMode && IsSegmentPresent(selectedSegment) && GetSelectedSocketView() != null;
            if (showSocketSelector)
                socketSelector?.ShowAt(GetSelectedSocketView().SelectionAnchor);
            else
                socketSelector?.Hide();
        }

        private int GetShapeIndex(MaskSegment segment)
        {
            return segments.TryGetValue(segment, out SegmentRuntime runtime) ? runtime.ShapeIndex : 0;
        }

        private int CountMarkedForCut()
        {
            int count = 0;
            foreach (SegmentRuntime runtime in segments.Values)
            {
                if (runtime.IsMarkedForCut)
                    count++;
            }

            return count;
        }

        private MaskWorkpieceSegmentView FindView(MaskSegment segment)
        {
            for (int i = 0; i < segmentViews.Count; i++)
            {
                if (segmentViews[i] != null && segmentViews[i].Segment == segment)
                    return segmentViews[i];
            }

            return null;
        }

        private Transform GetSegmentSelectionAnchor(MaskSegment segment)
        {
            MaskWorkpieceSegmentView view = FindView(segment);
            return view != null ? view.SelectionAnchor : transform;
        }

        private MaskWorkpieceSocketView GetSelectedSocketView()
        {
            IReadOnlyList<MaskWorkpieceSocketView> sockets = GetActiveSocketViewsForSegment(selectedSegment);
            if (sockets.Count == 0)
                return null;

            selectedSocketIndex = Mathf.Clamp(selectedSocketIndex, 0, sockets.Count - 1);
            return sockets[selectedSocketIndex];
        }

        private IReadOnlyList<MaskWorkpieceSocketView> GetActiveSocketViewsForSegment(MaskSegment segment)
        {
            MaskWorkpieceSegmentView view = FindView(segment);
            if (view == null)
                return Array.Empty<MaskWorkpieceSocketView>();

            int shapeIndex = GetShapeIndex(segment);
            MaskWorkpieceShapeVariant variant = view.GetShapeVariant(shapeIndex);
            if (variant == null)
                return Array.Empty<MaskWorkpieceSocketView>();

            return variant.Sockets;
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
            if (IsSegmentPresent(MaskSegment.Middle)) return MaskSegment.Middle;
            if (IsSegmentPresent(MaskSegment.Upper)) return MaskSegment.Upper;
            return MaskSegment.Lower;
        }
    }
}
