using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public event Action InteractPressed;
    
    [Header("Links")]
    [SerializeField] private Transform cameraPivot;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float gravity = -18f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private float pitch;
    private float verticalVel;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
            Debug.LogWarning("PlayerController: cameraPivot not set.");
    }

    private void OnEnable()
    {
        if (!Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (cameraPivot != null)
        {
            pitch -= lookInput.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(Vector3.up * (lookInput.x * mouseSensitivity));
        }

        bool grounded = controller.isGrounded;
        if (grounded && verticalVel < 0f)
            verticalVel = -2f;

        verticalVel += gravity * Time.deltaTime;

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

        Vector3 velocity = new Vector3(move.x, verticalVel, move.z);
        controller.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            InteractPressed?.Invoke();
        }
    }
}