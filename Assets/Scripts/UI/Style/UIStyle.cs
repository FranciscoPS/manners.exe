using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIPlateRole
{
    Primary,
    Secondary,
    Neutral,
    Danger,
    Highlight,
    Anomaly
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
    public const int IdentityVersion = 4;

    public static readonly int TimeId = Shader.PropertyToID("_UIStyleTime");

    private const string ColorTag = "<color=#";
    private static readonly System.Text.StringBuilder HighlightBuilder = new System.Text.StringBuilder(512);

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

    [HideInInspector] public int version;

    [Header("Paleta (documento de arte: portada + robot)")]
    [Tooltip("Tinta: contorno de todas las placas y del texto. Azul casi negro de las extremidades del robot (#060A2B).")]
    public Color ink = new Color32(0x06, 0x0A, 0x2B, 0xFF);
    [Tooltip("Pantalla: relleno de los paneles. Negro azulado, como la cara del robot.")]
    public Color panel = new Color32(0x12, 0x15, 0x2E, 0xFF);
    [Tooltip("Segundo tono de pantalla: filas y casillas dentro de un panel.")]
    public Color panelAlt = new Color32(0x22, 0x2B, 0x52, 0xFF);
    [Tooltip("Energía (naranja de portada, #C0552B): acción principal.")]
    public Color primary = new Color32(0xC0, 0x55, 0x2B, 0xFF);
    [Tooltip("Estructura (índigo de portada, #37446E): navegación, marcos de pantalla y placas en calma.")]
    public Color secondary = new Color32(0x37, 0x44, 0x6E, 0xFF);
    [Tooltip("Expresión (cian de la cara del robot, #19A8E6): información, experiencia y foco del cursor.")]
    public Color cyan = new Color32(0x19, 0xA8, 0xE6, 0xFF);
    [Tooltip("Oro pálido (#EBEAA2): monedas y números clave.")]
    public Color yellow = new Color32(0xEB, 0xEA, 0xA2, 0xFF);
    [Tooltip("Peligro (rojo de portada, #B52B1E): salir, daño, tiempo agotado y avisos que deben llamar la atención.")]
    public Color danger = new Color32(0xB5, 0x2B, 0x1E, 0xFF);
    [Tooltip("Rojo vivo para texto y luces de peligro sobre pantalla oscura (el rojo de portada aclarado para que se lea).")]
    public Color alert = new Color32(0xFF, 0x5A, 0x45, 0xFF);
    [Tooltip("Anomalía (púrpura de portada, #6F3778): sobrecargas y mejoras especiales.")]
    public Color anomaly = new Color32(0x6F, 0x37, 0x78, 0xFF);
    [Tooltip("Vida (verde): la barra de salud cuando está alta.")]
    public Color good = new Color32(0x6F, 0xD6, 0xA0, 0xFF);
    [Tooltip("Luz (#F8FAFD): texto principal.")]
    public Color paper = new Color32(0xF8, 0xFA, 0xFD, 0xFF);
    [Tooltip("Luz de borde (#A1B8BF): texto secundario y filo luminoso de los paneles.")]
    public Color textDim = new Color32(0xA1, 0xB8, 0xBF, 0xFF);
    [Tooltip("Placa neutra: volver / cancelar.")]
    public Color neutral = new Color32(0x2B, 0x36, 0x54, 0xFF);
    [Tooltip("Panel claro de C.H.A.R.L.I.E. (el color de su cara) para sus diálogos y las casillas de iconos.")]
    public Color cream = new Color32(0xEF, 0xEA, 0xDD, 0xFF);
    [Tooltip("Velo que oscurece el juego detrás de los menús.")]
    public Color scrim = new Color(0.024f, 0.039f, 0.17f, 0.84f);

    [Header("Resaltados sobre el papel de los diálogos")]
    [Tooltip("Contraste mínimo de una palabra resaltada sobre el papel claro. Los colores del texto que no llegan se cambian por el de su familia (rojos = Danger, morados = Anomaly, el resto abajo).")]
    [Range(1f, 7f)] public float paperContrast = 3.5f;
    [Tooltip("Sustituto de amarillos y dorados.")]
    public Color paperGold = new Color32(0xA8, 0x62, 0x00, 0xFF);
    [Tooltip("Sustituto de verdes.")]
    public Color paperGreen = new Color32(0x1F, 0x7A, 0x3A, 0xFF);
    [Tooltip("Sustituto de cian y turquesa.")]
    public Color paperCyan = new Color32(0x0B, 0x74, 0x99, 0xFF);
    [Tooltip("Sustituto de azules.")]
    public Color paperBlue = new Color32(0x1F, 0x5F, 0xBF, 0xFF);
    [Tooltip("Tinte de los botones desactivados.")]
    public Color disabled = new Color(0.36f, 0.4f, 0.5f, 0.7f);
    [Tooltip("Tinte de la sombra de las casillas: se multiplica por el color de la casilla para dar su tono oscuro.")]
    public Color lipTint = new Color(0.42f, 0.4f, 0.5f, 1f);

    [Header("Forma (círculo = amable, triángulo = peligro)")]
    [Tooltip("Inclinación de botones y placas (paralelogramo): desplazamiento horizontal por cada unidad de altura. 0.22 ≈ 12°.")]
    [Range(0f, 0.5f)] public float skew = 0.22f;
    [Tooltip("Inclinación reducida para placas muy altas o muy anchas.")]
    [Range(0f, 0.5f)] public float skewSoft = 0.08f;
    [Tooltip("Sombra dura de botones y placas: desplazamiento en unidades de canvas (1080p).")]
    public Vector2 shadowOffset = new Vector2(5f, -5f);
    [Tooltip("Sombra dura de los paneles grandes.")]
    public Vector2 panelShadowOffset = new Vector2(8f, -8f);
    [Tooltip("Grosor del contorno oscuro detrás de los personajes (C.H.A.R.L.I.E.). 0 = sin contorno.")]
    public float silhouetteOutline = 4f;
    [Tooltip("Desplazamiento de la sombra recortada que asoma detrás de los personajes.")]
    public Vector2 silhouetteOffset = new Vector2(12f, -9f);

    [Header("Sprites (generados con Tools > Manners > UI)")]
    public Sprite plate;
    public Sprite plateChamfer;
    public Sprite capsule;
    public Sprite panelDark;
    public Sprite panelSmall;
    public Sprite panelPaper;
    public Sprite fill;
    public Sprite frame;
    public Sprite screenFrame;
    public Sprite burst;
    public Sprite ring;
    public Sprite triangle;
    public Sprite cursor;
    public Sprite arrowDown;
    public Sprite chevron;
    public Sprite iconClock;
    public Sprite iconCoin;
    public Sprite iconSpeaker;

    [Header("Cara de la I.A.")]
    public Sprite faceScreen;
    public Sprite faceCalm;
    public Sprite faceHappy;
    public Sprite faceHurt;
    public Sprite faceAngry;
    public Sprite faceDead;
    [Tooltip("Segundos mínimos entre un gesto de la cara y el siguiente (parpadeo o mirada de reojo).")]
    public float faceBlinkMin = 0.5f;
    [Tooltip("Segundos máximos entre gestos de la cara.")]
    public float faceBlinkMax = 1.3f;
    [Tooltip("Probabilidad de que el gesto sea una mirada de reojo en vez de un parpadeo.")]
    [Range(0f, 1f)] public float faceGlanceChance = 0.3f;

    [Header("Materiales de placa")]
    [Tooltip("Franjas diagonales sutiles: botones y placas.")]
    public Material plateStripes;
    [Tooltip("Franjas diagonales marcadas: relleno de las barras y de las tarjetas al mantener pulsado.")]
    public Material plateStripesBold;
    [Tooltip("Franjas diagonales de precaución: solo en avisos de peligro.")]
    public Material plateHazard;
    [Tooltip("Líneas de barrido de las pantallas (paneles).")]
    public Material screenScan;
    [Tooltip("Líneas de barrido del velo de los menús.")]
    public Material scrimStripes;

    [Header("Tipografía")]
    public TMP_FontAsset font;
    [Tooltip("Títulos, botones y números: contorno fino de tinta y sombra dura corta.")]
    public Material textInk;
    [Tooltip("Texto corrido sobre pantallas: sin sombra.")]
    public Material textInkThin;
    [Tooltip("Texto de tinta sobre paneles claros.")]
    public Material textPlain;
    [Tooltip("Separación entre letras de títulos y botones.")]
    public float labelSpacing = 4f;

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
    public float accentCycleSpeed = 0.3f;
    [Tooltip("Cuánto se agitan los recortes de fondo de títulos y diálogos, en unidades de canvas. 0 = quietos.")]
    public float shardWobble = 4f;
    [Tooltip("Vaivenes por segundo de los recortes de fondo.")]
    public float shardSpeed = 0.35f;
    [Tooltip("Fotogramas por segundo del movimiento de los recortes: bajo se ve a saltos, como animación dibujada. 0 = fluido.")]
    public float shardFrameRate = 12f;
    [Tooltip("Duración de la entrada de los recortes al aparecer su pantalla.")]
    public float shardEnterDuration = 0.28f;

    public Color PlateColor(UIPlateRole role)
    {
        switch (role)
        {
            case UIPlateRole.Primary: return primary;
            case UIPlateRole.Secondary: return secondary;
            case UIPlateRole.Danger: return danger;
            case UIPlateRole.Highlight: return cyan;
            case UIPlateRole.Anomaly: return anomaly;
            default: return neutral;
        }
    }

    public ColorBlock ButtonColors(UIPlateRole role)
    {
        Color pressed = Color.Lerp(cyan, ink, 0.22f);
        pressed.a = 1f;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = PlateColor(role);
        colors.highlightedColor = cyan;
        colors.selectedColor = cyan;
        colors.pressedColor = pressed;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    public Color AccentCycle(float t)
    {
        t = Mathf.Repeat(t, 1f) * 3f;
        if (t < 1f) return Color.Lerp(primary, danger, Smooth(t));
        if (t < 2f) return Color.Lerp(danger, anomaly, Smooth(t - 1f));
        return Color.Lerp(anomaly, primary, Smooth(t - 2f));
    }

    public Color TextAccentCycle(float t)
    {
        t = Mathf.Repeat(t, 1f) * 3f;
        if (t < 1f) return Color.Lerp(paper, yellow, Smooth(t));
        if (t < 2f) return Color.Lerp(yellow, cyan, Smooth(t - 1f));
        return Color.Lerp(cyan, paper, Smooth(t - 2f));
    }

    public Color HealthColor(float ratio)
    {
        if (ratio > 0.5f) return good;
        if (ratio > 0.25f) return Color.Lerp(primary, good, Smooth((ratio - 0.25f) * 4f));
        return Color.Lerp(alert, primary, Smooth(ratio * 4f));
    }

    public string PaperHighlights(string richText, Color bodyColor)
    {
        if (string.IsNullOrEmpty(richText) || Luminance(bodyColor) > 0.4f) return richText;
        if (richText.IndexOf(ColorTag, System.StringComparison.OrdinalIgnoreCase) < 0) return richText;

        HighlightBuilder.Clear();
        int cursor = 0;
        while (cursor < richText.Length)
        {
            int open = richText.IndexOf(ColorTag, cursor, System.StringComparison.OrdinalIgnoreCase);
            int close = open >= 0 ? richText.IndexOf('>', open) : -1;
            if (open < 0 || close < 0)
            {
                HighlightBuilder.Append(richText, cursor, richText.Length - cursor);
                break;
            }

            HighlightBuilder.Append(richText, cursor, open - cursor);
            int codeStart = open + ColorTag.Length - 1;
            if (ColorUtility.TryParseHtmlString(richText.Substring(codeStart, close - codeStart), out Color parsed))
                HighlightBuilder.Append(ColorTag).Append(ColorUtility.ToHtmlStringRGB(PaperHighlight(parsed))).Append('>');
            else
                HighlightBuilder.Append(richText, open, close - open + 1);
            cursor = close + 1;
        }

        return HighlightBuilder.ToString();
    }

    public Color PaperHighlight(Color color)
    {
        float paper = Luminance(cream) + 0.05f;
        float word = Luminance(color) + 0.05f;
        if ((paper > word ? paper / word : word / paper) >= paperContrast) return color;

        Color.RGBToHSV(color, out float hue, out float saturation, out _);
        if (saturation < 0.15f) return ink;

        float degrees = hue * 360f;
        if (degrees < 20f || degrees >= 335f) return danger;
        if (degrees < 70f) return paperGold;
        if (degrees < 160f) return paperGreen;
        if (degrees < 200f) return paperCyan;
        if (degrees < 255f) return paperBlue;
        return anomaly;
    }

    private static float Luminance(Color color)
    {
        Color linear = color.linear;
        return 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public Color TextColor(UITextRole role)
    {
        switch (role)
        {
            case UITextRole.Heading: return cyan;
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
                return FontStyles.UpperCase;
            default:
                return FontStyles.Normal;
        }
    }

    public float TextSpacing(UITextRole role)
    {
        switch (role)
        {
            case UITextRole.Title:
            case UITextRole.Heading:
            case UITextRole.Label:
                return labelSpacing;
            case UITextRole.Number:
                return labelSpacing * 0.5f;
            default:
                return 0f;
        }
    }

    public void ApplyText(TMP_Text text, UITextRole role)
    {
        if (text == null) return;

        ApplyFont(text, role);
        text.color = TextColor(role);
        text.fontStyle = TextStyle(role);
        text.characterSpacing = TextSpacing(role);
        text.extraPadding = role != UITextRole.BodyDark;
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
        ApplySprite(image, large ? panelDark : panelSmall, Color.white, 0f, large ? panelShadowOffset : shadowOffset, large ? screenScan : null);
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

    public static void SetLip(Image image, Color color)
    {
        Shadow shadowEffect = image != null ? image.GetComponent<Shadow>() : null;
        if (shadowEffect != null) shadowEffect.effectColor = color;
    }

    public void ApplyButton(Button button, UIPlateRole role)
    {
        if (button == null) return;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            ApplySprite(image, plate, Color.white, skew, shadowOffset, plateStripes);
        }

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
