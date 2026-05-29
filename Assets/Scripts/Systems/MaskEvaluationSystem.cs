using System;
using System.Collections.Generic;
using Enums;
using Interactable.MaskWorkbench;
using Items;
using UnityEngine;

namespace Systems
{
    public class MaskEvaluationSystem : MonoBehaviour
    {
        [Serializable]
        public struct EvaluationResult
        {
            public bool IsSuccess;
            public float RecipeMatchScore;
            public float QualityScore;
            public float TotalScore;
            public int MatchedRecipeChecks;
            public int TotalRecipeChecks;
            public int MatchedExpectedInlays;
            public int TotalExpectedInlays;
        }

        private struct InlayKey : IEquatable<InlayKey>
        {
            public readonly MaskSegment Segment;
            public readonly ResourceType ResourceType;

            public InlayKey(MaskSegment segment, ResourceType resourceType)
            {
                Segment = segment;
                ResourceType = resourceType;
            }

            public bool Equals(InlayKey other)
            {
                return Segment == other.Segment && ResourceType == other.ResourceType;
            }

            public override bool Equals(object obj)
            {
                return obj is InlayKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Segment * 397) ^ (int)ResourceType;
                }
            }

            public override string ToString()
            {
                return $"{Segment}:{ResourceType}";
            }
        }

        [Header("Debug")]
        [SerializeField] private bool logEvaluation = true;

        public EvaluationResult LastResult { get; private set; }
        public int EvaluatedMasksCount { get; private set; }
        public float TotalQualityPoints { get; private set; }
        public float TotalQualityScore { get; private set; }

        public void Link()
        {
        }

        public EvaluationResult Evaluate(MaskItem mask)
        {
            EvaluationResult result = default;

            if (mask == null)
            {
                Debug.LogWarning("MaskEvaluationSystem: cannot evaluate null mask.");
                LastResult = result;
                return result;
            }

            var expected = mask.ExpectedCraftData;
            var actual = mask.ActualCraftData;

            int matchedRecipeChecks = 0;
            int totalRecipeChecks = 0;

            AddExactCheck(expected.BlankResourceType == actual.BlankResourceType, ref matchedRecipeChecks, ref totalRecipeChecks);
            AddExactCheck(expected.Size == actual.Size, ref matchedRecipeChecks, ref totalRecipeChecks);

            bool shapesMatch = AreShapeSetsEqual(expected.Shapes, actual.Shapes);
            AddExactCheck(shapesMatch, ref matchedRecipeChecks, ref totalRecipeChecks);

            HashSet<InlayKey> expectedInlays = BuildUniqueInlaySet(expected.Inlays);
            HashSet<InlayKey> actualInlays = BuildUniqueInlaySet(actual.Inlays);
            bool inlaysMatch = expectedInlays.SetEquals(actualInlays);
            AddExactCheck(inlaysMatch, ref matchedRecipeChecks, ref totalRecipeChecks);

            int matchedInlays = CountMatchedInlays(expectedInlays, actualInlays);

            result.MatchedRecipeChecks = matchedRecipeChecks;
            result.TotalRecipeChecks = totalRecipeChecks;
            result.MatchedExpectedInlays = matchedInlays;
            result.TotalExpectedInlays = expectedInlays.Count;
            result.RecipeMatchScore = totalRecipeChecks > 0 ? Mathf.Clamp01((float)matchedRecipeChecks / totalRecipeChecks) : 0f;
            result.QualityScore = expected.MaxQualityPoints > 0f
                ? Mathf.Clamp01(actual.ActualQualityPoints / expected.MaxQualityPoints)
                : 1f;
            result.TotalScore = result.RecipeMatchScore;
            result.IsSuccess = matchedRecipeChecks == totalRecipeChecks;

            EvaluatedMasksCount++;
            TotalQualityPoints += actual.ActualQualityPoints;
            TotalQualityScore += result.QualityScore;

            LastResult = result;

            if (logEvaluation)
            {
                Debug.Log($"MaskEvaluationSystem: mask={mask.ItemId}, success={result.IsSuccess}, " +
                          $"recipe={result.RecipeMatchScore:0.##} ({matchedRecipeChecks}/{totalRecipeChecks}), " +
                          $"blank={expected.BlankResourceType}->{actual.BlankResourceType}, " +
                          $"size={expected.Size}->{actual.Size}, " +
                          $"shapesMatch={shapesMatch}, inlaysMatch={inlaysMatch}, " +
                          $"inlays={matchedInlays}/{expectedInlays.Count}, quality={result.QualityScore:0.##}, " +
                          $"totalQualityPoints={TotalQualityPoints:0.##}");
            }

            return result;
        }

        private static void AddExactCheck(bool passed, ref int matched, ref int total)
        {
            total++;
            if (passed)
                matched++;
        }

        private static bool AreShapeSetsEqual(List<MaskItem.CraftShapeData> expectedShapes, List<MaskItem.CraftShapeData> actualShapes)
        {
            HashSet<MaskSegment> expected = BuildUniqueShapeSet(expectedShapes);
            HashSet<MaskSegment> actual = BuildUniqueShapeSet(actualShapes);

            return expected.SetEquals(actual);
        }

        private static HashSet<MaskSegment> BuildUniqueShapeSet(List<MaskItem.CraftShapeData> shapes)
        {
            HashSet<MaskSegment> result = new HashSet<MaskSegment>();

            if (shapes == null)
                return result;

            for (int i = 0; i < shapes.Count; i++)
            {
                result.Add(shapes[i].Segment);
            }

            return result;
        }

        private static int CountMatchedInlays(HashSet<InlayKey> expected, HashSet<InlayKey> actual)
        {
            int matched = 0;

            foreach (InlayKey key in expected)
            {
                if (actual.Contains(key))
                    matched++;
            }

            return matched;
        }

        private static HashSet<InlayKey> BuildUniqueInlaySet(List<MaskItem.CraftInlayData> inlays)
        {
            HashSet<InlayKey> result = new HashSet<InlayKey>();

            if (inlays == null)
                return result;

            for (int i = 0; i < inlays.Count; i++)
            {
                var inlay = inlays[i];
                if (inlay.ResourceType == ResourceType.None)
                    continue;

                result.Add(new InlayKey(inlay.Segment, inlay.ResourceType));
            }

            return result;
        }
    }
}
