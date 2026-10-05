using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIScreenIntro : MonoBehaviour
{
    [Tooltip("Bloques que entran en cascada al mostrarse la pantalla. Vacío = los hijos directos que no ocupan toda la pantalla.")]
    [SerializeField] private RectTransform[] blocks;

    private Vector2[] restPositions;
    private Vector3[] restScales;
    private bool[] animateScale;

    private void Awake()
    {
        if (blocks == null || blocks.Length == 0)
            blocks = CollectBlocks();

        restPositions = new Vector2[blocks.Length];
        restScales = new Vector3[blocks.Length];
        animateScale = new bool[blocks.Length];

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null) continue;
            restPositions[i] = blocks[i].anchoredPosition;
            restScales[i] = blocks[i].localScale;
            animateScale[i] = blocks[i].GetComponent<MenuButtonHover>() == null;
        }
    }

    private RectTransform[] CollectBlocks()
    {
        var found = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            RectTransform rect = child as RectTransform;
            if (rect == null || IsFullScreen(rect)) continue;
            found.Add(rect);
        }
        return found.ToArray();
    }

    private static bool IsFullScreen(RectTransform rect)
    {
        return rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.sizeDelta.sqrMagnitude < 4f;
    }

    private void OnEnable()
    {
        UIStyle style = UIStyle.Instance;
        float duration = style != null ? style.introDuration : 0.34f;
        float stagger = style != null ? style.introStagger : 0.045f;
        float slide = style != null ? style.introSlide : 70f;
        int order = 0;

        DOTween.Kill(this);

        for (int i = 0; i < blocks.Length; i++)
        {
            RectTransform block = blocks[i];
            if (block == null || !block.gameObject.activeSelf) continue;

            Vector2 rest = restPositions[i];
            Vector3 center = transform.InverseTransformPoint(block.TransformPoint(block.rect.center));
            Vector2 offset = Mathf.Abs(center.x) < 60f ? new Vector2(0f, -slide * 0.6f) : new Vector2(center.x < 0f ? -slide : slide, 0f);
            float delay = order * stagger;
            order++;

            block.anchoredPosition = rest + offset;
            block.DOAnchorPos(rest, duration).SetDelay(delay).SetEase(Ease.OutBack, 1.4f).SetUpdate(true).SetTarget(this);

            if (!animateScale[i]) continue;

            block.localScale = restScales[i] * 0.94f;
            block.DOScale(restScales[i], duration).SetDelay(delay).SetEase(Ease.OutBack, 1.6f).SetUpdate(true).SetTarget(this);
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(this);

        for (int i = 0; i < blocks.Length; i++)
        {
            RectTransform block = blocks[i];
            if (block == null) continue;

            block.anchoredPosition = restPositions[i];
            if (animateScale[i]) block.localScale = restScales[i];
        }
    }
}
