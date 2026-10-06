using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverrideStripCellUI : MonoBehaviour
{
    [Header("Referencias del icono")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text fallback;

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        fallback.gameObject.SetActive(sprite == null);
    }
}
