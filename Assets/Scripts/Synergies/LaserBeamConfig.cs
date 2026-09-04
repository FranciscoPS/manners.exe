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
    [Tooltip("Prefab del VFX del rayo (Assets/Prefabs/VFX/LaserBeam.prefab). Se conservan su curva de grosor, su gradiente de color y su Particle System: una copia brilla en el origen del rayo y otra en el punto de impacto. Si se deja vacío, el rayo se construye por código con 'Beam Material Override' o con un unlit de 'Beam Color'.")]
    public GameObject visualPrefabOverride;
    [Tooltip("Material del LineRenderer. Con prefab asignado sustituye al material que trae el prefab; sin prefab se usa directo. 'LaserBeam_Mat' (shader Custom/LaserBeam) repite la textura por unidad de largo para conservar el look del diseño a cualquier distancia; el 'RayoLaser_Mat' original la estira a lo largo de todo el rayo.")]
    public Material beamMaterialOverride;
    [Tooltip("Altura sobre el jugador desde la que sale el rayo. El origen del rayo no se mueve; solo el punto de impacto en el piso se desliza hacia afuera.")]
    public float beamOriginHeight = 1.6f;
    [Tooltip("Color del rayo. Solo se usa si no hay prefab ni 'Beam Material Override'.")]
    public Color beamColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Grosor visual del rayo (no afecta a quién golpea; eso lo decide 'Impact Radius'). Con prefab asignado multiplica su curva de grosor.")]
    public float lineWidth = 0.5f;
    [Tooltip("Segundos que tarda el rayo en alcanzar su grosor completo al aparecer.")]
    public float beamFadeIn = 0.06f;
    [Tooltip("Segundos del final del barrido durante los que el rayo se adelgaza hasta desaparecer.")]
    public float beamFadeOut = 0.2f;

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce cada vez que se dispara un barrido. Opcional.")]
    public AudioClip fireSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    public override void ApplyTo(GameObject effectInstance)
    {
        LaserBeamEffect effect = effectInstance.GetComponent<LaserBeamEffect>();
        effect?.Configure(this);
    }
}
