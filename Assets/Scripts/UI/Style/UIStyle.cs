using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIPlateRole
{
    Primary,
    Secondary,
    Neutral,
    Danger,
    Highlight
}

public enum UITextRole
{
    Title,
    Heading,
    Label,
    Body,
    BodyDark,
    Number
}

[CreateAssetMenu(fileName = "UIStyle", menuName = "Game/Estilo de la interfaz")]
public sealed class UIStyle : ScriptableObject
{
    public const string ResourceName = "UIStyle";
    public const string PlateShaderName = "UI/Manners Plate";

    public static readonly int TimeId = Shader.PropertyToID("_UIStyleTime");

    private static UIStyle instance;

    public static UIStyle Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<UIStyle>(ResourceName);
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("Paleta")]
    [Tooltip("Tinta: contorno grueso de todas las placas y textos, y color de las sombras duras.")]
    public Color ink = new Color32(0x15, 0x0A, 0x2B, 0xFF);
    [Tooltip("Relleno de los paneles oscuros (contenedores).")]
    public Color panel = new Color32(0x25, 0x16, 0x50, 0xFF);
    [Tooltip("Segundo tono de panel: filas, casillas y trama de puntos de los paneles.")]
    public Color panelAlt = new Color32(0x3A, 0x27, 0x78, 0xFF);
    [Tooltip("Acento principal (rosa): acciones principales, selección y títulos.")]
    public Color primary = new Color32(0xFF, 0x2E, 0x88, 0xFF);
    [Tooltip("Acento secundario (violeta): navegación y cabeceras.")]
    public Color secondary = new Color32(0x7B, 0x45, 0xF5, 0xFF);
    [Tooltip("Cian: experiencia, información y detalles fríos.")]
    public Color cyan = new Color32(0x23, 0xDD, 0xF0, 0xFF);
    [Tooltip("Amarillo: monedas, números clave y estado resaltado (hover / seleccionado).")]
    public Color yellow = new Color32(0xFF, 0xD7, 0x2E, 0xFF);
    [Tooltip("Peligro (rojo anaranjado): salir, daño, tiempo agotado.")]
    public Color danger = new Color32(0xFF, 0x4A, 0x2F, 0xFF);
    [Tooltip("Positivo (verde lima): vida y valores que mejoran.")]
    public Color good = new Color32(0x7C, 0xF2, 0x5A, 0xFF);
    [Tooltip("Papel: texto claro, línea exterior de los paneles y paneles de diálogo.")]
    public Color paper = new Color32(0xFF, 0xF5, 0xE1, 0xFF);
    [Tooltip("Texto secundario (lavanda) para descripciones y etiquetas pequeñas.")]
    public Color textDim = new Color32(0xCF, 0xC4, 0xF2, 0xFF);
    [Tooltip("Placa neutra: botones de volver / cancelar.")]
    public Color neutral = new Color32(0x4B, 0x3A, 0x96, 0xFF);
    [Tooltip("Velo que oscurece el juego detrás de los menús.")]
    public Color scrim = new Color(0.07f, 0.03f, 0.15f, 0.8f);
    [Tooltip("Tinte de los botones desactivados.")]
    public Color disabled = new Color(0.42f, 0.4f, 0.52f, 0.75f);

    [Header("Forma")]
    [Tooltip("Inclinación de las placas: desplazamiento horizontal por cada unidad de altura. 0.22 ≈ 12°. La cursiva de la fuente usa el mismo ángulo.")]
    [Range(0f, 0.5f)] public float skew = 0.22f;
    [Tooltip("Inclinación reducida para elementos altos (barras anchas, tarjetas).")]
    [Range(0f, 0.5f)] public float skewSoft = 0.08f;
    [Tooltip("Desplazamiento de la sombra dura de botones y placas, en unidades de canvas (1080p).")]
    public Vector2 shadowOffset = new Vector2(5f, -5f);
    [Tooltip("Desplazamiento de la sombra dura de los paneles grandes.")]
    public Vector2 panelShadowOffset = new Vector2(9f, -9f);

    [Header("Sprites (generados con Tools > Manners > UI)")]
    public Sprite plate;
    public Sprite plateChamfer;
    public Sprite panelDark;
    public Sprite panelSmall;
    public Sprite panelPaper;
    public Sprite fill;
    public Sprite frame;
    public Sprite burst;
    public Sprite bolt;
    public Sprite spark;
    public Sprite ring;
    public Sprite arrowDown;
    public Sprite chevron;
    public Sprite iconClock;
    public Sprite iconCoin;
    public Sprite iconSpeaker;

    [Header("Materiales de placa")]
    [Tooltip("Franjas diagonales animadas, sutiles: botones y placas.")]
    public Material plateStripes;
    [Tooltip("Franjas diagonales animadas, marcadas: cintas de título y barras.")]
    public Material plateStripesBold;
    [Tooltip("Franjas claras sobre fondo oscuro: velo de los menús.")]
    public Material scrimStripes;

    [Header("Tipografía")]
    public TMP_FontAsset font;
    [Tooltip("Contorno grueso de tinta + sombra dura: títulos, botones y números.")]
    public Material textInk;
    [Tooltip("Contorno fino de tinta: texto corrido sobre paneles oscuros.")]
    public Material textInkThin;
    [Tooltip("Sin contorno: texto de tinta sobre paneles de papel.")]
    public Material textPlain;

    [Header("Movimiento")]
    [Tooltip("Escala máxima del golpe (punch) cuando cambia un valor.")]
    public float punchScale = 1.16f;
    [Tooltip("Giro en grados del golpe.")]
    public float punchRotation = 4f;
    public float punchDuration = 0.32f;
    [Tooltip("Escala del botón bajo el cursor.")]
    public float hoverScale = 1.07f;
    [Tooltip("Giro en grados del botón bajo el cursor.")]
    public float hoverRotation = -1.5f;
    public float hoverDuration = 0.14f;
    [Tooltip("Cuánto crece la sombra dura bajo el cursor (multiplicador).")]
    public float hoverShadow = 1.8f;
    [Tooltip("Escala del botón al pulsarlo.")]
    public float pressScale = 0.95f;
    [Tooltip("Duración de la entrada de cada bloque de una pantalla.")]
    public float introDuration = 0.34f;
    [Tooltip("Retraso entre bloques consecutivos al entrar una pantalla.")]
    public float introStagger = 0.045f;
    [Tooltip("Distancia desde la que entran deslizándose los bloques, en unidades de canvas.")]
    public float introSlide = 70f;
    [Tooltip("Velocidad del ciclo de color de acento (vueltas por segundo).")]
    public float accentCycleSpeed = 0.35f;

    public Color PlateColor(UIPlateRole role)
    {
        switch (role)
        {
            case UIPlateRole.Primary: return primary;
            case UIPlateRole.Secondary: return secondary;
            case UIPlateRole.Danger: return danger;
            case UIPlateRole.Highlight: return yellow;
            default: return neutral;
        }
    }

    public ColorBlock ButtonColors(UIPlateRole role)
    {
        Color pressed = Color.Lerp(yellow, ink, 0.22f);
        pressed.a = 1f;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = PlateColor(role);
        colors.highlightedColor = yellow;
        colors.selectedColor = yellow;
        colors.pressedColor = pressed;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    public Color AccentCycle(float t)
    {
        t = Mathf.Repeat(t, 1f) * 3f;
        if (t < 1f) return Color.Lerp(yellow, primary, Smooth(t));
        if (t < 2f) return Color.Lerp(primary, cyan, Smooth(t - 1f));
        return Color.Lerp(cyan, yellow, Smooth(t - 2f));
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public Color TextColor(UITextRole role)
    {
        switch (role)
        {
            case UITextRole.Heading: return yellow;
            case UITextRole.BodyDark: return ink;
            case UITextRole.Number: return yellow;
            default: return paper;
        }
    }

    public Material TextMaterial(UITextRole role)
    {
        switch (role)
        {
            case UITextRole.Body: return textInkThin;
            case UITextRole.BodyDark: return textPlain;
            default: return textInk;
        }
    }

    public static FontStyles TextStyle(UITextRole role)
    {
        switch (role)
        {
            case UITextRole.Title:
            case UITextRole.Heading:
            case UITextRole.Label:
                return FontStyles.Italic | FontStyles.UpperCase;
            case UITextRole.Number:
                return FontStyles.Italic;
            default:
                return FontStyles.Normal;
        }
    }

    public void ApplyText(TMP_Text text, UITextRole role)
    {
        if (text == null) return;

        ApplyFont(text, role);
        text.color = TextColor(role);
        text.fontStyle = TextStyle(role);
    }

    public void ApplyFont(TMP_Text text, UITextRole role)
    {
        if (text == null || font == null) return;

        text.font = font;
        Material material = TextMaterial(role);
        if (material != null)
            text.fontSharedMaterial = material;
    }

    public void ApplyPlate(Image image, Color color, float skewAmount, bool shadow = true, Material material = null)
    {
        ApplySprite(image, plate, color, skewAmount, shadow ? shadowOffset : Vector2.zero, material);
    }

    public void ApplyPanel(Image image, bool large = true)
    {
        ApplySprite(image, large ? panelDark : panelSmall, Color.white, 0f, large ? panelShadowOffset : shadowOffset, null);
    }

    public void ApplySprite(Image image, Sprite sprite, Color color, float skewAmount, Vector2 shadow, Material material)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
        image.material = material;

        UISkew skewEffect = image.GetComponent<UISkew>();
        if (Mathf.Abs(skewAmount) > 0.0001f)
        {
            if (skewEffect == null) skewEffect = image.gameObject.AddComponent<UISkew>();
            skewEffect.Amount = skewAmount;
        }
        else if (skewEffect != null)
        {
            skewEffect.Amount = 0f;
        }

        Shadow shadowEffect = image.GetComponent<Shadow>();
        if (shadow.sqrMagnitude > 0.0001f)
        {
            if (shadowEffect == null) shadowEffect = image.gameObject.AddComponent<Shadow>();
            shadowEffect.effectColor = ink;
            shadowEffect.effectDistance = shadow;
            shadowEffect.useGraphicAlpha = true;
            shadowEffect.enabled = true;
        }
        else if (shadowEffect != null)
        {
            shadowEffect.enabled = false;
        }
    }

    public void ApplyButton(Button button, UIPlateRole role)
    {
        if (button == null) return;

        Image image = button.targetGraphic as Image;
        if (image != null)
            ApplyPlate(image, Color.white, skew, true, plateStripes);

        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors(role);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            ApplyText(label, UITextRole.Label);
    }

    public Image CreatePlate(string objectName, Transform parent, Color color, float skewAmount, bool shadow = true, Material material = null)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        ApplyPlate(image, color, skewAmount, shadow, material);
        return image;
    }
}
