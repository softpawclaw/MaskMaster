using System.Collections.Generic;
using Enums;
using Player;
using UnityEngine;

namespace Items
{
    public class PaperBoardPlaceHolder : ItemPlaceHolder
    {
        [Header("Board Sockets")]
        [SerializeField] private List<Transform> paperSockets = new();

        private readonly List<ItemBase> placedPapers = new();
        private PaperStackItem currentStack;

        protected override bool CanAcceptItem(ItemBase item)
        {
            if (item is not PaperStackItem stack) return false;
            if (stack.Count == 0) return false;
            if (stack.Count > paperSockets.Count) return false;

            return true;
        }

        protected override void PlaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(PlacementType.Hangable, ItemSize.Large);
            if (inItem == null)
            {
                Debug.Log("No suitable paper stack in hands");
                return;
            }

            if (!CanAcceptItem(inItem))
            {
                Debug.Log($"Refused to place item {inItem.ItemId} on paper board");
                return;
            }

            hands.FreeItem(inItem);
            AttachItem(inItem);
        }

        protected override void ReplaceItem(PlayerHandsController hands)
        {
            ReturnCurrentStackToHands(hands);
        }

        protected override void AttachItem(ItemBase item)
        {
            currentItem = item;
            currentStack = item as PaperStackItem;

            if (currentStack == null)
            {
                Debug.LogWarning("PaperBoardPlaceHolder received non-paper stack item");
                return;
            }

            var holderSocket = containerSocket != null ? containerSocket : transform;
            currentStack.transform.SetParent(holderSocket);
            currentStack.transform.localPosition = Vector3.zero;
            currentStack.transform.localRotation = Quaternion.identity;
            currentStack.gameObject.SetActive(true);

            placedPapers.Clear();
            var papers = currentStack.ExtractAllItems();

            for (int i = 0; i < papers.Count; i++)
            {
                var paper = papers[i];
                var socket = paperSockets[i];

                if (paper == null || socket == null) continue;

                paper.gameObject.SetActive(true);
                paper.transform.SetParent(socket);
                paper.transform.localPosition = Vector3.zero;
                paper.transform.localRotation = Quaternion.identity;

                placedPapers.Add(paper);
            }
        }

        private void ReturnCurrentStackToHands(PlayerHandsController hands)
        {
            if (currentStack == null) return;

            var stackToReturn = currentStack;

            if (!hands.GiveItem(stackToReturn))
            {
                Debug.LogWarning("Failed to return paper stack to hands");
                return;
            }

            foreach (var paper in placedPapers)
            {
                if (paper == null) continue;

                var pageSuccess = hands.GiveItem(paper);
                if (!pageSuccess)
                {
                    Debug.LogWarning($"Failed to return page {paper.ItemId} into hands/container");
                }
            }

            placedPapers.Clear();
            currentItem = null;
            currentStack = null;
        }
    }
}