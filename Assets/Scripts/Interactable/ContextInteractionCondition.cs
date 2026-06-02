using System;
using Enums;
using Global;
using Items;
using Player;
using Systems;
using UnityEngine;

namespace Interactable
{
    public class ContextInteractionCondition : MonoBehaviour
    {
        public enum InteractionMoment
        {
            BeforeInteraction,
            AfterInteraction
        }

        public enum ReactionType
        {
            PlayDialogueAndBlock,
            PlayDialogueAndContinue,
            BlockOnly
        }

        public enum HandValidationMode
        {
            None,
            ValidStorageEntryPaperStack,
            ValidStorageExitTray
        }

        [Serializable]
        public class Rule
        {
            [Header("Moment")]
            public InteractionMoment Moment = InteractionMoment.BeforeInteraction;

            [Header("Day")]
            public bool CheckDay;
            public int Day;

            [Header("Quest")]
            public bool CheckQuestId;
            public string QuestId;
            public bool CheckQuestState;
            public QuestState QuestState;

            [Header("Hand Validation")]
            public HandValidationMode HandValidation = HandValidationMode.None;

            [Header("Dialogue History")]
            public string[] RequiredShownDialogues;
            public string[] ForbiddenShownDialogues;

            [Header("Reaction")]
            public ReactionType ReactionType = ReactionType.PlayDialogueAndBlock;
            [Tooltip("If false, DialogueIds are played as an ordered sequence. If true, one random id is selected from DialogueIds.")]
            public bool RandomDialogueFromPool = false;
            public string[] DialogueIds;
        }

        [SerializeField] private Rule[] rules;

        private DaySystem daySystem;
        private QuestSystem questSystem;
        private UISystem uiSystem;
        private DialogueHistorySystem dialogueHistorySystem;

        public bool TryExecute(InteractionMoment moment, GameObject interactor, Action<bool> completed)
        {
            EnsureLinked();

            if (rules == null || rules.Length == 0) return false;

            for (int i = 0; i < rules.Length; i++)
            {
                Rule rule = rules[i];
                if (rule == null) continue;
                if (rule.Moment != moment) continue;
                if (!Matches(rule, interactor)) continue;

                ExecuteRule(rule, interactor, completed);
                return true;
            }

            return false;
        }

        private bool Matches(Rule rule, GameObject interactor)
        {
            if (rule.CheckDay)
            {
                if (daySystem == null || daySystem.CurrentDay != rule.Day) return false;
            }

            if (rule.CheckQuestId)
            {
                if (questSystem == null || questSystem.CurrentQuestId != rule.QuestId) return false;
            }

            if (rule.CheckQuestState)
            {
                if (questSystem == null || questSystem.CurrentState != rule.QuestState) return false;
            }


            if (!MatchesHandValidation(rule, interactor))
                return false;

            if (dialogueHistorySystem != null)
            {
                if (!dialogueHistorySystem.WereAllShown(rule.RequiredShownDialogues)) return false;
                if (dialogueHistorySystem.WasAnyShown(rule.ForbiddenShownDialogues)) return false;
            }
            else
            {
                if (HasAnyConfigured(rule.RequiredShownDialogues) || HasAnyConfigured(rule.ForbiddenShownDialogues)) return false;
            }

            return true;
        }


        private bool MatchesHandValidation(Rule rule, GameObject interactor)
        {
            if (rule.HandValidation == HandValidationMode.None)
                return true;

            PlayerHandsController hands = interactor != null ? interactor.GetComponent<PlayerHandsController>() : null;
            if (hands == null)
                return false;

            switch (rule.HandValidation)
            {
                case HandValidationMode.ValidStorageEntryPaperStack:
                {
                    PaperStackItem stack = hands.GetFirstItemInHands<PaperStackItem>();
                    return stack != null && stack.IsValidStorageEntryStack(out _);
                }

                case HandValidationMode.ValidStorageExitTray:
                {
                    TrayItem tray = hands.GetTrayInHands();
                    if (tray == null)
                        return false;

                    MainRecipeItem recipe = FindMainRecipeInHands(hands);
                    return recipe != null && tray.IsValidStorageExitTray(recipe, out _);
                }

                default:
                    return true;
            }
        }

        private static MainRecipeItem FindMainRecipeInHands(PlayerHandsController hands)
        {
            if (hands == null)
                return null;

            MainRecipeItem direct = hands.GetFirstItemInHands<MainRecipeItem>();
            if (direct != null)
                return direct;

            PaperStackItem stack = hands.GetFirstItemInHands<PaperStackItem>();
            return stack != null ? stack.GetMainRecipe() : null;
        }

        private void ExecuteRule(Rule rule, GameObject interactor, Action<bool> completed)
        {
            bool allowContinue = rule.ReactionType == ReactionType.PlayDialogueAndContinue;

            if (rule.ReactionType == ReactionType.BlockOnly)
            {
                completed?.Invoke(false);
                return;
            }

            if (uiSystem == null || !HasAnyConfigured(rule.DialogueIds))
            {
                completed?.Invoke(allowContinue);
                return;
            }

            if (rule.RandomDialogueFromPool)
            {
                string randomDialogueId = GetRandomDialogueId(rule.DialogueIds);
                if (string.IsNullOrWhiteSpace(randomDialogueId))
                {
                    completed?.Invoke(allowContinue);
                    return;
                }

                uiSystem.Execute(randomDialogueId, () => completed?.Invoke(allowContinue));
                return;
            }

            ExecuteDialogueSequence(rule.DialogueIds, 0, () => completed?.Invoke(allowContinue));
        }

        private void ExecuteDialogueSequence(string[] dialogueIds, int index, Action completed)
        {
            if (dialogueIds == null || index >= dialogueIds.Length)
            {
                completed?.Invoke();
                return;
            }

            string dialogueId = dialogueIds[index];
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                ExecuteDialogueSequence(dialogueIds, index + 1, completed);
                return;
            }

            uiSystem.Execute(dialogueId, () => ExecuteDialogueSequence(dialogueIds, index + 1, completed));
        }

        private void EnsureLinked()
        {
            if (Linker.Instance == null) return;

            if (daySystem == null) daySystem = Linker.Instance.DaySystem;
            if (questSystem == null) questSystem = Linker.Instance.QuestSystem;
            if (uiSystem == null) uiSystem = Linker.Instance.UISystem;
            if (dialogueHistorySystem == null) dialogueHistorySystem = Linker.Instance.DialogueHistorySystem;
        }

        private static string GetRandomDialogueId(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0) return null;

            if (dialogueIds.Length == 1) return dialogueIds[0];

            int index = UnityEngine.Random.Range(0, dialogueIds.Length);
            return dialogueIds[index];
        }

        private static bool HasAnyConfigured(string[] ids)
        {
            if (ids == null || ids.Length == 0) return false;

            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i])) return true;
            }

            return false;
        }
    }
}
