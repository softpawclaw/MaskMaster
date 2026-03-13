using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace UI
{
    public class ControllerUI : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PlayerInput playerInput;

        [Header("Action Maps")]
        [SerializeField] private string gameplayMapName = "Player";
        [SerializeField] private string uiMapName = "UI";
        [SerializeField] private string submitActionName = "Submit";

        [Header("Cursor")]
        [SerializeField] private bool manageCursor = true;
        [SerializeField] private bool showCursorWhileOpen = true;

        [Header("Fader")]
        [SerializeField] private float faderContinueDelay = 2.0f;

        private VisualElement root;

        private VisualElement dialogRoot;
        private Label dialogText;
        private Button continueButton;

        private VisualElement faderRoot;
        private Label faderText;
        private Button faderContinue;

        private Action onClosed;
        private Coroutine routine;

        private string previousMap;
        private InputAction submitAction;

        private bool canClose;
        public bool IsOpen { get; private set; }

        private enum ScreenMode
        {
            None,
            Dialog,
            Fader
        }

        private ScreenMode currentMode = ScreenMode.None;

        private void Awake()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (playerInput == null) playerInput = FindFirstObjectByType<PlayerInput>();

            root = uiDocument.rootVisualElement;

            dialogRoot = root.Q<VisualElement>("DialogRoot");
            dialogText = root.Q<Label>("DialogText");
            continueButton = root.Q<Button>("ContinueButton");

            faderRoot = root.Q<VisualElement>("FaderRoot");
            faderText = root.Q<Label>("FaderText");
            faderContinue = root.Q<Button>("FaderContinue");

            if (dialogRoot == null || dialogText == null || continueButton == null)
                Debug.LogError("ControllerUI: Dialog elements not found. Check UXML names: DialogRoot/DialogText/ContinueButton.");

            if (faderRoot == null || faderText == null || faderContinue == null)
                Debug.LogError("ControllerUI: Fader elements not found. Check UXML names: FaderRoot/FaderText/FaderContinue.");

            if (continueButton != null)
                continueButton.clicked += HandleContinue;

            if (faderContinue != null)
                faderContinue.clicked += HandleContinue;

            HideImmediate();
        }

        private void OnDisable()
        {
            StopCurrentRoutine();
            UnsubscribeSubmit();
            ForceHideAllVisuals();
            IsOpen = false;
            canClose = false;
            currentMode = ScreenMode.None;
        }

        public bool TryShowDialog(string text, float continueDelaySeconds, Action closed)
        {
            if (IsOpen) return false;
            ShowDialog(text, continueDelaySeconds, closed);
            return true;
        }

        public void ShowDialog(string text, float continueDelaySeconds, Action closed)
        {
            OpenScreen(closed, ScreenMode.Dialog);

            dialogText.text = text;
            dialogRoot.style.display = DisplayStyle.Flex;

            continueButton.style.display = DisplayStyle.None;
            continueButton.SetEnabled(false);

            canClose = false;

            StopCurrentRoutine();
            routine = StartCoroutine(EnableDialogContinueAfter(continueDelaySeconds));
        }

        public bool TryShowFader(Action closed)
        {
            if (IsOpen) return false;
            ShowFader(closed);
            return true;
        }

        public bool TryShowFader(string text, Action closed)
        {
            if (IsOpen) return false;
            ShowFader(text, closed);
            return true;
        }

        public void ShowFader(Action closed)
        {
            OpenScreen(closed, ScreenMode.Fader);

            faderText.text = string.Empty;
            faderText.style.display = DisplayStyle.None;

            faderRoot.style.display = DisplayStyle.Flex;
            faderContinue.style.display = DisplayStyle.None;
            faderContinue.SetEnabled(false);

            canClose = false;

            StopCurrentRoutine();
            routine = StartCoroutine(EnableFaderContinueAfter(faderContinueDelay));
        }

        public void ShowFader(string text, Action closed)
        {
            OpenScreen(closed, ScreenMode.Fader);

            faderText.text = text;
            faderText.style.display = DisplayStyle.Flex;

            faderRoot.style.display = DisplayStyle.Flex;
            faderContinue.style.display = DisplayStyle.None;
            faderContinue.SetEnabled(false);

            canClose = false;

            StopCurrentRoutine();
            routine = StartCoroutine(EnableFaderContinueAfter(faderContinueDelay));
        }

        private void OpenScreen(Action closed, ScreenMode mode)
        {
            HideImmediate();

            IsOpen = true;
            canClose = false;
            onClosed = closed;
            currentMode = mode;

            EnterUIMode();
            ForceHideAllVisuals();
        }

        private IEnumerator EnableDialogContinueAfter(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            canClose = true;

            if (continueButton != null)
            {
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.SetEnabled(true);
                continueButton.Focus();
            }

            routine = null;
        }

        private IEnumerator EnableFaderContinueAfter(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            canClose = true;

            if (faderContinue != null)
            {
                faderContinue.style.display = DisplayStyle.Flex;
                faderContinue.SetEnabled(true);
                faderContinue.Focus();
            }

            routine = null;
        }

        private void HandleContinue()
        {
            if (!IsOpen) return;
            if (!canClose) return;

            var cb = onClosed;
            HideImmediate();
            cb?.Invoke();
        }

        public void HideImmediate()
        {
            StopCurrentRoutine();
            ForceHideAllVisuals();

            if (IsOpen)
                ExitUIMode();

            onClosed = null;
            IsOpen = false;
            canClose = false;
            currentMode = ScreenMode.None;
        }

        private void ForceHideAllVisuals()
        {
            if (dialogRoot != null)
                dialogRoot.style.display = DisplayStyle.None;

            if (continueButton != null)
            {
                continueButton.style.display = DisplayStyle.None;
                continueButton.SetEnabled(false);
            }

            if (faderRoot != null)
                faderRoot.style.display = DisplayStyle.None;

            if (faderText != null)
            {
                faderText.text = string.Empty;
                faderText.style.display = DisplayStyle.None;
            }

            if (faderContinue != null)
            {
                faderContinue.style.display = DisplayStyle.None;
                faderContinue.SetEnabled(false);
            }
        }

        private void StopCurrentRoutine()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private void EnterUIMode()
        {
            if (playerInput != null)
            {
                previousMap = playerInput.currentActionMap != null
                    ? playerInput.currentActionMap.name
                    : gameplayMapName;

                playerInput.SwitchCurrentActionMap(uiMapName);
                SubscribeSubmit();
            }

            if (manageCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = showCursorWhileOpen;
            }
        }

        private void ExitUIMode()
        {
            UnsubscribeSubmit();

            if (playerInput != null)
            {
                var targetMap = string.IsNullOrEmpty(previousMap) ? gameplayMapName : previousMap;
                playerInput.SwitchCurrentActionMap(targetMap);
                previousMap = null;
            }

            if (manageCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SubscribeSubmit()
        {
            UnsubscribeSubmit();

            if (playerInput == null || playerInput.currentActionMap == null)
                return;

            submitAction = playerInput.currentActionMap.FindAction(submitActionName, throwIfNotFound: false);
            if (submitAction == null)
            {
                Debug.LogWarning(
                    $"ControllerUI: Submit action '{submitActionName}' not found in current map '{playerInput.currentActionMap.name}'.");
                return;
            }

            submitAction.performed += OnSubmitPerformed;
        }

        private void UnsubscribeSubmit()
        {
            if (submitAction != null)
            {
                submitAction.performed -= OnSubmitPerformed;
                submitAction = null;
            }
        }

        private void OnSubmitPerformed(InputAction.CallbackContext ctx)
        {
            if (!IsOpen) return;
            if (!canClose) return;

            HandleContinue();
        }
    }
}