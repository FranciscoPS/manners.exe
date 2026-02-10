using TMPro;
using UnityEngine;
using System.Collections;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;

    [Header("Rainbow Settings")]
    [SerializeField] private float colorChangeSpeed = 1f;

    private Coroutine rainbowCoroutine;

    private void OnEnable()
    {
        StartRainbowText();
    }

    private void OnDisable()
    {
        StopRainbowText();
    }

    private void StartRainbowText()
    {
        if (rainbowCoroutine != null)
            StopCoroutine(rainbowCoroutine);

        rainbowCoroutine = StartCoroutine(RainbowText());
    }

    private void StopRainbowText()
    {
        if (rainbowCoroutine != null)
            StopCoroutine(rainbowCoroutine);
    }

    private IEnumerator RainbowText()
    {
        float hue = 0f;

        while (true)
        {
            hue += Time.unscaledDeltaTime * colorChangeSpeed;

            if (hue > 1f)
                hue = 0f;

            levelUpText.color = Color.HSVToRGB(hue, 1f, 1f);

            yield return null;
        }
    }
}
