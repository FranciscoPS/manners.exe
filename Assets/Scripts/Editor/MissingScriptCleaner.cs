using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class MissingScriptCleaner : EditorWindow
{
    private int missingCount = 0;
    private string reportText = "Click 'Find Missing Scripts' to scan the scene.";
    private Vector2 scrollPosition;

    private bool pendingFind = false;
    private bool pendingRemove = false;

    [MenuItem("Tools/Missing Script Cleaner")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptCleaner>("Missing Script Cleaner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Script Cleaner", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This tool finds and removes GameObjects with missing script references in the active scene.", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Find Missing Scripts", GUILayout.Height(30)))
        {
            pendingFind = true;
        }

        GUILayout.Space(10);

        if (missingCount > 0)
        {
            EditorGUILayout.HelpBox($"Found {missingCount} missing script(s)!", MessageType.Warning);

            if (GUILayout.Button("Remove All Missing Scripts", GUILayout.Height(30)))
            {
                pendingRemove = true;
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Report:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // Run heavy work / modal dialogs OUTSIDE the layout Begin/End region.
        // Doing this inside a button callback can interrupt IMGUI mid-pass and
        // leave the GUIClip stack unbalanced ("pushing more GUIClips than popping").
        if (Event.current.type == EventType.Repaint && (pendingFind || pendingRemove))
        {
            bool doFind = pendingFind;
            bool doRemove = pendingRemove;
            pendingFind = false;
            pendingRemove = false;

            if (doFind)
            {
                FindMissingScripts();
            }
            else if (doRemove)
            {
                RemoveMissingScripts();
            }

            GUIUtility.ExitGUI();
        }
    }

    private void FindMissingScripts()
    {
        missingCount = 0;
        reportText = "";

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        reportText += $"Scanning scene: {activeScene.name}\n";
        reportText += "=====================================\n\n";

        foreach (GameObject rootObj in rootObjects)
        {
            FindMissingInGameObject(rootObj);
        }

        if (missingCount == 0)
        {
            reportText += "\n✓ No missing scripts found! Scene is clean.";
        }
        else
        {
            reportText += $"\n⚠ Total missing scripts: {missingCount}";
        }

        Repaint();
    }

    private void FindMissingInGameObject(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                missingCount++;
                string path = GetGameObjectPath(go);
                reportText += $"[{missingCount}] Missing script on: {path}\n";
            }
        }

        foreach (Transform child in go.transform)
        {
            FindMissingInGameObject(child.gameObject);
        }
    }

    private void RemoveMissingScripts()
    {
        if (missingCount == 0)
        {
            EditorUtility.DisplayDialog("Nothing to Remove", "No missing scripts found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Removal",
            $"Are you sure you want to remove {missingCount} missing script(s)?\n\nThis action cannot be undone.",
            "Yes, Remove", "Cancel"))
        {
            return;
        }

        int removedCount = 0;
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject rootObj in rootObjects)
        {
            removedCount += RemoveMissingInGameObject(rootObj);
        }

        if (rootObjects.Length > 0)
        {
            EditorUtility.SetDirty(rootObjects[0]);
        }
        EditorSceneManager.MarkSceneDirty(activeScene);

        reportText = $"✓ Successfully removed {removedCount} missing script(s)!\n\n";
        reportText += "Scene has been marked as dirty. Don't forget to save the scene.";
        missingCount = 0;

        Repaint();

        EditorUtility.DisplayDialog("Cleanup Complete",
            $"Removed {removedCount} missing script references.\n\nPlease save the scene.",
            "OK");
    }

    private int RemoveMissingInGameObject(GameObject go)
    {
        int removedCount = 0;
        Component[] components = go.GetComponents<Component>();

        SerializedObject so = new SerializedObject(go);
        SerializedProperty prop = so.FindProperty("m_Component");

        int propertyIndex = 0;
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                prop.DeleteArrayElementAtIndex(propertyIndex);
                removedCount++;
            }
            else
            {
                propertyIndex++;
            }
        }

        so.ApplyModifiedProperties();

        foreach (Transform child in go.transform)
        {
            removedCount += RemoveMissingInGameObject(child.gameObject);
        }

        return removedCount;
    }

    private string GetGameObjectPath(GameObject obj)
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
