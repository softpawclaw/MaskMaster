using System;
using System.Collections.Generic;
using System.Text;
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

        [Serializable]
        public struct CraftInlayData
        {
            public MaskSegment Segment;
            public MaskSocket Socket;
            public ResourceType ResourceType;

            public CraftInlayData(MaskSegment segment, MaskSocket socket, ResourceType resourceType)
            {
                Segment = segment;
                Socket = socket;
                ResourceType = resourceType;
            }
        }

        [Serializable]
        public struct CraftShapeData
        {
            public MaskSegment Segment;
            public int ShapeIndex;

            public CraftShapeData(MaskSegment segment, int shapeIndex)
            {
                Segment = segment;
                ShapeIndex = shapeIndex;
            }
        }

        [Serializable]
        public struct CraftMiniGameData
        {
            public MaskMiniGameKind Kind;
            public string ConfigId;
            public MaskMiniGameOutcome Outcome;
            public float Score;
            public float CursorT;

            public CraftMiniGameData(MaskMiniGameResult result)
            {
                Kind = result.Kind;
                ConfigId = result.ConfigId;
                Outcome = result.Outcome;
                Score = result.Score;
                CursorT = result.CursorT;
            }
        }

        [Serializable]
        public class CraftResultData
        {
            public string Label;
            public string OrderId;
            public string MaskId;
            public string ClientId;
            public string FaceCoverId;
            public string MistResistanceId;
            public string DistrictId;
            public string FactionId;
            public ResourceType BlankResourceType = ResourceType.None;
            public MaskSize Size = MaskSize.None;
            public float MaxQualityPoints;
            public float ActualQualityPoints;
            public List<CraftShapeData> Shapes = new();
            public List<CraftInlayData> Inlays = new();
            public List<CraftMiniGameData> MiniGames = new();

            public void Clear(string label)
            {
                Label = label;
                OrderId = string.Empty;
                MaskId = string.Empty;
                ClientId = string.Empty;
                FaceCoverId = string.Empty;
                MistResistanceId = string.Empty;
                DistrictId = string.Empty;
                FactionId = string.Empty;
                BlankResourceType = ResourceType.None;
                Size = MaskSize.None;
                MaxQualityPoints = 0f;
                ActualQualityPoints = 0f;
                Shapes.Clear();
                Inlays.Clear();
                MiniGames.Clear();
            }
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

        [Header("Craft Data Log")]
        [SerializeField] private bool logCraftData = true;
        [SerializeField] private CraftResultData expectedCraftData = new();
        [SerializeField] private CraftResultData actualCraftData = new();

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
        public CraftResultData ExpectedCraftData => expectedCraftData;
        public CraftResultData ActualCraftData => actualCraftData;

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
            expectedCraftData.Clear("EXPECTED");
            actualCraftData.Clear("ACTUAL");
            lastMiniGameAnchorIndex = -1;
            currentWorkbenchViewMode = MaskWorkbenchState.CraftSurfaceInspect;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }

        public void BeginCraftDataTracking(MainRecipeItem recipe)
        {
            expectedCraftData.Clear("EXPECTED");
            actualCraftData.Clear("ACTUAL");
            expectedQualityPoints = 0f;
            actualQualityPoints = 0f;

            if (recipe == null)
            {
                Debug.LogWarning($"{name}: cannot fill expected craft data. MainRecipeItem is null.");
                return;
            }

            DBMask.MaskData data = recipe.MaskData;
            FillRecipeIdentity(expectedCraftData, data);
            FillRecipeIdentity(actualCraftData, data);

            expectedCraftData.BlankResourceType = recipe.Material;
            expectedCraftData.Size = recipe.MaskSize;

            FillDefaultExpectedShapes(expectedCraftData, expectedCraftData.Size);
            FillExpectedInlays(expectedCraftData, recipe.Sockets);

            LogCraftBlock("EXPECTED DATA FILLED", expectedCraftData);
        }

        public void SetSourceBlank(ResourceType blankType)
        {
            sourceBlankType = blankType;
            actualCraftData.BlankResourceType = blankType;
            LogCraftBlock("ACTUAL BLANK RECORDED", actualCraftData);
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
            actualCraftData.Size = selectedSize;
            RefreshActualShapesSnapshot();
            LogCraftBlock("ACTUAL SIZE RECORDED", actualCraftData);

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
            RefreshActualShapesSnapshot();
            LogCraftBlock("ACTUAL SHAPE CHANGED", actualCraftData);
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

            RefreshActualShapesSnapshot();
            LogCraftBlock("ACTUAL SHAPES SOLIDIFIED", actualCraftData);
        }

        public void SetWorkbenchViewMode(MaskWorkbenchState mode)
        {
            currentWorkbenchViewMode = mode;
            RefreshCraftVisual(currentWorkbenchViewMode);
        }
        public void AddCraftQualityPoints(float points)
        {
            float safePoints = Mathf.Max(0f, points);
            actualQualityPoints += safePoints;
            actualCraftData.ActualQualityPoints = actualQualityPoints;
            LogCraftBlock("ACTUAL QUALITY POINTS RECORDED", actualCraftData);
        }

        public void AddExpectedQualityPoints(float points)
        {
            float safePoints = Mathf.Max(0f, points);
            expectedQualityPoints += safePoints;
            expectedCraftData.MaxQualityPoints = expectedQualityPoints;
            LogCraftBlock("EXPECTED QUALITY POINTS RECORDED", expectedCraftData);
        }

        public void SetExpectedQualityPoints(float points)
        {
            expectedQualityPoints = Mathf.Max(0f, points);
            expectedCraftData.MaxQualityPoints = expectedQualityPoints;
            LogCraftBlock("EXPECTED QUALITY POINTS SET", expectedCraftData);
        }

        public void RecordMiniGameResult(MaskMiniGameResult result)
        {
            actualCraftData.MiniGames.Add(new CraftMiniGameData(result));
            AddCraftQualityPoints(result.Score);
            LogCraftBlock($"ACTUAL MINI-GAME RECORDED: {result.Kind}", actualCraftData);
        }

        public void RecordAutoCompletedMiniGame(MaskMiniGameRequest request, float score)
        {
            MaskMiniGameResult result = new MaskMiniGameResult(
                request.ConfigId,
                request.Kind,
                MaskMiniGameOutcome.Good,
                Mathf.Max(0f, score),
                1f);

            actualCraftData.MiniGames.Add(new CraftMiniGameData(result));
            AddCraftQualityPoints(score);
            LogCraftBlock($"ACTUAL AUTO MINI-GAME RECORDED: {request.Kind}", actualCraftData);
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
                RecordActualInlay(planned.Segment, socketView.Socket, resourceType);
            }

            Debug.Log($"{name}: installed inlay group {resourceType}, count={group.Count}.");
            LogCraftBlock($"ACTUAL INLAY GROUP RECORDED: {resourceType}", actualCraftData);
        }

        public void LogFinalCraftDataDump()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine($"[{name}] MASK CRAFT FINAL DATA DUMP");
            AppendCraftBlock(builder, expectedCraftData);
            AppendCraftBlock(builder, actualCraftData);
            Debug.Log(builder.ToString());
        }

        private void FillRecipeIdentity(CraftResultData target, DBMask.MaskData data)
        {
            target.OrderId = data.OR_Id;
            target.MaskId = data.Id;
            target.ClientId = data.ClientId;
            target.FaceCoverId = data.FaceCoverId;
            target.MistResistanceId = data.MistResistanceId;
            target.DistrictId = data.DistrictId;
            target.FactionId = data.FactionId;
        }

        private void FillExpectedInlays(CraftResultData target, DBMaskCombination.MaskSocketResource[] sockets)
        {
            target.Inlays.Clear();
            if (sockets == null)
                return;

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i].ResourceType == ResourceType.None)
                    continue;

                target.Inlays.Add(new CraftInlayData(MaskSegment.Middle, sockets[i].Socket, sockets[i].ResourceType));
            }
        }

        private void FillDefaultExpectedShapes(CraftResultData target, MaskSize size)
        {
            target.Shapes.Clear();

            bool upper = size == MaskSize.Large || size == MaskSize.Medium;
            bool middle = size != MaskSize.None;
            bool lower = size == MaskSize.Large;

            if (upper) target.Shapes.Add(new CraftShapeData(MaskSegment.Upper, 0));
            if (middle) target.Shapes.Add(new CraftShapeData(MaskSegment.Middle, 0));
            if (lower) target.Shapes.Add(new CraftShapeData(MaskSegment.Lower, 0));
        }

        private void RefreshActualShapesSnapshot()
        {
            actualCraftData.Shapes.Clear();
            for (int i = 0; i < SegmentOrder.Length; i++)
            {
                MaskSegment segment = SegmentOrder[i];
                if (!IsSegmentPresent(segment))
                    continue;

                actualCraftData.Shapes.Add(new CraftShapeData(segment, GetShapeIndex(segment)));
            }
        }

        private void RecordActualInlay(MaskSegment segment, MaskSocket socket, ResourceType resourceType)
        {
            if (resourceType == ResourceType.None || socket == MaskSocket.None)
                return;

            for (int i = 0; i < actualCraftData.Inlays.Count; i++)
            {
                CraftInlayData existing = actualCraftData.Inlays[i];
                if (existing.Segment != segment || existing.Socket != socket)
                    continue;

                actualCraftData.Inlays[i] = new CraftInlayData(segment, socket, resourceType);
                return;
            }

            actualCraftData.Inlays.Add(new CraftInlayData(segment, socket, resourceType));
        }

        private void LogCraftBlock(string title, CraftResultData data)
        {
            if (!logCraftData)
                return;

            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine($"[{name}] {title}");
            AppendCraftBlock(builder, data);
            Debug.Log(builder.ToString());
        }

        private static void AppendCraftBlock(StringBuilder builder, CraftResultData data)
        {
            builder.AppendLine($"--- {data.Label} ---");
            builder.AppendLine($"OrderId={data.OrderId}, MaskId={data.MaskId}, ClientId={data.ClientId}");
            builder.AppendLine($"Tags: FaceCover={data.FaceCoverId}, MistResistance={data.MistResistanceId}, District={data.DistrictId}, Faction={data.FactionId}");
            builder.AppendLine($"Blank={data.BlankResourceType}, Size={data.Size}, MaxQuality={data.MaxQualityPoints:0.##}, ActualQuality={data.ActualQualityPoints:0.##}");
            builder.AppendLine($"Shapes: {FormatShapes(data.Shapes)}");
            builder.AppendLine($"Inlays: {FormatInlays(data.Inlays)}");
            builder.AppendLine($"MiniGames: {FormatMiniGames(data.MiniGames)}");
        }

        private static string FormatShapes(List<CraftShapeData> shapes)
        {
            if (shapes == null || shapes.Count == 0)
                return "<none>";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < shapes.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(shapes[i].Segment).Append("=Shape_").Append(shapes[i].ShapeIndex);
            }
            return builder.ToString();
        }

        private static string FormatInlays(List<CraftInlayData> inlays)
        {
            if (inlays == null || inlays.Count == 0)
                return "<none>";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < inlays.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(inlays[i].Segment).Append('/').Append(inlays[i].Socket).Append('=').Append(inlays[i].ResourceType);
            }
            return builder.ToString();
        }

        private static string FormatMiniGames(List<CraftMiniGameData> miniGames)
        {
            if (miniGames == null || miniGames.Count == 0)
                return "<none>";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < miniGames.Count; i++)
            {
                if (i > 0) builder.Append(" | ");
                CraftMiniGameData item = miniGames[i];
                builder.Append(item.Kind)
                    .Append('(').Append(item.ConfigId).Append(")=")
                    .Append(item.Outcome)
                    .Append(", score=").Append(item.Score.ToString("0.##"))
                    .Append(", t=").Append(item.CursorT.ToString("0.000"));
            }
            return builder.ToString();
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
