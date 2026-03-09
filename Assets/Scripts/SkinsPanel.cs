using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class SkinsPanel : MonoBehaviour
{
    [Header("Asignaciones (Inspector)")]
    [Tooltip("Imagen que se actualizará con el sprite del primer grupo de botones.")]
    [SerializeField] private Image targetImageA;

    [Tooltip("Imagen que se actualizará con el sprite del segundo grupo de botones.")]
    [SerializeField] private Image targetImageB;

    [Tooltip("Botones cuyo sprite se copiará a Target Image A al pulsarlos.")]
    [SerializeField] private Button[] sourceButtonsA = new Button[0];

    [Tooltip("Botones cuyo sprite se copiará a Target Image B al pulsarlos.")]
    [SerializeField] private Button[] sourceButtonsB = new Button[0];

    private readonly Dictionary<Button, UnityAction> buttonHandlers = new Dictionary<Button, UnityAction>();
    private readonly Dictionary<Button, Image> buttonTargetMap = new Dictionary<Button, Image>();

    private void OnEnable()
    {
        SubscribeAll();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UnsubscribeAll();
        SubscribeAll();
    }

    private void SubscribeAll()
    {
        UnsubscribeAll();

        if (sourceButtonsA != null)
        {
            for (int i = 0; i < sourceButtonsA.Length; i++)
            {
                var btn = sourceButtonsA[i];
                if (btn == null) continue;

                if (buttonHandlers.ContainsKey(btn))
                {
                    btn.onClick.RemoveListener(buttonHandlers[btn]);
                    buttonHandlers.Remove(btn);
                    buttonTargetMap.Remove(btn);
                }

                Button localBtn = btn;
                Image target = targetImageA;
                UnityAction handler = () => OnSourceButtonClicked(localBtn, target);

                buttonHandlers[localBtn] = handler;
                buttonTargetMap[localBtn] = target;
                localBtn.onClick.AddListener(handler);
            }
        }

        if (sourceButtonsB != null)
        {
            for (int i = 0; i < sourceButtonsB.Length; i++)
            {
                var btn = sourceButtonsB[i];
                if (btn == null) continue;

                if (buttonHandlers.ContainsKey(btn))
                {
                    btn.onClick.RemoveListener(buttonHandlers[btn]);
                    buttonHandlers.Remove(btn);
                    buttonTargetMap.Remove(btn);
                }

                Button localBtn = btn;
                Image target = targetImageB;
                UnityAction handler = () => OnSourceButtonClicked(localBtn, target);

                buttonHandlers[localBtn] = handler;
                buttonTargetMap[localBtn] = target;
                localBtn.onClick.AddListener(handler);
            }
        }
    }

    private void UnsubscribeAll()
    {
        foreach (var kv in buttonHandlers)
        {
            if (kv.Key != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }
        buttonHandlers.Clear();
        buttonTargetMap.Clear();
    }

    private void OnSourceButtonClicked(Button clickedButton, Image target)
    {
        if (clickedButton == null || target == null) return;

        Image btnImage = clickedButton.image;
        if (btnImage == null || btnImage.sprite == null) return;

        target.sprite = btnImage.sprite;
    }
}
