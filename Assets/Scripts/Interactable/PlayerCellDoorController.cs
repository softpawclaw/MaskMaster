using Global;
using Systems;
using UnityEngine;

namespace Interactable
{
    public class PlayerCellDoorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DoorInteractable door;
        [SerializeField] private OrderWindowInteractable orderWindow;
        [SerializeField] private DaySystem daySystem;

        private int trackedDay = -1;
        private bool closeTriggeredToday;

        private void Start()
        {
            ResolveReferences();

            if (daySystem != null)
            {
                trackedDay = daySystem.CurrentDay;
                daySystem.OnDayStateChangedDelegate += OnDayStateChanged;
            }

            if (orderWindow != null)
            {
                orderWindow.InteractionCompleted += OnOrderWindowInteractionCompleted;
            }
        }

        private void OnDestroy()
        {
            if (daySystem != null)
            {
                daySystem.OnDayStateChangedDelegate -= OnDayStateChanged;
            }

            if (orderWindow != null)
            {
                orderWindow.InteractionCompleted -= OnOrderWindowInteractionCompleted;
            }
        }

        private void ResolveReferences()
        {
            var linker = Linker.Instance;
            if (linker == null)
                return;

            if (orderWindow == null)
                orderWindow = linker.OrderWindowInteractable;

            if (daySystem == null)
                daySystem = linker.DaySystem;
        }

        private void OnDayStateChanged(Enums.EDayState state, int currentDay)
        {
            if (currentDay == trackedDay)
                return;

            trackedDay = currentDay;
            closeTriggeredToday = false;
        }

        private void OnOrderWindowInteractionCompleted(GameObject interactor)
        {
            if (closeTriggeredToday)
                return;

            closeTriggeredToday = true;
            CloseDoorIfOpen();
        }

        private void CloseDoorIfOpen()
        {
            if (door == null)
            {
                Debug.LogWarning("PlayerCellDoorController: Door is not assigned.");
                return;
            }

            if (door.IsMoving || !door.IsOpen)
                return;

            door.Interact(gameObject);
        }
    }
}
