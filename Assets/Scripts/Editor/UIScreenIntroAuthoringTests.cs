using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UIScreenIntroAuthoringTests
{
    private static readonly string[] Prefabs =
    {
        "Assets/Prefabs/UI/Settings/GraphicsSettingsPanel.prefab",
        "Assets/Prefabs/UI/InitialsEntryUI.prefab",
        "Assets/Prefabs/UI/Notifications/ChestAnnouncement.prefab",
        "Assets/Prefabs/UI/Notifications/OvertimeAlert.prefab",
        "Assets/Prefabs/UI/Chest/ChestOpeningSequence.prefab",
        "Assets/Prefabs/UI/Overrides/OverrideActivationHUD.prefab"
    };

    [Test]
    public void IntroAnimationsTargetAuthoredContentWithoutTweeningResponsiveFrames()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        int checkedIntros = 0;
        try
        {
            foreach (string path in Prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try { checkedIntros += AssertBlocks(root.GetComponentsInChildren<UIScreenIntro>(true), path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in UIAuthoringTools.Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                checkedIntros += AssertBlocks(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UIScreenIntro>(true)).ToArray(), path);
            }
            Assert.Greater(checkedIntros, 10);
        }
        finally
        {
            if (setup.Any(item => item.isLoaded && !string.IsNullOrEmpty(item.path)) && setup.Count(item => item.isActive) == 1 && setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static int AssertBlocks(UIScreenIntro[] intros, string path)
    {
        int checkedIntros = 0;
        foreach (UIScreenIntro intro in intros)
        {
            Transform frame = intro.transform.Find("ContentFrame");
            if (frame == null) continue;
            checkedIntros++;
            SerializedProperty blocks = new SerializedObject(intro).FindProperty("blocks");
            Assert.Greater(blocks.arraySize, 0, path + " / " + intro.name + ": la animación debe apuntar a los componentes reales.");
            for (int i = 0; i < blocks.arraySize; i++)
            {
                RectTransform block = blocks.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
                string location = path + " / " + intro.name + " / " + i;
                Assert.IsNotNull(block, location);
                Assert.IsTrue(block.IsChildOf(frame), location + ": el bloque debe pertenecer a su composición.");
                Assert.AreNotSame(frame, block, location + ": el marco no debe recibir tweens.");
                Assert.IsNull(block.GetComponent<AspectRatioFitter>(), location + ": el tween compite con el ajuste de proporción.");
            }
        }
        return checkedIntros;
    }
}
