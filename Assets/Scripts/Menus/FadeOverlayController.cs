using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class FadeOverlayController : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        canvasGroup = GetComponent<CanvasGroup>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
            canvasGroup.alpha = 0f;
        }

        Destroy(gameObject);
    }
}
