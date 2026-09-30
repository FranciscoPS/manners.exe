using UnityEngine;

[CreateAssetMenu(fileName = "EmpPulseConfig", menuName = "Game/Overrides/EMP Pulse Config")]
public class EmpPulseConfig : OverrideEffectConfig
{
    [Header("Pulso")]
    [Tooltip("Cada cuántos segundos se dispara el pulso.")]
    public float interval = 5f;
    [Tooltip("Radio máximo que alcanza el círculo al terminar de expandirse.")]
    public float radius = 5f;
    [Tooltip("Cuánto tarda el círculo en expandirse desde el jugador hasta el radio máximo.")]
    public float expandDuration = 0.35f;
    [Tooltip("Cuánto tiempo quedan congelados/stunneados (velocidad 0) los enemigos alcanzados.")]
    public float freezeDuration = 2f;
    [Tooltip("Distancia a la que el congelamiento se contagia de un enemigo ya congelado a otro cercano, al terminar la expansión. El contagio no se acumula: aplica el mismo Freeze Duration, no lo suma.")]
    public float chainRadius = 2.5f;
    [Tooltip("Cuántos 'saltos' puede dar el contagio desde los enemigos alcanzados por el círculo. 1 = solo vecinos directos; 3 = vecinos de vecinos de vecinos. 0 = sin contagio. Evita que en una horda densa el pulso congele el mapa entero.")]
    [Min(0)] public int maxChainHops = 3;

    [Header("Combinaciones")]
    [Tooltip("Tope de pulsos adicionales mientras el jugador tenga Multi Shot. Se emiten siempre, en cada secuencia: en total, la mitad de las balas del Multi Shot, redondeada hacia arriba (4 balas = 2 ondas, 7 = 4, 10 = 5), sin pasar de 1 + este valor. 8 deja la regla de la mitad sin tope práctico; 0 desactiva la combinación.")]
    [Range(0, 8)] public int maxExtraPulses = 8;
    [Tooltip("Pausa entre la expansión completa de un pulso y el siguiente eco. Los ecos no generan más ecos; el intervalo normal empieza al terminar la secuencia.")]
    [Min(0.01f)] public float repeatDelay = 0.15f;
    [Tooltip("Multiplicador de la duración del congelamiento del EMP mientras Campo Criogénico esté activo.")]
    [Min(1f)] public float cryoFreezeDurationMultiplier = 1.5f;
    [Tooltip("Multiplicador del daño del láser contra enemigos congelados mientras el EMP esté activo.")]
    [Min(1f)] public float laserDamageMultiplier = 1.5f;

    [Header("Visual")]
    [Tooltip("Si se asigna, se instancia este prefab (VFX o modelo) en cada pulso en vez del círculo generado por código. Con un prefab propio, tú controlas su propia animación de expansión; 'Ring Lifetime' se ignora en ese caso.")]
    public GameObject visualPrefabOverride;
    [Tooltip("Color del círculo generado por código. Solo se usa si 'Visual Prefab Override' está vacío.")]
    public Color ringColor = new Color(0.6f, 0.9f, 1f, 0.5f);
    [Tooltip("Cuánto tiempo permanece visible el círculo ya expandido, antes de desaparecer. Solo aplica si 'Visual Prefab Override' está vacío.")]
    public float ringLifetime = 0.4f;

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce cada vez que se dispara un pulso. Opcional.")]
    public AudioClip pulseSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    public override void ApplyTo(GameObject effectInstance)
    {
        EmpPulseEffect effect = effectInstance.GetComponent<EmpPulseEffect>();
        effect?.Configure(this);
    }
}
