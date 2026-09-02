using UnityEngine;

public static class SynergyVisualUtility
{
    public static GameObject CreateFlatDisc(string name, Transform parent, Vector3 position, float diameter, Color color)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        Object.Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(parent, false);

        if (parent != null)
            disc.transform.localPosition = position;
        else
            disc.transform.position = position;

        disc.transform.localScale = new Vector3(diameter, 0.02f, diameter);

        Renderer renderer = disc.GetComponent<Renderer>();
        Material material = new Material(FindLitShader());
        material.color = color;
        MakeTransparent(material);
        renderer.sharedMaterial = material;

        return disc;
    }

    public static GameObject CreateSphere(string name, Transform parent, Vector3 position, float diameter, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(parent, false);

        if (parent != null)
            sphere.transform.localPosition = position;
        else
            sphere.transform.position = position;

        sphere.transform.localScale = new Vector3(diameter, diameter, diameter);

        Renderer renderer = sphere.GetComponent<Renderer>();
        Material material = new Material(FindLitShader());
        material.color = color;
        MakeTransparent(material);
        renderer.sharedMaterial = material;

        return sphere;
    }

    public static Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }

    public static Shader FindUnlitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
        return shader;
    }

    public static void MakeTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
