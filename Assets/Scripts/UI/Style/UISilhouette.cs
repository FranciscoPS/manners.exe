using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Silhouette")]
public class UISilhouette : BaseMeshEffect
{
    private const int RingSteps = 8;

    [Tooltip("Color de la silueta que queda detrás de la imagen.")]
    [SerializeField] private Color color = new Color(0.024f, 0.039f, 0.17f, 1f);
    [Tooltip("Grosor del contorno alrededor de la imagen.")]
    [SerializeField] private float outline = 4f;
    [Tooltip("Desplazamiento de la sombra recortada que asoma por un lado.")]
    [SerializeField] private Vector2 offset = new Vector2(12f, -9f);

    private static readonly List<UIVertex> Source = new List<UIVertex>();
    private static readonly List<UIVertex> Output = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        Source.Clear();
        Output.Clear();
        vh.GetUIVertexStream(Source);

        if (offset.sqrMagnitude > 0.0001f)
            AddCopy(offset);

        if (outline > 0f)
        {
            for (int i = 0; i < RingSteps; i++)
            {
                float angle = i * (Mathf.PI * 2f / RingSteps);
                AddCopy(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outline);
            }
        }

        Output.AddRange(Source);
        vh.Clear();
        vh.AddUIVertexTriangleStream(Output);
    }

    private void AddCopy(Vector2 shift)
    {
        Color32 tint = color;

        for (int i = 0; i < Source.Count; i++)
        {
            UIVertex vertex = Source[i];
            vertex.position.x += shift.x;
            vertex.position.y += shift.y;
            vertex.color = new Color32(tint.r, tint.g, tint.b, (byte)(tint.a * vertex.color.a / 255));
            Output.Add(vertex);
        }
    }
}
