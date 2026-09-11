using UnityEngine;
using UnityEngine.Rendering;

public static class FlipbookMaterialUtility
{
    // The normal prefab path shares the authored material. Only a texture
    // override or missing template needs an instance, owned by that effect.
    public static Material Resolve(Material template, Texture texture, bool additive, out Material owned)
    {
        owned = null;
        if (template != null && (texture == null || template.mainTexture == texture)) return template;
        if (template != null) owned = new Material(template);
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) return null;
            owned = new Material(shader);
            owned.SetFloat("_Surface", 1);
            owned.SetFloat("_BlendOp", 0);
            owned.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            owned.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            owned.SetFloat("_ZWrite", 0);
            owned.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            owned.renderQueue = 3000;
        }
        if (texture != null) owned.mainTexture = texture;
        return owned;
    }

    public static void Release(ref Material owned)
    {
        if (owned == null) return;
        if (Application.isPlaying) Object.Destroy(owned);
        else Object.DestroyImmediate(owned);
        owned = null;
    }
}
