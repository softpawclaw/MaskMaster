using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
    public class OrderTicketSystem : MonoBehaviour
    {
        private enum TicketRuntimeState
        {
            Free = 0,
            AssignedToQuest = 1,
            AtCustomer = 2,
            AtWindow = 3
        }

        [Serializable]
        private class TicketSlot
        {
            public int TicketNumber;
            public GameObject TicketObject;

            [NonSerialized] public Transform OriginalParent;
            [NonSerialized] public Vector3 OriginalLocalPosition;
            [NonSerialized] public Quaternion OriginalLocalRotation;
            [NonSerialized] public TicketRuntimeState State;
            [NonSerialized] public string QuestId;
        }

        [Header("Tickets")]
        [SerializeField] private TicketSlot[] tickets;

        [Header("Window Points")]
        [SerializeField] private Transform windowSpot;
        [SerializeField] private Transform customerPoint;

        [Header("Movement")]
        [SerializeField] private float moveDuration = 0.35f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Debug")]
        [SerializeField] private bool logTickets = true;

        private readonly Dictionary<string, int> questToTicket = new();

        private void Awake()
        {
            CacheOriginalTicketTransforms();
        }

        public void Link()
        {
            CacheOriginalTicketTransforms();
        }

        public bool HasTicketForQuest(string questId)
        {
            return !string.IsNullOrWhiteSpace(questId) && questToTicket.ContainsKey(questId);
        }

        public bool TryGetTicketForQuest(string questId, out int ticketNumber)
        {
            ticketNumber = 0;
            if (string.IsNullOrWhiteSpace(questId)) return false;
            return questToTicket.TryGetValue(questId, out ticketNumber);
        }

        public bool TryAssignFreeTicket(string questId, out int ticketNumber)
        {
            ticketNumber = 0;

            if (string.IsNullOrWhiteSpace(questId))
            {
                Debug.LogWarning("OrderTicketSystem: questId is empty.");
                return false;
            }

            if (questToTicket.TryGetValue(questId, out ticketNumber))
                return true;

            TicketSlot slot = FindLowestFreeTicket();
            if (slot == null)
            {
                Debug.LogWarning("OrderTicketSystem: no free tickets.");
                return false;
            }

            slot.State = TicketRuntimeState.AssignedToQuest;
            slot.QuestId = questId;
            questToTicket[questId] = slot.TicketNumber;
            ticketNumber = slot.TicketNumber;

            if (logTickets)
                Debug.Log($"OrderTicketSystem: assigned ticket {ticketNumber} to quest {questId}.");

            return true;
        }


        public void PlaceTicketAtWindowSpotInstant(int ticketNumber)
        {
            TicketSlot slot = FindTicket(ticketNumber);
            if (slot == null) return;
            if (slot.TicketObject == null) return;
            if (windowSpot == null)
            {
                Debug.LogWarning("OrderTicketSystem: windowSpot is not assigned.");
                return;
            }

            Transform ticketTransform = slot.TicketObject.transform;
            ticketTransform.SetParent(windowSpot, false);
            ticketTransform.localPosition = Vector3.zero;
            ticketTransform.localRotation = Quaternion.identity;
            slot.TicketObject.SetActive(true);
            slot.State = TicketRuntimeState.AtWindow;

            if (logTickets)
                Debug.Log($"OrderTicketSystem: ticket {ticketNumber} placed at window spot instantly.");
        }

        public IEnumerator MoveTicketToWindowSpot(int ticketNumber)
        {
            TicketSlot slot = FindTicket(ticketNumber);
            if (slot == null) yield break;
            if (slot.TicketObject == null) yield break;

            yield return MoveTicket(slot, windowSpot, true);
            slot.State = TicketRuntimeState.AtWindow;
        }

        public IEnumerator MoveTicketFromWindowToCustomer(int ticketNumber)
        {
            TicketSlot slot = FindTicket(ticketNumber);
            if (slot == null) yield break;
            if (slot.TicketObject == null) yield break;

            if (customerPoint != null)
                yield return MoveTicket(slot, customerPoint, true);

            slot.TicketObject.SetActive(false);
            slot.State = TicketRuntimeState.AtCustomer;

            if (logTickets)
                Debug.Log($"OrderTicketSystem: ticket {ticketNumber} moved to customer.");
        }

        public IEnumerator MoveTicketFromCustomerToWindow(int ticketNumber)
        {
            TicketSlot slot = FindTicket(ticketNumber);
            if (slot == null) yield break;
            if (slot.TicketObject == null) yield break;

            if (customerPoint != null)
            {
                slot.TicketObject.transform.SetParent(customerPoint.parent, true);
                slot.TicketObject.transform.SetPositionAndRotation(customerPoint.position, customerPoint.rotation);
            }

            slot.TicketObject.SetActive(true);

            yield return MoveTicket(slot, windowSpot, true);
            slot.State = TicketRuntimeState.AtWindow;

            if (logTickets)
                Debug.Log($"OrderTicketSystem: ticket {ticketNumber} returned to window.");
        }

        public void ReturnTicketToRack(string questId)
        {
            if (!TryGetTicketForQuest(questId, out int ticketNumber))
                return;

            TicketSlot slot = FindTicket(ticketNumber);
            if (slot == null)
                return;

            ResetTicketToRack(slot);

            questToTicket.Remove(questId);
            slot.QuestId = string.Empty;
            slot.State = TicketRuntimeState.Free;

            if (logTickets)
                Debug.Log($"OrderTicketSystem: ticket {ticketNumber} returned to rack.");
        }

        public void ReturnAllTicketsToRack()
        {
            questToTicket.Clear();

            if (tickets == null) return;

            for (int i = 0; i < tickets.Length; i++)
            {
                ResetTicketToRack(tickets[i]);
                tickets[i].QuestId = string.Empty;
                tickets[i].State = TicketRuntimeState.Free;
            }
        }

        private IEnumerator MoveTicket(TicketSlot slot, Transform target, bool worldSpace)
        {
            if (slot == null || slot.TicketObject == null || target == null)
                yield break;

            Transform ticketTransform = slot.TicketObject.transform;
            slot.TicketObject.SetActive(true);

            Vector3 startPosition = ticketTransform.position;
            Quaternion startRotation = ticketTransform.rotation;
            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;

            ticketTransform.SetParent(target.parent, true);

            float safeDuration = Mathf.Max(0.01f, moveDuration);
            float time = 0f;

            while (time < safeDuration)
            {
                time += Time.deltaTime;
                float t = moveCurve != null ? moveCurve.Evaluate(Mathf.Clamp01(time / safeDuration)) : Mathf.Clamp01(time / safeDuration);

                ticketTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                ticketTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

                yield return null;
            }

            ticketTransform.SetParent(target, true);
            ticketTransform.localPosition = Vector3.zero;
            ticketTransform.localRotation = Quaternion.identity;
        }

        private void CacheOriginalTicketTransforms()
        {
            if (tickets == null) return;

            for (int i = 0; i < tickets.Length; i++)
            {
                TicketSlot slot = tickets[i];
                if (slot == null || slot.TicketObject == null)
                    continue;

                Transform t = slot.TicketObject.transform;
                slot.OriginalParent = t.parent;
                slot.OriginalLocalPosition = t.localPosition;
                slot.OriginalLocalRotation = t.localRotation;
            }
        }

        private void ResetTicketToRack(TicketSlot slot)
        {
            if (slot == null || slot.TicketObject == null)
                return;

            Transform t = slot.TicketObject.transform;
            t.SetParent(slot.OriginalParent, false);
            t.localPosition = slot.OriginalLocalPosition;
            t.localRotation = slot.OriginalLocalRotation;
            slot.TicketObject.SetActive(true);
        }

        private TicketSlot FindLowestFreeTicket()
        {
            TicketSlot result = null;

            if (tickets == null)
                return null;

            for (int i = 0; i < tickets.Length; i++)
            {
                TicketSlot slot = tickets[i];
                if (slot == null) continue;
                if (slot.State != TicketRuntimeState.Free) continue;
                if (slot.TicketObject == null) continue;

                if (result == null || slot.TicketNumber < result.TicketNumber)
                    result = slot;
            }

            return result;
        }

        private TicketSlot FindTicket(int ticketNumber)
        {
            if (tickets == null)
                return null;

            for (int i = 0; i < tickets.Length; i++)
            {
                if (tickets[i] != null && tickets[i].TicketNumber == ticketNumber)
                    return tickets[i];
            }

            Debug.LogWarning($"OrderTicketSystem: ticket {ticketNumber} not found.");
            return null;
        }
    }
}
