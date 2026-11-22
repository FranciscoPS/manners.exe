using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct HelicopterZoomPunch
{
    [Tooltip("Cuánto se cierra el plano (0.1 = 10 % menos campo de visión). 0 = sin acercamiento.")]
    [Range(0f, 0.5f)] public float strength;
    [Tooltip("Segundos que tarda en acercarse.")]
    [Min(0f)] public float attack;
    [Tooltip("Segundos que se queda cerca.")]
    [Min(0f)] public float hold;
    [Tooltip("Segundos que tarda en volver al plano normal.")]
    [Min(0f)] public float release;

    public HelicopterZoomPunch(float strength, float attack, float hold, float release)
    {
        this.strength = strength;
        this.attack = attack;
        this.hold = hold;
        this.release = release;
    }

    public float Duration => attack + hold + release;
}

[CreateAssetMenu(fileName = "HelicopterCameraConfig", menuName = "Game/Cámara de helicóptero")]
public sealed class HelicopterCameraConfig : ScriptableObject
{
    [Header("Encuadre (teleobjetivo)")]
    [Tooltip("Inclinación hacia abajo en grados. La cámara de siempre usa 45; más alto se ve más desde arriba.")]
    [Range(30f, 80f)] public float pitch = 52f;
    [Tooltip("Rumbo base en grados. 0 mira hacia el norte del mapa, igual que los controles.")]
    [Range(-180f, 180f)] public float heading = 0f;
    [Tooltip("Campo de visión vertical. Menos grados = perspectiva más comprimida, como un teleobjetivo. La cámara de siempre usa 60.")]
    [Range(15f, 70f)] public float fieldOfView = 36f;
    [Tooltip("Metros visibles de alto a la altura del robot. La distancia de la cámara se calcula para conservarlos con cualquier campo de visión (la cámara de siempre ve unos 23).")]
    [Min(5f)] public float framedHeight = 23f;

    [Header("Persecución del camarógrafo")]
    [Tooltip("Zona del centro de la pantalla en la que el robot se mueve sin que la cámara lo siga, como fracción de pantalla (0.1 = 10 % del ancho y del alto). Al salir de ella, la cámara lo persigue. 0 = siempre centrado.")]
    public Vector2 deadZone = new Vector2(0.1f, 0.1f);
    [Tooltip("Retraso con el que la cámara persigue al robot fuera de la zona muerta, en segundos aproximados. Más alto = persecución más lenta. La cámara de siempre usa 0.5.")]
    [Range(0f, 4f)] public float followDamping = 1.4f;
    [Tooltip("Límite duro como fracción de pantalla: el robot nunca sale de esta zona central aunque la cámara vaya atrasada (0.5 = la mitad central). 0 = sin límite.")]
    public Vector2 hardLimits = new Vector2(0.5f, 0.5f);
    [Tooltip("Segundos que el robot debe quedarse quieto para que el camarógrafo lo vuelva a centrar. 0 = nunca recentra.")]
    [Min(0f)] public float recenterDelay = 0.8f;
    [Tooltip("Velocidad en m/s por debajo de la cual el robot cuenta como quieto.")]
    [Min(0f)] public float idleSpeed = 0.5f;
    [Tooltip("Segundos que tarda la zona muerta en cerrarse al recentrar o en abrirse al volver a moverse.")]
    [Min(0.01f)] public float recenterBlend = 0.6f;

    [Header("Órbita del helicóptero")]
    [Tooltip("Activa el balanceo, la altura, la deriva y los acercamientos. Apagado deja el encuadre de teleobjetivo quieto (para quien se maree).")]
    public bool cameraMotion = true;
    [Tooltip("Grados máximos que el helicóptero rodea al robot hacia cada lado. Los controles siguen en ejes del mapa: por encima de 12 se empieza a notar.")]
    [Range(0f, 20f)] public float orbitAmplitude = 10f;
    [Tooltip("Segundos que tarda en ir de un lado al otro y volver.")]
    [Min(1f)] public float orbitPeriod = 22f;
    [Tooltip("Grados que la nariz del helicóptero sube y baja.")]
    [Range(0f, 6f)] public float pitchAmplitude = 2f;
    [Tooltip("Segundos de un ciclo completo de cabeceo.")]
    [Min(1f)] public float pitchPeriod = 9f;
    [Tooltip("Cuánto sube y baja el helicóptero, como fracción de la distancia (0.03 = 3 %). Se nota como un zoom suave.")]
    [Range(0f, 0.15f)] public float altitudeAmplitude = 0.03f;
    [Tooltip("Segundos de un ciclo completo de subida y bajada.")]
    [Min(1f)] public float altitudePeriod = 13f;

    [Header("Deriva del gimbal")]
    [Tooltip("Grados de deriva lenta de la cámara estabilizada: la mano del camarógrafo. 0 = trípode.")]
    [Range(0f, 2f)] public float driftAmplitude = 0.5f;
    [Tooltip("Velocidad de la deriva. Bajo = flota, alto = tiembla.")]
    [Range(0.01f, 2f)] public float driftFrequency = 0.3f;

    [Header("Zoom del camarógrafo")]
    [Tooltip("Acercamiento al subir de nivel.")]
    public HelicopterZoomPunch levelUp = new HelicopterZoomPunch(0.08f, 0.35f, 0.5f, 1.6f);
    [Tooltip("Acercamiento cuando aparece un cofre.")]
    public HelicopterZoomPunch chestSpawned = new HelicopterZoomPunch(0.06f, 0.3f, 0.6f, 1.6f);
    [Tooltip("Acercamiento al activar una sobrecarga.")]
    public HelicopterZoomPunch overrideActivated = new HelicopterZoomPunch(0.14f, 0.25f, 1f, 2f);
    [Tooltip("Acercamiento cuando la vida baja del umbral.")]
    public HelicopterZoomPunch lowHealth = new HelicopterZoomPunch(0.1f, 0.4f, 1.2f, 2.2f);
    [Tooltip("Fracción de vida bajo la cual el camarógrafo se acerca. Vuelve a armarse al recuperar un 10 % por encima.")]
    [Range(0.05f, 0.9f)] public float lowHealthThreshold = 0.3f;
    [Tooltip("Rapidez con que el lente sigue al zoom objetivo; suaviza un acercamiento que interrumpe a otro.")]
    [Min(0.5f)] public float zoomSmoothing = 10f;

    [Header("Plano abierto con hordas")]
    [Tooltip("Enemigos activos desde los que el camarógrafo empieza a abrir el plano.")]
    [Min(0)] public int crowdStart = 60;
    [Tooltip("Enemigos activos con los que el plano queda abierto del todo.")]
    [Min(1)] public int crowdFull = 220;
    [Tooltip("Cuánto se abre el campo de visión con la horda completa (0.15 = 15 % más).")]
    [Range(0f, 0.5f)] public float crowdWiden = 0.15f;
    [Tooltip("Segundos que tarda en abrir o cerrar el plano por completo.")]
    [Min(0.1f)] public float crowdResponse = 2.5f;

    [Header("Lente (señal de TV)")]
    [Tooltip("Perfil de post-proceso de la señal de TV. Se mezcla encima del estilo toon sin modificarlo.")]
    public VolumeProfile lensProfile;
    [Tooltip("Intensidad del perfil de lente. 0 = sin filtro.")]
    [Range(0f, 1f)] public float lensWeight = 1f;
    [Tooltip("Prioridad del Volume de lente. Debe ser mayor que la del Global Volume del nivel (0).")]
    public float lensPriority = 10f;
    [Tooltip("Inicio del desenfoque de fondo, en múltiplos de la distancia de la cámara al robot. El estilo toon equivale a 1.25 (25 m con la cámara a 20 m).")]
    [Min(0.1f)] public float depthOfFieldStart = 1.25f;
    [Tooltip("Fin del desenfoque de fondo, en múltiplos de la distancia de la cámara al robot. El estilo toon equivale a 2.5.")]
    [Min(0.2f)] public float depthOfFieldEnd = 2.5f;
}
