using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class WarningMarquee : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Texto TMP que se desplaza. La fuente, el tamaño, el color, el espaciado y el material (brillo) se editan directamente en ese componente.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("Mensaje que se repite. La fuente pixel solo tiene ASCII sin acentos (A-Z, 0-9 y signos básicos).")]
    [FormerlySerializedAs("message"), SerializeField] private string messageEnglish = "WARNING";
    [SerializeField] private string messageSpanish = "ADVERTENCIA";
    [Tooltip("Separador entre repeticiones. La fuente pixel no tiene •, ▶ ni ⚠: usa / - > | _ : o espacios.")]
    [SerializeField] private string separator = "  //  ";
    [Tooltip("Cuántas veces se repite el mensaje dentro del texto. Tiene que cubrir el ancho de la franja más una repetición: súbelo si ves un hueco en el borde.")]
    [SerializeField, Min(2)] private int repetitions = 10;

    [Header("Movimiento")]
    [Tooltip("Velocidad en unidades del canvas por segundo (con escala 0.01, 100 = 1 metro por segundo). Positivo = avanza hacia la izquierda; negativo = hacia la derecha. El texto está anclado a la pared: la franja solo lo revela alrededor del jugador, así que moverse no cambia su dirección ni su velocidad.")]
    [SerializeField] private float scrollSpeed = 90f;

    private RectTransform labelRect;
    private float originX;
    private float period;
    private float offset;
    private bool measured;

    private void OnEnable() { GameLocalization.LanguageChanged += ApplyMessage; ApplyMessage(); }
    private void OnDisable() => GameLocalization.LanguageChanged -= ApplyMessage;

    private void Awake()
    {
        if (label == null) return;

        labelRect = label.rectTransform;
        originX = labelRect.anchoredPosition.x;

        string composed = ComposeMessage();
        if (label.text != composed)
            label.text = composed;
    }

    public void ApplyMessage()
    {
        if (label == null) return;

        label.text = ComposeMessage();
        measured = false;
    }

    public void Scroll(float deltaTime, float wallAnchor)
    {
        if (labelRect == null) return;
        if (!measured) Measure();
        if (period <= 0f) return;

        offset = Mathf.Repeat(offset + scrollSpeed * deltaTime, period);
        Vector2 position = labelRect.anchoredPosition;
        position.x = originX - Mathf.Repeat(offset + wallAnchor, period);
        labelRect.anchoredPosition = position;
    }

    private void Measure()
    {
        measured = true;
        label.ForceMeshUpdate();

        TMP_TextInfo info = label.textInfo;
        int perRepetition = info.characterCount / Mathf.Max(1, repetitions);
        period = perRepetition > 0 && perRepetition < info.characterCount
            ? info.characterInfo[perRepetition].origin - info.characterInfo[0].origin
            : label.preferredWidth / Mathf.Max(1, repetitions);

        labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, label.preferredWidth);
    }

    private string ComposeMessage()
    {
        string unit = (GameLocalization.Language == GameLanguage.Spanish ? messageSpanish : messageEnglish) + separator;
        System.Text.StringBuilder builder = new System.Text.StringBuilder(unit.Length * repetitions);
        for (int i = 0; i < repetitions; i++)
            builder.Append(unit);

        return builder.ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyMessageInEditor;
        UnityEditor.EditorApplication.delayCall += ApplyMessageInEditor;
    }

    private void ApplyMessageInEditor()
    {
        if (this == null || label == null || label.text == ComposeMessage()) return;

        ApplyMessage();
        UnityEditor.EditorUtility.SetDirty(label);

        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(label))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(label);
    }
#endif
}
