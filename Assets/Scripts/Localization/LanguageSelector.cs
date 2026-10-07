using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public sealed class LanguageSelector : MonoBehaviour
{
    [Header("Idioma general")]
    [SerializeField] private GameLanguage defaultLanguage = GameLanguage.English;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text valueLabel;

    private void Awake()
    {
        GameLocalization.Initialize(defaultLanguage);
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);
    }
    private void OnEnable()
    {
        GameLocalization.LanguageChanged += Refresh;
        Refresh();
    }
    private void OnDisable() => GameLocalization.LanguageChanged -= Refresh;
    private void OnDestroy()
    {
        previousButton.onClick.RemoveListener(Previous);
        nextButton.onClick.RemoveListener(Next);
    }
    private void Previous() => GameLocalization.Step(-1);
    private void Next() => GameLocalization.Step(1);
    private void Refresh() { if (valueLabel != null) valueLabel.text = GameLocalization.NativeName; }
}
