using UnityEngine;
using DG.Tweening;

public class MenuBackgroundCamera : MonoBehaviour
{
    [Header("Órbita")]
    [SerializeField] private Vector3 orbitCenter = Vector3.zero;
    [SerializeField] private float orbitRadius = 30f;
    [SerializeField] private float orbitHeight = 25f;
    [SerializeField] private float orbitSpeed = 4f;

    [Header("Fade de entrada")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeInDuration = 1.5f;

    private float orbitAngle;

    private void Start()
    {
        orbitAngle = Random.Range(0f, 360f);
        transform.position = OrbitPosition(orbitAngle);
        transform.rotation = Quaternion.LookRotation(orbitCenter - transform.position);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.DOFade(0f, fadeInDuration).SetUpdate(true);
        }
    }

    private void Update()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        transform.position = OrbitPosition(orbitAngle);
        transform.rotation = Quaternion.LookRotation(orbitCenter - transform.position);
    }

    private Vector3 OrbitPosition(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return orbitCenter + new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(rad) * orbitRadius
        );
    }
}
