using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InitialsEntryUIBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/InitialsEntryUI.prefab";
    private const string FontPath = "Assets/Fonts/CyberpunkCraftpixPixel SDF.asset";

    [MenuItem("Tools/Leaderboard/Build Initials Entry UI Prefab")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject root = new GameObject("InitialsEntryUI", typeof(RectTransform), typeof(CanvasGroup));
        StretchFull(root.GetComponent<RectTransform>());

        GameObject bg = CreateUIObject("Background", root.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.92f);
        StretchFull(bg.GetComponent<RectTransform>());

        CreateAnchoredText(root.transform, "Title", "ENTER YOUR INITIALS", font, 42,
            Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 80f));

        GameObject slotsRow = CreateUIObject("InitialsRow", root.transform);
        RectTransform slotsRt = slotsRow.GetComponent<RectTransform>();
        slotsRt.anchorMin = slotsRt.anchorMax = new Vector2(0.5f, 1f);
        slotsRt.pivot = new Vector2(0.5f, 1f);
        slotsRt.anchoredPosition = new Vector2(0f, -160f);
        slotsRt.sizeDelta = new Vector2(300f, 100f);
        HorizontalLayoutGroup hlg = slotsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 24f;

        var slots = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = CreateUIObject($"Slot{i}", slotsRow.transform);
            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = 60f;
            le.preferredHeight = 90f;
            TextMeshProUGUI tmp = slot.AddComponent<TextMeshProUGUI>();
            SetupText(tmp, "_", font, 64, Color.white);
            slots[i] = tmp;
        }

        GameObject cursor = CreateUIObject("Cursor", root.transform);
        Image cursorImg = cursor.AddComponent<Image>();
        cursorImg.color = new Color(1f, 0.2f, 0.4f);
        RectTransform cursorRt = cursor.GetComponent<RectTransform>();
        cursorRt.sizeDelta = new Vector2(40f, 8f);
        cursorRt.anchorMin = cursorRt.anchorMax = new Vector2(0.5f, 1f);
        cursorRt.pivot = new Vector2(0.5f, 0.5f);

        GameObject grid = CreateUIObject("Grid", root.transform);
        RectTransform gridRt = grid.GetComponent<RectTransform>();
        gridRt.anchorMin = gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = new Vector2(0f, -60f);
        gridRt.sizeDelta = new Vector2(720f, 280f);

        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(64f, 64f);
        glg.spacing = new Vector2(8f, 8f);
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 10;

        InitialsEntryUI initialsComp = root.AddComponent<InitialsEntryUI>();

        string[] labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString())
            .Concat(new[] { "SPC", "DEL", "END" }).ToArray();

        Button firstButton = null;
        foreach (string label in labels)
        {
            GameObject btnGo = CreateUIObject($"Button_{label}", grid.transform);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.12f);
            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            GameObject labelGo = CreateUIObject("Label", btnGo.transform);
            StretchFull(labelGo.GetComponent<RectTransform>());
            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            SetupText(labelTmp, label, font, label.Length > 1 ? 16 : 28, Color.white);

            UnityEventTools.AddStringPersistentListener(btn.onClick, initialsComp.OnLetterPressed, label);

            if (label == "A") firstButton = btn;
        }

        var so = new SerializedObject(initialsComp);
        so.FindProperty("firstLetterButton").objectReferenceValue = firstButton;
        SerializedProperty slotsProp = so.FindProperty("initialSlots");
        slotsProp.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.FindProperty("cursor").objectReferenceValue = cursorRt;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[InitialsEntryUIBuilder] Prefab creado en {PrefabPath}");
    }

    [MenuItem("Tools/Leaderboard/Wire Initials Entry UI Into Open Scene")]
    public static void WireIntoOpenScene()
    {
        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>();
        if (gameOverUI == null)
        {
            Debug.LogError("[InitialsEntryUIBuilder] No se encontró GameOverUI en la escena abierta.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[InitialsEntryUIBuilder] No existe el prefab en {PrefabPath}. Corré primero 'Build Initials Entry UI Prefab'.");
            return;
        }

        var gameOverSo = new SerializedObject(gameOverUI);
        GameObject gameOverPanel = gameOverSo.FindProperty("gameOverPanel").objectReferenceValue as GameObject;
        Transform parent = gameOverPanel != null ? gameOverPanel.transform.parent : gameOverUI.transform;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.SetAsLastSibling();
        instance.SetActive(false);

        gameOverSo.FindProperty("initialsEntryPanel").objectReferenceValue = instance;
        gameOverSo.FindProperty("initialsEntryUI").objectReferenceValue = instance.GetComponent<InitialsEntryUI>();
        gameOverSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(gameOverUI.gameObject.scene);
        Debug.Log("[InitialsEntryUIBuilder] Instalado y wireado en GameOverUI. Guardá la escena (Ctrl+S / Cmd+S).");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetupText(TextMeshProUGUI tmp, string text, TMP_FontAsset font, float size, Color color)
    {
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
    }

    private static void CreateAnchoredText(Transform parent, string name, string text, TMP_FontAsset font,
        float size, Color color, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        SetupText(tmp, text, font, size, color);
    }
}
