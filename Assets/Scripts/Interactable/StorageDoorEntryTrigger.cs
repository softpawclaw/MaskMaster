using UnityEngine;

namespace Interactable
{
    /// <summary>
    /// Trigger zone placed just inside the storage room.
    /// It closes the storage door once after the player has been allowed to enter.
    /// The trigger is armed through StorageDoorController.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StorageDoorEntryTrigger : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private StorageDoorController storageDoorController;

        [Header("Debug")]
        [SerializeField] private bool logTrigger = true;

        private void Awake()
        {
            if (storageDoorController == null)
                storageDoorController = GetComponentInParent<StorageDoorController>();

            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null && !triggerCollider.isTrigger)
            {
                triggerCollider.isTrigger = true;

                if (logTrigger)
                    Debug.Log($"{name}: Collider was not trigger. Switched isTrigger=true.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            if (storageDoorController == null)
            {
                Debug.LogWarning($"{name}: StorageDoorController is not assigned.");
                return;
            }

            bool consumed = storageDoorController.TryCloseDoorFromEntryTrigger(gameObject);

            if (logTrigger && !consumed)
                Debug.Log($"{name}: player entered, but trigger was not armed.");
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
                return false;

            return other.GetComponentInParent<PlayerController>() != null
                   || other.GetComponentInParent<Player.PlayerHandsController>() != null;
        }
    }
}
