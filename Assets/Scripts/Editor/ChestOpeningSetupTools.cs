using UnityEditor;
using UnityEngine;

public static class ChestOpeningSetupTools
{
    private const string ConfigPath = "Assets/Resources/ChestOpeningConfig.asset";

    [MenuItem("Tools/Manners/VFX/Crear configuración de apertura de cofre", false, 30)]
    public static void CreateChestOpeningConfig()
    {
        ChestOpeningConfig config = AssetDatabase.LoadAssetAtPath<ChestOpeningConfig>(ConfigPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ChestOpeningConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log($"[ChestOpeningSetup] Configuración lista en {ConfigPath}. Ajusta tiempos, colores, sacudidas de cámara y SFX desde el Inspector.");
    }
}
