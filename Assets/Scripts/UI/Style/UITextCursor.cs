using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UITextCursor : MonoBehaviour
{
    [Tooltip("Texto al que sigue el cursor: se coloca justo después de su última letra.")]
    [SerializeField] private TMP_Text target;
    [Tooltip("Separación entre el avance de la última letra y el cursor, en unidades de canvas. Negativo lo pega más al texto.")]
    [SerializeField] private float gap = 1f;
    [Tooltip("Segundos que tarda cada medio parpadeo.")]
    [SerializeField] private float halfPeriod = 0.5f;

    private Graphic graphic;
    private Tween tween;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        Color color = graphic.color;
        color.a = 1f;
        graphic.color = color;
        tween = graphic.DOFade(0f, halfPeriod).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        StartCoroutine(PlaceAfterLayout());
    }

    private void OnDisable()
    {
        tween?.Kill();
        tween = null;
    }

    private IEnumerator PlaceAfterLayout()
    {
        yield return null;
        Place();
    }

    private void Place()
    {
        if (target == null) return;

        target.ForceMeshUpdate();
        TMP_TextInfo info = target.textInfo;
        int last = info.characterCount - 1;
        while (last >= 0 && !info.characterInfo[last].isVisible) last--;
        if (last < 0) return;

        TMP_CharacterInfo character = info.characterInfo[last];
        RectTransform rect = (RectTransform)transform;
        Vector3 world = target.transform.TransformPoint(new Vector3(character.xAdvance + gap, character.baseLine, 0f));
        Vector3 local = rect.parent.InverseTransformPoint(world);
        Vector2 size = rect.rect.size;
        rect.localPosition = new Vector3(local.x + rect.pivot.x * size.x, local.y + rect.pivot.y * size.y, rect.localPosition.z);
    }
}
