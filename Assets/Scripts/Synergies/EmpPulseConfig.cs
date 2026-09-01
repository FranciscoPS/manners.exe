using UnityEngine;

[CreateAssetMenu(fileName = "EmpPulseConfig", menuName = "Game/Synergies/EMP Pulse Config")]
public class EmpPulseConfig : SynergyEffectConfig
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

    [Header("Visual")]
    [Tooltip("Si se asigna, se instancia este prefab (VFX o modelo) en cada pulso en vez del círculo generado por código. Con un prefab propio, tú controlas su propia animación de expansión; 'Ring Lifetime' se ignora en ese caso.")]
    public GameObject visualPrefabOverride;
    [Tooltip("Color del círculo generado por código. Solo se usa si 'Visual Prefab Override' está vacío.")]
    public Color ringColor = new Color(0.6f, 0.9f, 1f, 0.5f);
    [Tooltip("Cuánto tiempo permanece visible el círculo ya expandido, antes de desaparecer. Solo aplica si 'Visual Prefab Override' está vacío.")]
    public float ringLifetime = 0.4f;

    public override void ApplyTo(GameObject effectInstance)
    {
        EmpPulseEffect effect = effectInstance.GetComponent<EmpPulseEffect>();
        effect?.Configure(this);
    }
}
