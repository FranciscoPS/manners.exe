using UnityEngine;

[CreateAssetMenu(fileName = "EmpPulseConfig", menuName = "Game/Synergies/EMP Pulse Config")]
public class EmpPulseConfig : SynergyEffectConfig
{
    [Header("Pulso")]
    [Tooltip("Cada cuántos segundos se dispara el pulso.")]
    public float interval = 5f;
    [Tooltip("Radio inicial del pulso alrededor del jugador.")]
    public float radius = 5f;
    [Tooltip("Cuánto tiempo quedan congelados (velocidad 0) los enemigos alcanzados.")]
    public float freezeDuration = 2f;
    [Tooltip("Distancia a la que el congelamiento se contagia de un enemigo ya congelado a otro cercano. El contagio no se acumula: solo aplica el mismo freezeDuration.")]
    public float chainRadius = 2.5f;

    [Header("Visual")]
    [Tooltip("Si se asigna, se instancia este prefab (VFX o modelo) en cada pulso en vez del anillo generado por código. Con un prefab propio, tú controlas cuándo desaparece (por ejemplo un Particle System con Stop Action: Destroy); 'Ring Lifetime' se ignora en ese caso.")]
    public GameObject visualPrefabOverride;
    [Tooltip("Color del anillo generado por código. Solo se usa si 'Visual Prefab Override' está vacío.")]
    public Color ringColor = new Color(0.6f, 0.9f, 1f, 0.5f);
    [Tooltip("Cuánto tiempo permanece visible el anillo generado por código. Solo se usa si 'Visual Prefab Override' está vacío.")]
    public float ringLifetime = 0.4f;

    public override void ApplyTo(GameObject effectInstance)
    {
        EmpPulseEffect effect = effectInstance.GetComponent<EmpPulseEffect>();
        effect?.Configure(this);
    }
}
