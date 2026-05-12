using System;
using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    /// <summary>
    /// Тупая болванка на отдельном socket стола.
    /// Не знает про формы, сокеты и финальную маску: только материал, три части и отрез.
    /// </summary>
    public class MaskBlankWorkpiece : MonoBehaviour
    {
        private class SegmentRuntime
        {
            public bool IsPresent = true;
            public bool IsMarkedForCut;
        }

        [Header("Debug Primitive Fallback")]
        [SerializeField] private bool buildPrimitiveFallbackIfEmpty = true;
        [SerializeField] private Vector3 primitiveSegmentScale = new(0.8f, 0.18f, 0.35f);
        [SerializeField] private float primitiveSegmentOffset = 0.22f;

        [Header("Configured Views")]
        [SerializeField] private List<MaskWorkpieceSegmentView> segmentViews = new();

        private readonly Dictionary<MaskSegment, SegmentRuntime> segments = new();
        private ResourceType sourceBlankType = ResourceType.None;
        private MaskSegment selectedSegment = MaskSegment.Middle;

        public ResourceType SourceBlankType => sourceBlankType;
        public MaskSegment SelectedSegment => selectedSegment;

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
            RefreshVisuals(MaskWorkbenchState.SizeSelection);
        }

        public void SetViewMode(MaskWorkbenchState mode)
        {
            RefreshVisuals(mode);
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
                RefreshVisuals(MaskWorkbenchState.SizeSelection);
                return;
            }
        }

        public bool ToggleSelectedSegmentCutMark()
        {
            return ToggleSegmentCutMark(selectedSegment);
        }

        public bool ToggleSegmentCutMark(MaskSegment segment)
        {
            EnsureRuntimeState();

            if (!segments.TryGetValue(segment, out SegmentRuntime runtime) || !runtime.IsPresent)
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
            RefreshVisuals(MaskWorkbenchState.SizeSelection);
            return true;
        }

        public void ApplyMarkedCuts()
        {
            EnsureRuntimeState();

            foreach (KeyValuePair<MaskSegment, SegmentRuntime> pair in segments)
            {
                if (!pair.Value.IsMarkedForCut)
                    continue;

                pair.Value.IsPresent = false;
                pair.Value.IsMarkedForCut = false;
            }

            if (!HasAnyPresentSegment())
                segments[MaskSegment.Middle].IsPresent = true;

            if (!IsSegmentPresent(selectedSegment))
                selectedSegment = FindFirstPresentSegment();

            RefreshVisuals(MaskWorkbenchState.FormSelection);
        }

        public bool[] BuildPresenceSnapshot()
        {
            EnsureRuntimeState();
            return new[]
            {
                IsSegmentPresent(MaskSegment.Upper),
                IsSegmentPresent(MaskSegment.Middle),
                IsSegmentPresent(MaskSegment.Lower)
            };
        }

        public Transform GetSelectionAnchor(MaskSegment segment)
        {
            MaskWorkpieceSegmentView view = FindView(segment);
            return view != null ? view.SelectionAnchor : transform;
        }

        public Transform GetSelectedSelectionAnchor()
        {
            return GetSelectionAnchor(selectedSegment);
        }

        private void RefreshVisuals(MaskWorkbenchState mode)
        {
            EnsureRuntimeState();
            EnsureConfiguredViews();

            bool showBlank = mode == MaskWorkbenchState.SizeSelection || mode == MaskWorkbenchState.FormSelection;

            for (int i = 0; i < segmentViews.Count; i++)
            {
                MaskWorkpieceSegmentView view = segmentViews[i];
                if (view == null)
                    continue;

                bool present = IsSegmentPresent(view.Segment);
                view.EnsureRootActive();
                view.SetBlankMeshVisible(showBlank && present);
                view.SetMarkedForCut(showBlank && present && IsSegmentMarkedForCut(view.Segment));
                view.RefreshShape(0, false, false, null);
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
            root.name = $"{segment}_BlankSegment";
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, localY, 0f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = primitiveSegmentScale;

            MaskWorkpieceSegmentView view = root.AddComponent<MaskWorkpieceSegmentView>();
            view.ConfigureFallback(segment);
            segmentViews.Add(view);
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
    }
}
