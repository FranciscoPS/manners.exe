using System;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InitialsEntryUI : MonoBehaviour
{
    private const int InitialsLength = 3;

    [Header("Navegación")]
    [SerializeField] private Button firstLetterButton;

    [Header("Slots de iniciales")]
    [SerializeField] private TextMeshProUGUI[] initialSlots;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private float cursorBlinkDuration = 0.5f;

    public event Action<string> OnInitialsConfirmed;

    private readonly StringBuilder buffer = new StringBuilder();
    private Tween cursorTween;

    private void OnEnable()
    {
        buffer.Clear();
        RefreshSlots();

        if (firstLetterButton != null)
            EventSystem.current?.SetSelectedGameObject(firstLetterButton.gameObject);

        StartCursorBlink();
    }

    private void OnDisable()
    {
        cursorTween?.Kill();
    }

    public void OnLetterPressed(string letter)
    {
        if (letter == "DEL")
        {
            if (buffer.Length > 0)
                buffer.Length--;
        }
        else if (letter == "END")
        {
            if (buffer.Length == 0) return;
            OnInitialsConfirmed?.Invoke(buffer.ToString().PadRight(InitialsLength));
            return;
        }
        else
        {
            if (buffer.Length >= InitialsLength) return;
            buffer.Append(letter == "SPC" ? " " : letter);
        }

        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (initialSlots == null) return;

        for (int i = 0; i < initialSlots.Length; i++)
            initialSlots[i].text = i < buffer.Length ? buffer[i].ToString() : "_";

        if (cursor != null && initialSlots.Length > 0)
        {
            int index = Mathf.Min(buffer.Length, initialSlots.Length - 1);
            cursor.position = initialSlots[index].rectTransform.position;
        }
    }

    private void StartCursorBlink()
    {
        cursorTween?.Kill();
        if (cursor == null) return;

        Graphic graphic = cursor.GetComponent<Graphic>();
        if (graphic == null) return;

        Color c = graphic.color;
        graphic.color = new Color(c.r, c.g, c.b, 1f);
        cursorTween = graphic.DOFade(0f, cursorBlinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }
}
