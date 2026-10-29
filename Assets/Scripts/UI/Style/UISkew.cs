using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Skew")]
public class UISkew : BaseMeshEffect
{
    [Tooltip("Desplazamiento horizontal por cada unidad de altura. Positivo = la parte superior se inclina a la derecha.")]
    [SerializeField] private float amount = 0.22f;

    public float Amount
    {
        get => amount;
        set
        {
            if (Mathf.Approximately(amount, value)) return;
            amount = value;
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || Mathf.Approximately(amount, 0f)) return;

        float centerY = graphic.rectTransform.rect.center.y;
        UIVertex vertex = default;
        int count = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.position.x += (vertex.position.y - centerY) * amount;
            vh.SetUIVertex(vertex, i);
        }
    }
}
