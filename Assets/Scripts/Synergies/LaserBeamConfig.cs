using UnityEngine;

[CreateAssetMenu(fileName = "LaserBeamConfig", menuName = "Game/Synergies/Laser Beam Config")]
public class LaserBeamConfig : SynergyEffectConfig
{
    [Header("Objetivo")]
    [Tooltip("Cada cuántos segundos se dispara un nuevo barrido.")]
    public float interval = 3f;
    [Tooltip("Distancia máxima para buscar al enemigo más cercano al activarse el barrido. Si no hay ningún enemigo dentro de este radio, se usa también para buscar un edificio al azar (o, si tampoco hay, un punto al azar) — así se ve que la sinergia sigue activa.")]
    public float range = 14f;

    [Header("Barrido")]
    [Tooltip("Daño aplicado a los enemigos cerca del punto de impacto, en cada tick mientras dura el barrido.")]
    public float damage = 5f;
    [Tooltip("Cada cuántos segundos se aplica daño mientras el rayo barre el piso.")]
    public float damageTickInterval = 0.25f;
    [Tooltip("Radio alrededor del punto de impacto en el piso donde se aplica daño.")]
    public float impactRadius = 1.2f;
    [Tooltip("Cuánto tarda el punto de impacto en deslizarse desde el enemigo detectado hasta el final del barrido.")]
    public float sweepDuration = 1.2f;
    [Tooltip("Cuánto más allá del enemigo detectado se extiende el barrido, en la misma línea recta.")]
    public float extendDistance = 4f;

    [Header("Visual")]
    [Tooltip("Altura sobre el jugador desde la que sale el rayo. El origen del rayo no se mueve; solo el punto de impacto en el piso se desliza hacia afuera.")]
    public float beamOriginHeight = 1.6f;
    [Tooltip("Color del rayo. Solo se usa si 'Beam Material Override' está vacío.")]
    public Color beamColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Grosor visual de la línea (no afecta a quién golpea; eso lo decide 'Impact Radius').")]
    public float lineWidth = 0.15f;
    [Tooltip("Si se asigna, se usa este material en el LineRenderer en vez del material unlit + color por defecto. Ideal para un shader de energía con textura o scroll de UV.")]
    public Material beamMaterialOverride;

    public override void ApplyTo(GameObject effectInstance)
    {
        LaserBeamEffect effect = effectInstance.GetComponent<LaserBeamEffect>();
        effect?.Configure(this);
    }
}
