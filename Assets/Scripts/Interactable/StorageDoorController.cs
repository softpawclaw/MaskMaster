using Enums;
using Global;
using Systems;
using UnityEngine;
using MaskWorkbenchComponent = Interactable.MaskWorkbench.MaskWorkbench;

namespace Interactable
{
    /// <summary>
    /// Scenario wrapper for a storage door.
    /// The door itself stays a dumb toggle-interactable; this component decides when it must open/close.
    /// </summary>
    public class StorageDoorController : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private DoorInteractable door;
        [SerializeField] private MaskWorkbenchComponent maskWorkbench;

        [Header("Rules")]
        [SerializeField] private bool closeOnDayStart = true;
        [SerializeField] private bool closeOnProductionStarted = true;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = true;

        private DaySystem daySystem;
        private bool subscribedToDaySystem;
        private bool subscribedToWorkbench;
        private bool entryTriggerArmed;

        private void Awake()
        {
            if (door == null)
                door = GetComponent<DoorInteractable>();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (subscribedToDaySystem && daySystem != null)
                daySystem.OnDayStateChangedDelegate -= OnDayStateChanged;

            if (subscribedToWorkbench && maskWorkbench != null)
                maskWorkbench.ProductionStarted -= OnProductionStarted;

            subscribedToDaySystem = false;
            subscribedToWorkbench = false;
        }

        public void OpenDoor()
        {
            SetDoorOpen(true);
        }

        public void CloseDoor()
        {
            SetDoorOpen(false);
        }

        public void ArmEntryTrigger()
        {
            entryTriggerArmed = true;

            if (logStateChanges)
                Debug.Log($"{name}: storage entry trigger armed.");
        }

        public void DisarmEntryTrigger()
        {
            entryTriggerArmed = false;

            if (logStateChanges)
                Debug.Log($"{name}: storage entry trigger disarmed.");
        }

        public bool TryCloseDoorFromEntryTrigger(GameObject source)
        {
            if (!entryTriggerArmed)
                return false;

            entryTriggerArmed = false;

            if (logStateChanges)
                Debug.Log($"{name}: storage entry trigger consumed by {source?.name}. Closing door.");

            CloseDoor();
            return true;
        }

        private void TrySubscribe()
        {
            if (!subscribedToDaySystem)
            {
                var linker = Linker.Instance;
                if (linker != null && linker.DaySystem != null)
                {
                    daySystem = linker.DaySystem;
                    daySystem.OnDayStateChangedDelegate += OnDayStateChanged;
                    subscribedToDaySystem = true;
                }
            }

            if (!subscribedToWorkbench && maskWorkbench != null)
            {
                maskWorkbench.ProductionStarted += OnProductionStarted;
                subscribedToWorkbench = true;
            }
        }

        private void OnDayStateChanged(EDayState state, int day)
        {
            if (!closeOnDayStart)
                return;

            if (state != EDayState.Start)
                return;

            CloseDoor();
            ArmEntryTrigger();
        }

        private void OnProductionStarted(MaskWorkbenchComponent source)
        {
            if (!closeOnProductionStarted)
                return;

            DisarmEntryTrigger();
            CloseDoor();
        }

        private void SetDoorOpen(bool targetOpen)
        {
            if (door == null)
            {
                Debug.LogWarning($"{name}: StorageDoorController has no DoorInteractable assigned.");
                return;
            }

            if (door.IsMoving)
            {
                Debug.Log($"{name}: storage door is moving, state request ignored. targetOpen={targetOpen}");
                return;
            }

            if (door.IsOpen == targetOpen)
            {
                if (logStateChanges)
                    Debug.Log($"{name}: storage door already {(targetOpen ? "open" : "closed")}. No toggle needed.");
                return;
            }

            if (logStateChanges)
                Debug.Log($"{name}: toggling storage door to {(targetOpen ? "open" : "closed")}.");

            door.Interact(gameObject);
        }
    }
}
