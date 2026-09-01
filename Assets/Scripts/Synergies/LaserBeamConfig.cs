using UnityEngine;

[CreateAssetMenu(fileName = "LaserBeamConfig", menuName = "Game/Synergies/Laser Beam Config")]
public class LaserBeamConfig : SynergyEffectConfig
{
    [Header("Disparo")]
    [Tooltip("Cada cuántos segundos se dispara el rayo.")]
    public float interval = 3f;
    [Tooltip("Daño aplicado a cada enemigo que toque el rayo. Perforante: golpea a todos en la línea, no se detiene en el primero.")]
    public float damage = 15f;
    [Tooltip("Distancia máxima que recorre el rayo.")]
    public float range = 14f;
    [Tooltip("Grosor de la zona de impacto del rayo (radio de detección a cada lado de la línea).")]
    public float width = 0.6f;

    [Header("Visual")]
    [Tooltip("Cuánto tiempo permanece visible el rayo en pantalla tras dispararse.")]
    public float beamDuration = 0.2f;
    [Tooltip("Color del rayo. Solo se usa si 'Beam Material Override' está vacío.")]
    public Color beamColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Grosor visual de la línea (no afecta el daño; eso lo controla 'Width').")]
    public float lineWidth = 0.15f;
    [Tooltip("Si se asigna, se usa este material en el LineRenderer en vez del material unlit + color por defecto. Ideal para un shader de energía con textura o scroll de UV.")]
    public Material beamMaterialOverride;

    public override void ApplyTo(GameObject effectInstance)
    {
        LaserBeamEffect effect = effectInstance.GetComponent<LaserBeamEffect>();
        effect?.Configure(this);
    }
}
