using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class MissingScriptDetector
{
    static MissingScriptDetector()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        CheckForMissingScripts();
    }

    [MenuItem("Tools/Check Missing Scripts Now")]
    private static void CheckForMissingScripts()
    {
        int missingCount = 0;
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.LogWarning($"Missing script on GameObject: {GetGameObjectPath(go)}", go);
                }
            }
        }

        if (missingCount > 0)
        {
            Debug.LogError($"Found {missingCount} missing script(s) in the scene! Use Tools > Missing Script Cleaner to fix them.");
        }
        else
        {
            Debug.Log("No missing scripts found. Scene is clean!");
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
