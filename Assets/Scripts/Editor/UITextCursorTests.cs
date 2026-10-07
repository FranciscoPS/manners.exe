using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class UITextCursorTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float PositionTolerance = .25f;

    [UnityTest]
    public IEnumerator AuthoredMenuCursorsStayAfterLastGlyphAcrossLanguagesAndViewports()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        GameObject cameraObject = null;
        RenderTexture texture = null;
        try
        {
            var scene = EditorSceneManager.OpenScene(UIAuthoringTools.Scenes[0], OpenSceneMode.Single);
            Canvas canvas = scene.GetRootGameObjects().First(root => root.name == "Canvas").GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            UITextCursor[] cursors = canvas.GetComponentsInChildren<UITextCursor>(true);
            Assert.GreaterOrEqual(cursors.Length, 8);
            foreach (UITextCursor cursor in cursors)
            {
                ActivateAncestors(cursor.transform, canvas.transform);
                TMP_Text target = (TMP_Text)typeof(UITextCursor).GetField("target", Fields).GetValue(cursor);
                Assert.IsNotNull(target, cursor.name);
                ActivateAncestors(target.transform, canvas.transform);
            }
            cameraObject = new GameObject("Cursor viewport test camera") { hideFlags = HideFlags.HideAndDontSave };
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(12000f, -20000f, 7000f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 50f;

            foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(3840, 1080), new Vector2Int(1440, 1080) })
            {
                if (texture != null) { camera.targetTexture = null; texture.Release(); Object.DestroyImmediate(texture); }
                texture = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = texture;
                yield return null;
                Assert.AreEqual(size.x / (float)size.y, canvas.pixelRect.width / canvas.pixelRect.height, .001f);
                foreach (GameLanguage language in new[] { GameLanguage.English, GameLanguage.Spanish, GameLanguage.English, GameLanguage.Spanish })
                {
                    foreach (LocalizedText text in canvas.GetComponentsInChildren<LocalizedText>()) text.ApplyLanguage(language);
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    Canvas.ForceUpdateCanvases();
                    foreach (UITextCursor cursor in cursors) AssertAfterLastGlyph(cursor, size, language);
                }
            }
        }
        finally
        {
            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
            if (texture != null) { texture.Release(); Object.DestroyImmediate(texture); }
            if (setup.Any(item => item.isLoaded && !string.IsNullOrEmpty(item.path)) && setup.Count(item => item.isActive) == 1 && setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static void ActivateAncestors(Transform target, Transform root)
    {
        while (target != null)
        {
            target.gameObject.SetActive(true);
            if (target == root) return;
            target = target.parent;
        }
    }

    private static void AssertAfterLastGlyph(UITextCursor cursor, Vector2Int size, GameLanguage language)
    {
        TMP_Text target = (TMP_Text)typeof(UITextCursor).GetField("target", Fields).GetValue(cursor);
        Assert.IsNotNull(target, cursor.name);
        TMP_TextInfo info = target.textInfo;
        int last = info.characterCount - 1;
        while (last >= 0 && !info.characterInfo[last].isVisible) last--;
        Assert.GreaterOrEqual(last, 0, target.name);
        TMP_CharacterInfo character = info.characterInfo[last];
        float gap = (float)typeof(UITextCursor).GetField("gap", Fields).GetValue(cursor);
        float right = Mathf.Max(character.xAdvance, Mathf.Max(character.topRight.x, character.bottomRight.x));
        var corners = new Vector3[4];
        ((RectTransform)cursor.transform).GetWorldCorners(corners);
        Vector3 left = target.transform.InverseTransformPoint(corners[0]);
        string context = $"{target.name}: {language}, {size.x}x{size.y}";
        Assert.AreEqual(right + gap, left.x, PositionTolerance, context);
        Assert.AreEqual(character.baseLine, left.y, PositionTolerance, context);
        Assert.GreaterOrEqual(left.x, Mathf.Max(character.topRight.x, character.bottomRight.x) + gap - PositionTolerance, context);
    }
}
