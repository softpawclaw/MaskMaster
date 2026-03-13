using UnityEngine;

public class PlayerRayInteractor : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float interactDistance = 2.2f;
    [SerializeField] private LayerMask interactMask = ~0; // по умолчанию все слои

    [Header("Debug")]
    [SerializeField] private bool debugRay = true;
    [SerializeField] private bool debugHitPoint = true;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (player == null) player = GetComponent<PlayerController>();

        if (cam == null) cam = Camera.main;

        if (player == null) Debug.LogError("PlayerRayInteractor: PlayerController not found.");
        if (cam == null) Debug.LogError("PlayerRayInteractor: Camera not found (assign explicitly).");
    }

    private void OnEnable()
    {
        if (player != null)
            player.InteractPressed += TryInteract;
    }

    private void OnDisable()
    {
        if (player != null)
            player.InteractPressed -= TryInteract;
    }

    private void TryInteract()
    {
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (debugRay)
            Debug.DrawRay(origin, dir * interactDistance, Color.yellow, 5f);

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
            return;

        if (debugHitPoint)
            Debug.DrawLine(origin, hit.point, Color.green, 0.25f);

        // Важно: берём Interactable у коллайдера или у родителей
        var interactable = hit.collider.GetComponentInParent<Interactable.Interactable>();
        if (interactable == null) return;

        interactable.Interact(player.gameObject);
    }
}