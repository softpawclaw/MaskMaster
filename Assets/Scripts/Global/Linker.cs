using DB;
using Interactable;
using Interactable.Table;
using Items;
using Player;
using Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Global
{
    public class Linker : MonoBehaviour
    {
        [SerializeField] private PlayerInput configPlayerInput = null;
        [SerializeField] private PlayerController configPlayerController = null;
        [SerializeField] private PlayerHandsController playerHandsController = null;
        
        [SerializeField] private OrderWindowInteractable orderWindowInteractable = null;
        [SerializeField] private MaskCraftTable maskCraftTable = null;
        [SerializeField] private MaskShelfInteractable maskShelfInteractable = null;
        
        [SerializeField] private DBMask dbMask = null;
        [SerializeField] private DBQuest dbQuest = null;
        [SerializeField] private DBClients dbClients = null;
        [SerializeField] private DBNames dbNames = null;
        [SerializeField] private DBFaceCover dbFaceCover = null;
        [SerializeField] private DBMistResistance dbMistResistance = null;
        [SerializeField] private DBDistrict dbDistrict = null;
        [SerializeField] private DBFaction dbFaction = null;
        [SerializeField] private DBMaskCombination dbMaskCombination = null;
        [FormerlySerializedAs("catalogPageDatabase")] [SerializeField] private DBCatalogPage dbCatalogPage = null;
        
        [SerializeField] private ItemsFactory itemsFactory = null;
        
        public PlayerInput PlayerInput { private set; get; } = null;
        public PlayerController PlayerController { private set; get; } = null;
        public PlayerHandsController PlayerHandsController { private set; get; } = null;
        public OrderWindowInteractable OrderWindowInteractable { private set; get; } = null;
        public MaskCraftTable MaskCraftTable { private set; get; } = null;
        public MaskShelfInteractable MaskShelfInteractable { private set; get; } = null;
        
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
        public QuestSystem QuestSystem { private set; get; } = null;
        public DelayedDialogSystem DelayedDialogSystem { private set; get; } = null;
        
        public DBQuest DBQuest { private set; get; } = null;
        public DBMask DBMask { private set; get; } = null;
        public DBClients DBClients { private set; get; } = null;
        public DBNames DBNames { private set; get; } = null;
        public DBFaceCover DBFaceCover { private set; get; } = null;
        public DBMistResistance DBMistResistance { private set; get; } = null;
        public DBDistrict DBDistrict { private set; get; } = null;
        public DBFaction DBFaction { private set; get; } = null;
        public DBMaskCombination DBMaskCombination { private set; get; } = null;
        public DBCatalogPage DBCatalogPage { private set; get; } = null;
        
        public ItemsFactory ItemsFactory { private set; get; } = null;

        public static Linker Instance { private set; get; } = null;
        
        private void Awake()
        {
            PlayerInput = configPlayerInput;
            PlayerController = configPlayerController;
            PlayerHandsController = playerHandsController;
            
            OrderWindowInteractable = orderWindowInteractable;
            MaskCraftTable = maskCraftTable;
            MaskShelfInteractable = maskShelfInteractable;
            
            DBQuest = dbQuest;
            DBMask = dbMask;
            DBClients = dbClients;
            DBNames = dbNames;
            DBFaceCover = dbFaceCover;
            DBMistResistance = dbMistResistance;
            DBDistrict = dbDistrict;
            DBFaction = dbFaction;
            DBMaskCombination = dbMaskCombination;
            DBCatalogPage = dbCatalogPage;
            
            ItemsFactory = itemsFactory;
            
            Campaign = GetComponent<Campaign>();
            DaySystem = GetComponent<DaySystem>();
            DirectorSystem = GetComponent<DirectorSystem>();
            UISystem = GetComponent<UISystem>();
            AnimationSystem = GetComponent<AnimationSystem>();
            InteractionSystem = GetComponent<InteractionSystem>();
            WorkDaySystem = GetComponent<WorkDaySystem>();
            OrdersSystem = GetComponent<OrdersSystem>();
            PlacerSystem = GetComponent<PlacerSystem>();
            InputControlSystem = GetComponent<InputControlSystem>();
            QuestSystem = GetComponent<QuestSystem>();
            DelayedDialogSystem = GetComponent<DelayedDialogSystem>();

            Instance = this;

            OrderWindowInteractable.Link();
            MaskCraftTable.Link();
            MaskShelfInteractable.Link();
            
            Campaign.Link();
            DirectorSystem.Link();
            OrdersSystem.Link();
            WorkDaySystem.Link();
            UISystem.Link();
            InputControlSystem.Link();
            QuestSystem.Link();
            DelayedDialogSystem.Link();
        }
    }
}
