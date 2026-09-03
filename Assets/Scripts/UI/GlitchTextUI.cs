using UnityEngine;
using TMPro;

public class GlitchTextUI : MonoBehaviour
{
    private enum Phase { Idle, Burst }

    [SerializeField] private TextMeshProUGUI target;

    [Tooltip("Parte inicial del texto que nunca se altera (ej. 'Sin' para 'Sinergias').")]
    [SerializeField] private string prefix = "Sin";

    [Header("Ritmo")]
    [Tooltip("Pausa mínima/máxima (segundos) leyendo el texto normal entre un glitch y otro.")]
    [SerializeField] private float idleMin = 0.7f;
    [SerializeField] private float idleMax = 2.2f;

    [Tooltip("Duración mínima/máxima (segundos) de cada ráfaga de glitch.")]
    [SerializeField] private float burstMin = 0.12f;
    [SerializeField] private float burstMax = 0.4f;

    [Tooltip("Cada cuántos segundos cambia el 'frame' del glitch dentro de una ráfaga (0.06 ≈ 16 fps, como posterizeTime).")]
    [SerializeField] private float stepInterval = 0.06f;

    [Tooltip("Probabilidad de que tras una ráfaga venga otra casi inmediata (tartamudeo).")]
    [Range(0f, 1f)] [SerializeField] private float doubleBurstChance = 0.35f;
    [SerializeField] private float doubleBurstGapMin = 0.08f;
    [SerializeField] private float doubleBurstGapMax = 0.2f;

    [Header("Letras")]
    [Tooltip("Probabilidad de que una ráfaga cambie letras; el resto solo aplica estática sin tocar el texto.")]
    [Range(0f, 1f)] [SerializeField] private float scrambleChance = 0.6f;

    [Tooltip("Probabilidad, por letra y por frame, de reemplazarla por un símbolo durante una ráfaga con letras.")]
    [Range(0f, 1f)] [SerializeField] private float letterReplaceChance = 0.7f;

    [SerializeField] private string glitchCharacters = "!@#$%&*?/\\|<>[]{}01";

    [Header("Estática")]
    [Tooltip("Desplazamiento máximo en X/Y (px) por frame durante una ráfaga.")]
    [SerializeField] private float jitterX = 4f;
    [SerializeField] private float jitterY = 2f;

    [Tooltip("Parpadeo de color tipo separación RGB durante las ráfagas.")]
    [SerializeField] private bool useColorFlicker = true;
    [SerializeField] private Color flickerColorA = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private Color flickerColorB = new Color(1f, 0f, 1f, 1f);

    private Phase phase;
    private float phaseTimer;
    private float stepTimer;
    private bool scrambleThisBurst;

    private string originalText;
    private int lockedCount;
    private Vector2 baseAnchoredPosition;
    private Color baseColor;
    private bool captured;

    private void OnEnable()
    {
        if (target == null)
            target = GetComponent<TextMeshProUGUI>();

        if (target == null) return;

        Capture();
        phase = Phase.Idle;
        phaseTimer = Random.Range(idleMin, idleMax);
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Update()
    {
        if (!captured) return;

        float dt = Time.unscaledDeltaTime;
        phaseTimer -= dt;

        if (phase == Phase.Idle)
        {
            if (phaseTimer <= 0f)
                StartBurst();
            return;
        }

        stepTimer -= dt;
        if (stepTimer <= 0f)
            Step();

        if (phaseTimer <= 0f)
            EndBurst();
    }

    private void Capture()
    {
        originalText = target.text;
        lockedCount = originalText.StartsWith(prefix) ? prefix.Length : 0;
        baseAnchoredPosition = target.rectTransform.anchoredPosition;
        baseColor = target.color;
        captured = true;
    }

    private void Restore()
    {
        if (!captured || target == null) return;

        target.text = originalText;
        target.rectTransform.anchoredPosition = baseAnchoredPosition;
        target.color = baseColor;
    }

    private void StartBurst()
    {
        phase = Phase.Burst;
        phaseTimer = Random.Range(burstMin, burstMax);
        scrambleThisBurst = Random.value < scrambleChance;
        stepTimer = 0f;
    }

    private void Step()
    {
        stepTimer = stepInterval;

        if (scrambleThisBurst)
            target.text = Scramble();

        target.rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(
            Random.Range(-jitterX, jitterX),
            Random.Range(-jitterY, jitterY));

        if (useColorFlicker)
        {
            float roll = Random.value;
            target.color = roll < 0.33f ? flickerColorA : roll < 0.66f ? flickerColorB : baseColor;
        }
    }

    private void EndBurst()
    {
        Restore();
        phase = Phase.Idle;
        phaseTimer = Random.value < doubleBurstChance
            ? Random.Range(doubleBurstGapMin, doubleBurstGapMax)
            : Random.Range(idleMin, idleMax);
    }

    private string Scramble()
    {
        char[] chars = originalText.ToCharArray();

        for (int i = lockedCount; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i])) continue;
            if (Random.value < letterReplaceChance)
                chars[i] = glitchCharacters[Random.Range(0, glitchCharacters.Length)];
        }

        return new string(chars);
    }
}
