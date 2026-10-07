using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public class UITextCursor : MonoBehaviour
{
    [Tooltip("Texto al que sigue el cursor: se coloca justo después de su última letra.")]
    [SerializeField] private TMP_Text target;
    [Tooltip("Separación entre el avance de la última letra y el cursor, en unidades del texto.")]
    [SerializeField] private float gap = 1f;
    [Tooltip("Segundos que tarda cada medio parpadeo.")]
    [SerializeField] private float halfPeriod = 0.5f;

    private Graphic graphic;
    private RectTransform rect;
    private Tween tween;
    private Matrix4x4 targetMatrix;
    private Matrix4x4 parentMatrix;
    private Vector2 cursorSize;
    private Vector2 cursorPivot;
    private float placedGap;
    private float placedFontSize;
    private bool needsPlacement = true;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        if (graphic == null) graphic = GetComponent<Graphic>();
        if (rect == null) rect = (RectTransform)transform;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Canvas.willRenderCanvases += OnCanvasWillRender;
        needsPlacement = true;
        if (!Application.IsPlaying(gameObject)) return;
        Color color = graphic.color;
        color.a = 1f;
        graphic.color = color;
        tween = graphic.DOFade(0f, Mathf.Max(0.01f, halfPeriod)).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        Canvas.willRenderCanvases -= OnCanvasWillRender;
        tween?.Kill();
        tween = null;
    }

    private void OnValidate() => needsPlacement = true;
    private void OnRectTransformDimensionsChange() => needsPlacement = true;
    private void OnTransformParentChanged() => needsPlacement = true;

    private void OnTextChanged(Object changed)
    {
        if (changed == target) Place();
    }

    private void OnCanvasWillRender()
    {
        if (target == null || !target.isActiveAndEnabled || rect == null || rect.parent == null) return;
        if (!needsPlacement && !target.havePropertiesChanged && targetMatrix == target.transform.localToWorldMatrix && parentMatrix == rect.parent.worldToLocalMatrix && cursorSize == rect.rect.size && cursorPivot == rect.pivot && placedGap == gap && placedFontSize == target.fontSize) return;
        if (target.havePropertiesChanged) target.ForceMeshUpdate();
        Place();
    }

    private void Place()
    {
        if (target == null || !target.isActiveAndEnabled || rect == null || rect.parent == null) return;
        TMP_TextInfo info = target.textInfo;
        int last = info.characterCount - 1;
        while (last >= 0 && !info.characterInfo[last].isVisible) last--;
        if (last < 0) return;

        TMP_CharacterInfo character = info.characterInfo[last];
        float right = Mathf.Max(character.xAdvance, Mathf.Max(character.topRight.x, character.bottomRight.x));
        Vector3 world = target.transform.TransformPoint(new Vector3(right + gap, character.baseLine, 0f));
        Vector3 local = rect.parent.InverseTransformPoint(world);
        Vector2 size = rect.rect.size;
        Vector3 position = new Vector3(local.x + rect.pivot.x * size.x, local.y + rect.pivot.y * size.y, rect.localPosition.z);
        if ((rect.localPosition - position).sqrMagnitude > 0.0001f) rect.localPosition = position;
        targetMatrix = target.transform.localToWorldMatrix;
        parentMatrix = rect.parent.worldToLocalMatrix;
        cursorSize = size;
        cursorPivot = rect.pivot;
        placedGap = gap;
        placedFontSize = target.fontSize;
        needsPlacement = false;
    }
}
