using System.Collections;
using Interactable.Workbench;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerWorkbenchModeController : MonoBehaviour
    {
        private enum RuntimeState
        {
            Free = 0,
            Entering = 1,
            Overview = 2,
            LockedView = 3,
            Exiting = 4
        }

        [Header("Links")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraPivot;

        [Header("Input")]
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string workbenchActionMap = "Workbench";
        [SerializeField] private bool useTabKeyForCancel = true;

        [Header("Raycast")]
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask interactMask = ~0;
        [SerializeField] private bool debugRay = false;

        private RuntimeState runtimeState = RuntimeState.Free;
        private WorkbenchInteractableBase activeWorkbench;
        private WorkbenchFocusTarget currentLockedTarget;

        private InputAction lookAction;
        private InputAction interactAction;

        private Vector3 returnPosition;
        private Quaternion returnRotation;
        private float returnPitch;
        private string previousActionMap;

        private float baseYaw;
        private float basePitch;
        private float currentYawOffset;
        private float currentPitchOffset;
        private float overviewCameraLocalPitch;

        private Coroutine activeRoutine;
        private bool cancelWasPressed;
        private bool interactWasPressed;

        public bool IsInWorkbenchMode => runtimeState != RuntimeState.Free;
        public bool IsInOverview => runtimeState == RuntimeState.Overview;
        public WorkbenchInteractableBase ActiveWorkbench => activeWorkbench;

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (cameraPivot == null && playerController != null) cameraPivot = playerController.CameraPivot;
        }

        private void OnEnable()
        {
            ResolveActions();
            SubscribeActions();
        }

        private void OnDisable()
        {
            UnsubscribeActions();
        }

        private void Update()
        {
            if (runtimeState == RuntimeState.Overview)
            {
                UpdateOverviewLook();
                TryHandleOverviewInteract();
                TryHandleCancelToExit();
            }
            else if (runtimeState == RuntimeState.LockedView)
            {
                TryHandleCancelToOverview();
            }
        }

        public bool TryEnterWorkbench(WorkbenchInteractableBase workbench)
        {
            if (workbench == null) return false;
            if (runtimeState != RuntimeState.Free) return false;

            if (playerController == null || playerController.CameraPivot == null)
            {
                Debug.LogError("PlayerWorkbenchModeController: PlayerController or camera pivot is missing.");
                return false;
            }

            activeWorkbench = workbench;
            currentLockedTarget = null;

            returnPosition = transform.position;
            returnRotation = transform.rotation;
            returnPitch = playerController.Pitch;
            previousActionMap = playerInput != null ? playerInput.currentActionMap?.name : string.Empty;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(EnterWorkbenchRoutine(workbench));
            return true;
        }

        public void RequestLockedView(WorkbenchFocusTarget target, Transform viewTransform, float duration)
        {
            if (activeWorkbench == null) return;
            if (runtimeState != RuntimeState.Overview) return;
            if (target == null || viewTransform == null) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(SwitchToLockedViewRoutine(target, viewTransform, duration));
        }

        public void ReturnToOverview(float duration)
        {
            if (activeWorkbench == null) return;
            if (runtimeState != RuntimeState.LockedView) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(ReturnToOverviewRoutine(duration));
        }

        public void ExitWorkbench(float duration)
        {
            if (activeWorkbench == null) return;
            if (runtimeState == RuntimeState.Exiting || runtimeState == RuntimeState.Free) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(ExitWorkbenchRoutine(duration));
        }

        private IEnumerator EnterWorkbenchRoutine(WorkbenchInteractableBase workbench)
        {
            runtimeState = RuntimeState.Entering;
            cancelWasPressed = false;
            interactWasPressed = false;

            if (playerController != null)
            {
                playerController.ResetRuntimeInput();
                playerController.enabled = false;
            }

            if (characterController != null)
                characterController.enabled = false;

            SwitchActionMap(workbenchActionMap);
            ApplyCursorState(false, CursorLockMode.Locked);

            ResetCameraLocalPositionToPivotZero();

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startPitch = playerController != null ? playerController.Pitch : 0f;

            GetViewAngles(workbench.OverviewView, out float targetYaw, out float targetPitch);

            float time = 0f;
            float duration = Mathf.Max(0.01f, workbench.EnterDuration);

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = workbench.EnterCurve.Evaluate(Mathf.Clamp01(time / duration));

                transform.position = Vector3.Lerp(startPos, workbench.PlayerAnchor.position, t);
                transform.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(0f, targetYaw, 0f), t);
                playerController.SetPitch(Mathf.LerpAngle(startPitch, targetPitch, t));

                yield return null;
            }

            transform.position = workbench.PlayerAnchor.position;
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            playerController.SetPitch(targetPitch);

            ResetCameraLocalPositionToPivotZero();

            currentYawOffset = 0f;
            currentPitchOffset = 0f;
            baseYaw = targetYaw;
            basePitch = targetPitch;
            overviewCameraLocalPitch = NormalizeAngle(targetPitch);

            runtimeState = RuntimeState.Overview;
            activeRoutine = null;
            activeWorkbench.OnWorkbenchEntered(this);
        }

        private IEnumerator SwitchToLockedViewRoutine(WorkbenchFocusTarget target, Transform viewTransform, float duration)
        {
            runtimeState = RuntimeState.Entering;
            cancelWasPressed = false;
            interactWasPressed = false;

            CaptureOverviewCameraLocalPitch();

            target.NotifyFocusEntered();
            activeWorkbench.OnLockedViewEnterStarted(target);

            float startYaw = transform.eulerAngles.y;
            float startPitch = playerController.Pitch;
            Vector3 startCamPos = playerCamera != null ? playerCamera.transform.position : Vector3.zero;
            Quaternion startCamRot = playerCamera != null ? playerCamera.transform.rotation : Quaternion.identity;

            GetViewAngles(viewTransform, out float targetYaw, out float targetPitch);

            float time = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (time < safeDuration)
            {
                time += Time.deltaTime;
                float t = activeWorkbench.LockedViewCurve.Evaluate(Mathf.Clamp01(time / safeDuration));

                transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(startYaw, targetYaw, t), 0f);
                playerController.SetPitch(Mathf.LerpAngle(startPitch, targetPitch, t));

                if (playerCamera != null)
                {
                    playerCamera.transform.position = Vector3.Lerp(startCamPos, viewTransform.position, t);
                    playerCamera.transform.rotation = Quaternion.Slerp(startCamRot, viewTransform.rotation, t);
                }

                yield return null;
            }

            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            playerController.SetPitch(targetPitch);

            if (playerCamera != null)
            {
                playerCamera.transform.position = viewTransform.position;
                playerCamera.transform.rotation = viewTransform.rotation;
            }

            currentLockedTarget = target;
            runtimeState = RuntimeState.LockedView;
            activeRoutine = null;
            activeWorkbench.OnLockedViewEntered(target);
        }

        private IEnumerator ReturnToOverviewRoutine(float duration)
        {
            runtimeState = RuntimeState.Entering;
            cancelWasPressed = false;
            interactWasPressed = false;

            WorkbenchFocusTarget leavingTarget = currentLockedTarget;
            if (leavingTarget != null)
                activeWorkbench.OnLockedViewExitStarted(leavingTarget);

            float startYaw = transform.eulerAngles.y;
            float startPitch = playerController.Pitch;

            Vector3 startCamPos = playerCamera != null ? playerCamera.transform.position : Vector3.zero;
            float startCamLocalPitch = playerCamera != null
                ? NormalizeAngle(playerCamera.transform.localEulerAngles.x)
                : 0f;

            Quaternion targetPlayerRot = Quaternion.Euler(0f, baseYaw + currentYawOffset, 0f);

            Vector3 targetCamPos = startCamPos;
            if (playerCamera != null && cameraPivot != null)
                targetCamPos = cameraPivot.position;

            float targetPitch = basePitch + currentPitchOffset;

            float time = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (time < safeDuration)
            {
                time += Time.deltaTime;
                float t = activeWorkbench.LockedViewCurve.Evaluate(Mathf.Clamp01(time / safeDuration));

                transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(startYaw, baseYaw + currentYawOffset, t), 0f);
                playerController.SetPitch(Mathf.LerpAngle(startPitch, targetPitch, t));

                if (playerCamera != null)
                {
                    playerCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);

                    float localPitch = Mathf.LerpAngle(startCamLocalPitch, overviewCameraLocalPitch, t);
                    playerCamera.transform.localRotation = Quaternion.Euler(localPitch, 0f, 0f);
                }

                yield return null;
            }

            transform.rotation = targetPlayerRot;
            playerController.SetPitch(targetPitch);

            ResetCameraLocalPositionToPivotZero();
            SetCameraLocalRotationToOverviewPitch(overviewCameraLocalPitch);

            if (leavingTarget != null)
            {
                leavingTarget.NotifyFocusExited();
                activeWorkbench.OnLockedViewExited(leavingTarget);
            }

            currentLockedTarget = null;
            runtimeState = RuntimeState.Overview;
            activeRoutine = null;
        }

        private IEnumerator ExitWorkbenchRoutine(float duration)
        {
            runtimeState = RuntimeState.Exiting;
            cancelWasPressed = false;
            interactWasPressed = false;

            if (currentLockedTarget != null)
            {
                activeWorkbench.OnLockedViewExitStarted(currentLockedTarget);
                currentLockedTarget.NotifyFocusExited();
                activeWorkbench.OnLockedViewExited(currentLockedTarget);
                currentLockedTarget = null;
            }

            activeWorkbench.OnWorkbenchExitStarted(this);

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startPitch = playerController.Pitch;

            float time = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (time < safeDuration)
            {
                time += Time.deltaTime;
                float t = activeWorkbench.ExitCurve.Evaluate(Mathf.Clamp01(time / safeDuration));

                transform.position = Vector3.Lerp(startPos, returnPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, returnRotation, t);
                playerController.SetPitch(Mathf.LerpAngle(startPitch, returnPitch, t));

                yield return null;
            }

            transform.position = returnPosition;
            transform.rotation = returnRotation;
            playerController.SetPitch(returnPitch);

            ResetCameraLocalPositionToPivotZero();
            SetCameraLocalRotationToOverviewPitch(NormalizeAngle(returnPitch));

            SwitchActionMap(previousActionMap);

            if (characterController != null)
                characterController.enabled = true;

            if (playerController != null)
            {
                playerController.ResetRuntimeInput();
                playerController.enabled = true;
            }

            ApplyCursorState(false, CursorLockMode.Locked);

            WorkbenchInteractableBase finishedWorkbench = activeWorkbench;
            activeWorkbench = null;
            runtimeState = RuntimeState.Free;
            activeRoutine = null;

            finishedWorkbench?.OnWorkbenchExited(this);
        }

        private void UpdateOverviewLook()
        {
            if (activeWorkbench == null || lookAction == null) return;

            Vector2 look = lookAction.ReadValue<Vector2>();
            float sens = activeWorkbench.LookSensitivity;

            currentYawOffset += look.x * sens;
            currentPitchOffset -= look.y * sens;

            currentYawOffset = Mathf.Clamp(currentYawOffset, -activeWorkbench.LeftYawLimit, activeWorkbench.RightYawLimit);
            currentPitchOffset = Mathf.Clamp(currentPitchOffset, -activeWorkbench.DownPitchLimit, activeWorkbench.UpPitchLimit);

            float finalYaw = baseYaw + currentYawOffset;
            float finalPitch = basePitch + currentPitchOffset;

            transform.rotation = Quaternion.Euler(0f, finalYaw, 0f);
            playerController.SetPitch(finalPitch);
        }

        private void TryHandleOverviewInteract()
        {
            if (!ConsumeInteractPressed()) return;
            if (playerCamera == null || activeWorkbench == null) return;

            Vector3 origin = playerCamera.transform.position;
            Vector3 dir = playerCamera.transform.forward;

            if (debugRay)
                Debug.DrawRay(origin, dir * interactDistance, Color.cyan, 1.5f);

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
                return;

            WorkbenchFocusTarget target = hit.collider.GetComponentInParent<WorkbenchFocusTarget>();
            if (target == null) return;
            if (!target.isActiveAndEnabled) return;
            if (!target.IsAvailableFor(activeWorkbench)) return;

            activeWorkbench.HandleFocusInteract(target);
        }

        private void TryHandleCancelToOverview()
        {
            if (!ConsumeCancelPressed()) return;
            activeWorkbench?.HandleCancelFromLockedView();
        }

        private void TryHandleCancelToExit()
        {
            if (!ConsumeCancelPressed()) return;
            activeWorkbench?.HandleCancelFromOverview();
        }

        private bool ConsumeInteractPressed()
        {
            if (!interactWasPressed) return false;
            interactWasPressed = false;
            return true;
        }

        private bool ConsumeCancelPressed()
        {
            if (!cancelWasPressed) return false;
            cancelWasPressed = false;
            return true;
        }

        private void HandleInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!IsInWorkbenchMode) return;
            interactWasPressed = true;
        }

        private void ResolveActions()
        {
            if (playerInput == null || playerInput.actions == null)
                return;

            lookAction = !string.IsNullOrWhiteSpace(lookActionName) ? playerInput.actions[lookActionName] : null;
            interactAction = !string.IsNullOrWhiteSpace(interactActionName) ? playerInput.actions[interactActionName] : null;
        }

        private void SubscribeActions()
        {
            if (interactAction != null)
                interactAction.performed += HandleInteractPerformed;
        }

        private void UnsubscribeActions()
        {
            if (interactAction != null)
                interactAction.performed -= HandleInteractPerformed;
        }

        private void LateUpdate()
        {
            if (!IsInWorkbenchMode) return;
            if (!useTabKeyForCancel) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
                cancelWasPressed = true;
        }

        private void GetViewAngles(Transform viewTransform, out float yaw, out float pitch)
        {
            if (viewTransform == null)
            {
                yaw = transform.eulerAngles.y;
                pitch = playerController != null ? playerController.Pitch : 0f;
                return;
            }

            Vector3 euler = viewTransform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private void SwitchActionMap(string actionMapName)
        {
            if (playerInput == null) return;
            if (string.IsNullOrWhiteSpace(actionMapName)) return;
            if (playerInput.actions == null) return;
            if (playerInput.currentActionMap != null && playerInput.currentActionMap.name == actionMapName) return;
            if (playerInput.actions.FindActionMap(actionMapName, false) == null) return;

            playerInput.SwitchCurrentActionMap(actionMapName);
        }

        private void ResetCameraLocalPositionToPivotZero()
        {
            if (playerCamera == null) return;

            playerCamera.transform.localPosition = Vector3.zero;
        }

        private void CaptureOverviewCameraLocalPitch()
        {
            if (playerCamera == null) return;

            overviewCameraLocalPitch = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }

        private void SetCameraLocalRotationToOverviewPitch(float pitch)
        {
            if (playerCamera == null) return;

            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static void ApplyCursorState(bool visible, CursorLockMode lockMode)
        {
            Cursor.visible = visible;
            Cursor.lockState = lockMode;
        }
    }
}