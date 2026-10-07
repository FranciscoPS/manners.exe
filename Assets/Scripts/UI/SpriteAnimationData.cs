using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteAnimation", menuName = "Game/Sprite Animation")]
public class SpriteAnimationData : ScriptableObject
{
    [Header("=== FRAMES ===")]
    [Tooltip("Frames en orden de reproducción. Para llenarlos en el orden de la cuadrícula del spritesheet (de arriba hacia abajo y de izquierda a derecha), arrastra cualquier frame del sheet aquí y usa ⋮ > Cargar frames del spritesheet.")]
    [SerializeField] private Sprite[] frames = new Sprite[0];

    [Header("=== REPRODUCCIÓN ===")]
    [Tooltip("Frames por segundo. Corre en tiempo real, así que también avanza mientras el tutorial congela el juego.")]
    [SerializeField][Min(1f)] private float framesPerSecond = 12f;

    [Tooltip("Índice del primer frame del tramo que se repite. Los frames anteriores son la entrada y se reproducen una sola vez; desde este frame hasta el último se repiten mientras el robot siga hablando (el texto se sigue escribiendo). Cuando el texto termina, se completa la vuelta en curso y la imagen se queda en el último frame. 0 = se repite la animación completa.")]
    [SerializeField][Min(0)] private int loopStartFrame = 0;

    public Sprite[] Frames => frames;
    public bool HasFrames => frames != null && frames.Length > 0;
    public float FrameDuration => 1f / framesPerSecond;
    public int LoopStartFrame => Mathf.Clamp(loopStartFrame, 0, frames.Length - 1);

#if UNITY_EDITOR
    [ContextMenu("Cargar frames del spritesheet")]
    private void LoadFramesFromSpritesheet()
    {
        Sprite reference = frames?.FirstOrDefault(frame => frame != null);
        if (reference == null)
        {
            Debug.LogWarning($"[{name}] Arrastra primero cualquier frame del spritesheet a Frames.", this);
            return;
        }

        string sheetPath = UnityEditor.AssetDatabase.GetAssetPath(reference.texture);
        UnityEditor.Undo.RecordObject(this, "Cargar frames del spritesheet");
        frames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderByDescending(frame => frame.rect.y)
            .ThenBy(frame => frame.rect.x)
            .ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
