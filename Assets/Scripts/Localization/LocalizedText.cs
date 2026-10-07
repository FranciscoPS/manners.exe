using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
[DefaultExecutionOrder(-1000)]
public sealed class LocalizedText : MonoBehaviour
{
    [Header("Texto editable por idioma")]
    [SerializeField] private LocalizedString content;
    [SerializeField] private GameLanguage inspectorPreview = GameLanguage.English;
    private TMP_Text label;
    public event Action Applied;
    public LocalizedString Content => content;

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += Apply;
        Apply();
    }
    private void OnDisable() => GameLocalization.LanguageChanged -= Apply;
    private void OnValidate()
    {
        if (!Application.isPlaying) ApplyLanguage(inspectorPreview);
    }
    public void SetContent(LocalizedString value)
    {
        content = value;
        Apply();
    }
    public void Apply() => ApplyLanguage(Application.isPlaying ? GameLocalization.Language : inspectorPreview);
    public void ApplyLanguage(GameLanguage language)
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label == null) return;
        label.text = content.Get(language);
        Applied?.Invoke();
    }
}
