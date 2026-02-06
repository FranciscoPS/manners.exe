using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Configuration")]
public class EnemyConfiguration : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 3f;
    public float contactDamage = 10f;
    
    [Header("Experience Drop")]
    public OrbConfiguration orbConfig;
    public int minOrbs = 1;
    public int maxOrbs = 3;
    public float orbSpawnRadius = 1f;
    
    [Header("Visual")]
    public Mesh mesh;
    public Material material;
    public Color color = Color.white;
    public Vector3 scale = Vector3.one;
    
    [Header("Effects")]
    public GameObject deathEffect;
    public bool hasLight = false;
    public Color lightColor = Color.red;
    public float lightIntensity = 3f;
    
    public void ApplyToEnemy(GameObject enemyObject)
    {
        EnemyController controller = enemyObject.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.SetStats(moveSpeed, contactDamage);
        }
        
        EnemyHealth health = enemyObject.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.SetConfiguration(maxHealth, orbConfig, minOrbs, maxOrbs, orbSpawnRadius);
        }
        
        ApplyVisuals(enemyObject);
    }
    
    private void ApplyVisuals(GameObject enemyObject)
    {
        MeshFilter meshFilter = enemyObject.GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }
        
        Renderer renderer = enemyObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material matInstance = null;
            if (material != null)
            {
                matInstance = new Material(material);
                renderer.material = matInstance;
            }
            else
            {
                matInstance = renderer.material;
            }
            
            if (matInstance != null)
            {
                matInstance.color = color;
                
                if (matInstance.HasProperty("_BaseColor"))
                    matInstance.SetColor("_BaseColor", color);
                if (matInstance.HasProperty("_Color"))
                    matInstance.SetColor("_Color", color);
                if (matInstance.HasProperty("_EmissionColor"))
                    matInstance.SetColor("_EmissionColor", color * 0.3f);
            }
        }
        
        enemyObject.transform.localScale = scale;
        
        Light light = enemyObject.GetComponent<Light>();
        if (hasLight)
        {
            if (light == null)
            {
                light = enemyObject.AddComponent<Light>();
                light.type = LightType.Point;
            }
            light.color = lightColor;
            light.intensity = lightIntensity;
            light.range = 5f;
            light.enabled = true;
        }
        else if (light != null)
        {
            light.enabled = false;
        }
    }
}
