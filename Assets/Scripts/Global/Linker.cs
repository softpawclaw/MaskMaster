using Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Global
{
    public class Linker : MonoBehaviour
    {
        [SerializeField] private PlayerInput configPlayerInput = null;
        [SerializeField] private PlayerController configPlayerController = null;
        
        public PlayerInput PlayerInput { private set; get; } = null;
        public PlayerController PlayerController { private set; get; } = null;
        public Campaign Campaign { private set; get; } = null;
        public DaySystem DaySystem { private set; get; } = null;
        public DirectorSystem DirectorSystem { private set; get; } = null;
        public UISystem UISystem { private set; get; } = null;
        public AnimationSystem AnimationSystem { private set; get; } = null;
        public InteractionSystem InteractionSystem { private set; get; } = null;
        public WorkDaySystem WorkDaySystem { private set; get; } = null;
        public OrdersSystem OrdersSystem { private set; get; } = null;
        public PlacerSystem PlacerSystem { private set; get; } = null;
        public InputControlSystem InputControlSystem { private set; get; } = null;

        public static Linker Instance { private set; get; } = null;
        
        private void Awake()
        {
            PlayerInput = configPlayerInput;
            PlayerController =  configPlayerController;
            
            Campaign = GetComponent<Campaign>();
            DaySystem =  GetComponent<DaySystem>();
            DirectorSystem = GetComponent<DirectorSystem>();
            UISystem = GetComponent<UISystem>();
            AnimationSystem = GetComponent<AnimationSystem>();
            InteractionSystem = GetComponent<InteractionSystem>();
            WorkDaySystem = GetComponent<WorkDaySystem>();
            OrdersSystem = GetComponent<OrdersSystem>();
            PlacerSystem = GetComponent<PlacerSystem>();
            InputControlSystem = GetComponent<InputControlSystem>();

            Instance = this;
            
            Campaign.Link();
            DirectorSystem.Link();
            OrdersSystem.Link();
            WorkDaySystem.Link();
            UISystem.Link();
            InputControlSystem.Link();
        }
    }
}