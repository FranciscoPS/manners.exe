using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ConfirmDialog : MonoBehaviour, IUpdateable
{
    [Header("Referencias del prefab")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmLabel;
    [SerializeField] private Button cancelButton;

    private LocalizedString title;
    private LocalizedString message;
    private LocalizedString confirmText;
    private Action confirmed;
    private GameObject returnFocus;
    private static int lastBackFrame = -1;

    public bool IsOpen => gameObject.activeInHierarchy;

    public bool IsActive => isActiveAndEnabled;

    public static bool ConsumedBackThisFrame => lastBackFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lastBackFrame = -1;
    }

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += RefreshTexts;
        UpdateManager.Instance?.Register(this);
        RefreshTexts();
        if (cancelButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= RefreshTexts;
        UpdateManager.Instance?.Unregister(this);
    }

    public void Show(LocalizedString titleValue, LocalizedString messageValue, LocalizedString confirmValue, Action onConfirmed)
    {
        title = titleValue;
        message = messageValue;
        confirmText = confirmValue;
        confirmed = onConfirmed;
        returnFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        gameObject.SetActive(true);
        RefreshTexts();
    }

    public void OnUpdate(float deltaTime)
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (back) Cancel();
    }

    public void Confirm()
    {
        Action action = confirmed;
        Hide();
        action?.Invoke();
    }

    public void Cancel()
    {
        lastBackFrame = Time.frameCount;
        Hide();
        if (returnFocus != null && returnFocus.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(returnFocus);
    }

    private void Hide()
    {
        confirmed = null;
        gameObject.SetActive(false);
    }

    private void RefreshTexts()
    {
        if (titleText != null) titleText.text = title.Value;
        if (messageText != null)
        {
            string body = message.Value;
            messageText.text = body;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(body));
        }
        if (confirmLabel != null) confirmLabel.text = confirmText.Value;
    }
}
