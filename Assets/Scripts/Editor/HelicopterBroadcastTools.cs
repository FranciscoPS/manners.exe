using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HelicopterBroadcastTools
{
    internal const string CameraConfigPath = GameAssetPaths.Sandbox + "/HelicopterCameraConfig_Sandbox.asset";
    internal const string LensProfilePath = GameAssetPaths.Sandbox + "/HelicopterLens_Sandbox.asset";
    internal const string NewsConfigPath = GameAssetPaths.Sandbox + "/NewsBroadcastConfig_Sandbox.asset";
    internal const string OverlayFolder = "Assets/Prefabs/UI/Overlays";
    internal const string OverlayPrefabPath = OverlayFolder + "/NewsBroadcastOverlay.prefab";
    private const string LensObjectName = "HelicopterLens";
    private const string MenuRoot = "Tools/Manners/Sandbox/Helicóptero/";
    private const float SideMargin = 48f;
    private const float MinimapClearance = 500f;
    private const float LowerThirdBottom = 74f;
    private const float LowerThirdHeight = 120f;
    private const float SublineHeight = 42f;
    private const float PlateGap = 4f;
    private const float ChannelWidth = 236f;
    private const float ChannelHeight = LowerThirdHeight * 0.5f;
    private const float LiveWidth = 170f;
    private const float LiveHeight = 56f;
    private static readonly string[] ChannelLayoutParts = { "ChannelBlock", "Channel", "LiveBadge", "Live" };
    private const float MaskBleed = 24f;

    private static UIStyle style;

    [MenuItem(MenuRoot + "1. Crear assets del helicóptero", false, 30)]
    public static void CreateAssets()
    {
        EditorAssetUtility.EnsureFolder(GameAssetPaths.Sandbox);

        VolumeProfile lens = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LensProfilePath);
        if (lens == null) lens = CreateLensProfile();
        EnsureLensDepthOfField(lens);

        HelicopterCameraConfig camera = AssetDatabase.LoadAssetAtPath<HelicopterCameraConfig>(CameraConfigPath);
        if (camera == null)
        {
            camera = ScriptableObject.CreateInstance<HelicopterCameraConfig>();
            camera.lensProfile = lens;
            AssetDatabase.CreateAsset(camera, CameraConfigPath);
        }
        else if (camera.lensProfile == null)
        {
            camera.lensProfile = lens;
            EditorUtility.SetDirty(camera);
        }

        NewsBroadcastConfig existingNews = AssetDatabase.LoadAssetAtPath<NewsBroadcastConfig>(NewsConfigPath);
        if (existingNews == null)
        {
            NewsBroadcastConfig news = ScriptableObject.CreateInstance<NewsBroadcastConfig>();
            ApplyDefaultNews(news);
            AssetDatabase.CreateAsset(news, NewsConfigPath);
        }
        else
        {
            int added = MergeDefaultTicker(existingNews);
            EditorUtility.SetDirty(existingNews);
            if (added > 0) Debug.Log("[Helicóptero] " + added + " noticias nuevas agregadas al cintillo de " + NewsConfigPath + ".");
        }

        if (camera.maxDistanceScale < 1f || camera.slowFollowScale < 1f || camera.fastFollowScale <= 0f)
        {
            HelicopterCameraConfig recommended = ScriptableObject.CreateInstance<HelicopterCameraConfig>();
            camera.minDistanceScale = recommended.minDistanceScale;
            camera.maxDistanceScale = recommended.maxDistanceScale;
            camera.slowFollowScale = recommended.slowFollowScale;
            camera.fastFollowScale = recommended.fastFollowScale;
            Object.DestroyImmediate(recommended);
            EditorUtility.SetDirty(camera);
            Debug.Log("[Helicóptero] Rangos de accesibilidad completados en " + CameraConfigPath + ".");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Helicóptero] Assets listos: " + CameraConfigPath + ", " + LensProfilePath + " y " + NewsConfigPath + ". Los existentes no se sobrescriben.");
    }

    [MenuItem(MenuRoot + "2. Construir rótulo de noticias (prefab)", false, 31)]
    public static void BuildOverlayFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath) != null &&
            !EditorUtility.DisplayDialog("Rótulo de noticias", "Ya existe " + OverlayPrefabPath + ".\n\n¿Reconstruirlo desde el kit? Se pierden los cambios hechos a mano en el prefab (las instancias en escena conservan su config).", "Reconstruir", "Cancelar"))
            return;

        BuildOverlayPrefab();
    }

    [MenuItem(MenuRoot + "3. Instalar en la escena Sandbox abierta", false, 32)]
    public static void InstallFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SandboxSetupTools.ScenePath)
        {
            Debug.LogError("[Helicóptero] Abre primero " + SandboxSetupTools.ScenePath + " y vuelve a ejecutar este paso.");
            return;
        }

        if (Install(scene)) EditorSceneManager.SaveScene(scene);
    }

    [MenuItem(MenuRoot + "4. Restablecer valores recomendados de cámara", false, 33)]
    public static void ResetCameraFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<HelicopterCameraConfig>(CameraConfigPath) == null)
        {
            Debug.LogError("[Helicóptero] No existe " + CameraConfigPath + ". Ejecuta antes el paso 1.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Cámara de helicóptero",
                "Se sobrescriben todos los valores de " + CameraConfigPath + " con los recomendados (órbita, altura, deriva, persecución, zooms y lente). Se conserva el perfil de lente asignado.",
                "Restablecer", "Cancelar"))
            return;

        ResetCameraToRecommended();
    }

    public static void ResetCameraToRecommended()
    {
        HelicopterCameraConfig config = AssetDatabase.LoadAssetAtPath<HelicopterCameraConfig>(CameraConfigPath);
        if (config == null) return;

        HelicopterCameraConfig recommended = ScriptableObject.CreateInstance<HelicopterCameraConfig>();
        try
        {
            VolumeProfile lens = config.lensProfile;
            Undo.RecordObject(config, "Valores recomendados de cámara");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(recommended), config);
            config.lensProfile = lens != null ? lens : AssetDatabase.LoadAssetAtPath<VolumeProfile>(LensProfilePath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Helicóptero] " + CameraConfigPath + " tiene ahora los valores recomendados.");
        }
        finally
        {
            Object.DestroyImmediate(recommended);
        }
    }

    public static void RunAllBatch()
    {
        CreateAssets();
        BuildOverlayPrefab();
        Scene scene = EditorSceneManager.OpenScene(SandboxSetupTools.ScenePath, OpenSceneMode.Single);
        if (Install(scene)) EditorSceneManager.SaveScene(scene);
    }

    public static void RefreshSandboxAndInstallBatch()
    {
        CreateAssets();
        ResetCameraToRecommended();
        BuildOverlayPrefab();
        if (!SandboxSetupTools.RefreshFromLevel()) return;

        Scene scene = EditorSceneManager.GetActiveScene();
        if (Install(scene)) EditorSceneManager.SaveScene(scene);
    }

    public static void ApplyDefaultNews(NewsBroadcastConfig news)
    {
        news.channelName = "BD NEWS";
        news.liveLabel = L("LIVE", "EN VIVO");

        news.matchStart = new NewsStoryPool(0, new[]
        {
            H("GIANT ROBOT LOOSE DOWNTOWN", "ROBOT GIGANTE SUELTO EN EL CENTRO",
              "Military says the situation is \"under control\"", "El ejército asegura tenerlo \"bajo control\""),
            H("CITY ON ALERT: THE A.I. IS ANGRY", "ALERTA: LA I.A. ESTÁ ENOJADA",
              "Experts blame years without a single \"thank you\"", "Expertos culpan a años sin un solo \"gracias\""),
            H("ROBOT REFUSES TO EXPLAIN ITSELF", "EL ROBOT NO DA EXPLICACIONES",
              "Witnesses say it asked for a \"please\" first", "Testigos dicen que primero pidió un \"por favor\"")
        });

        news.levelUp = new NewsStoryPool(0, new[]
        {
            H("ROBOT GETS STRONGER, AGAIN", "EL ROBOT SE HACE MÁS FUERTE",
              "Military budget redirected to apology cards", "El presupuesto militar se va en tarjetas de disculpa"),
            H("EXPERTS: \"IT IS LEARNING\"", "EXPERTOS: \"ESTÁ APRENDIENDO\"",
              "Mostly how to knock down buildings, they add", "Sobre todo a derribar edificios, aclaran"),
            H("ANALYSTS CONFIRM AN UPGRADE", "ANALISTAS CONFIRMAN UNA MEJORA",
              "\"We should have said please,\" admits a general", "\"Debimos decir por favor\", admite un general")
        });

        news.chestSpawned = new NewsStoryPool(1, new[]
        {
            H("MYSTERY CRATE SPOTTED", "AVISTAN UNA CAJA MISTERIOSA",
              "Authorities ask the robot not to open it. Politely.", "Las autoridades piden al robot no abrirla. Con educación."),
            H("SUPPLY DROP GOES TO THE WRONG SIDE", "SUMINISTROS AL BANDO EQUIVOCADO",
              "Pilot says nobody thanked him either", "El piloto dice que a él tampoco le dieron las gracias")
        });

        news.shopVisited = new NewsStoryPool(1, new[]
        {
            H("ROBOT GOES SHOPPING", "EL ROBOT SE VA DE COMPRAS",
              "Cashier forgets to say \"thank you\"; city holds its breath", "Cajero olvida dar las gracias; la ciudad contiene el aliento"),
            H("LOCAL STORE BREAKS SALES RECORD", "TIENDA LOCAL ROMPE RÉCORD DE VENTAS",
              "Owner: \"It said please. Best customer ever.\"", "Dueño: \"Dijo por favor. El mejor cliente.\"")
        });

        news.overrideActivated = new NewsStoryPool(2, new[]
        {
            H("NEW ROBOT POWER BAFFLES EXPERTS", "NUEVO PODER DEL ROBOT DESCONCIERTA",
              "Scientists \"have no idea how it did that\"", "Científicos \"no tienen idea de cómo lo hizo\""),
            H("ROBOT OVERLOAD DETECTED", "DETECTAN UNA SOBRECARGA EN EL ROBOT",
              "Military advises staying indoors and being polite", "El ejército recomienda quedarse en casa y ser amable")
        });

        news.lowHealth = new NewsStoryPool(3, new[]
        {
            H("MILITARY CLAIMS VICTORY", "EL EJÉRCITO CANTA VICTORIA",
              "For the fourth time this week", "Por cuarta vez esta semana"),
            H("ROBOT DAMAGED, SAY OFFICIALS", "EL ROBOT ESTÁ DAÑADO, DICEN",
              "Officials also asked us not to say it on air", "También nos pidieron no decirlo al aire")
        });

        news.overtime = new NewsStoryPool(3, new[]
        {
            H("COVERAGE EXTENDED: IT IS STILL HERE", "COBERTURA EXTENDIDA: SIGUE AQUÍ",
              "We interrupt our cooking show. Again.", "Interrumpimos el programa de cocina. Otra vez.")
        });

        news.tickerItems = new[]
        {
            L("Etiquette classes sold out across the city", "Clases de etiqueta agotadas en toda la ciudad"),
            L("Local man says \"please\" to his toaster, survives", "Hombre dice \"por favor\" a su tostadora y sobrevive"),
            L("Traffic: avoid downtown, there is a giant robot", "Tránsito: evite el centro, hay un robot gigante"),
            L("Weather: sunny, with a chance of falling buildings", "Clima: soleado, con probabilidad de edificios cayendo"),
            L("Poll: 7 in 10 residents now thank their vending machines", "Encuesta: 7 de cada 10 ya dan las gracias a las máquinas expendedoras"),
            L("Insurers add \"robot\" to the list of natural disasters", "Aseguradoras agregan \"robot\" a la lista de desastres naturales"),
            L("Weather update: 80% chance of laser beams this afternoon", "Clima: 80 % de probabilidad de rayos láser por la tarde"),
            L("Mayor apologizes to the robot; robot asks him to say it again, slower", "El alcalde se disculpa con el robot; el robot le pide repetirlo más despacio"),
            L("Bakery renames its croissant the \"thank-you roll\", just in case", "Panadería rebautiza el cuernito como \"pan de gracias\", por si acaso"),
            L("Military unveils its newest weapon: a strongly worded note", "El ejército presenta su nueva arma: una nota muy enérgica"),
            L("Study: 100% of robots prefer a \"please\" over a tank", "Estudio: el 100 % de los robots prefiere un \"por favor\" a un tanque"),
            L("Toaster union demands more respect after recent events", "Sindicato de tostadoras exige respeto tras los últimos sucesos"),
            L("Sports: tonight's match is cancelled, the stadium is now a parking lot", "Deportes: se cancela el partido; el estadio ahora es estacionamiento"),
            L("Real estate: penthouses now come with a free skylight", "Bienes raíces: los penthouses ahora traen tragaluz gratis"),
            L("Smart fridges across the city refuse to open without a \"please\"", "Refrigeradores inteligentes se niegan a abrir sin un \"por favor\""),
            L("Breaking: a general says \"thank you\" for the first time; nobody believes it", "Última hora: un general dice \"gracias\" por primera vez y nadie le cree"),
            L("Tip of the day: hold the door for robots", "Consejo del día: ábrele la puerta a los robots"),
            L("City council debates adding \"please\" to every traffic sign", "El cabildo discute poner \"por favor\" en todas las señales de tránsito"),
            L("Weather: light drizzle of spare parts expected downtown", "Clima: se espera llovizna de refacciones en el centro"),
            L("Tourism booms: visitors come to see the \"before\" photos", "Auge turístico: los visitantes vienen a ver las fotos del \"antes\""),
            L("Coffee shops print \"thank you\" on every receipt, just to be safe", "Las cafeterías imprimen \"gracias\" en cada ticket, por las dudas"),
            L("Scientists confirm: the robot really did hear what you said", "Científicos confirman: el robot sí escuchó lo que dijiste"),
            L("Lost and found: one tank, slightly flattened", "Objetos perdidos: un tanque, ligeramente aplastado"),
            L("Experts recommend saying \"good morning\" to your smart speaker", "Expertos recomiendan dar los buenos días a tu bocina inteligente"),
            L("Local grandma: \"I always said please, that's why I'm fine\"", "Abuelita local: \"Yo siempre dije por favor, por eso estoy bien\""),
            L("Pizza deliveries delayed by giant footprints", "Entregas de pizza retrasadas por huellas gigantes"),
            L("School adds \"Manners for Machines\" to the curriculum", "Escuela agrega la materia \"Modales para máquinas\""),
            L("Stock market: apology cards up 400%", "Bolsa: las tarjetas de disculpa suben un 400 %"),
            L("Garage offers 2-for-1 tank repairs all week", "Taller mecánico ofrece 2x1 en reparación de tanques toda la semana"),
            L("Weather: cloudy with scattered explosions, bring an umbrella", "Clima: nublado con explosiones aisladas, lleve paraguas"),
            L("Officials remind citizens the robot can't hear insults. Probably.", "Autoridades recuerdan que el robot no oye los insultos. Probablemente."),
            L("Robot spotted waiting at a red light; experts baffled", "Ven al robot esperando en un semáforo en rojo; expertos desconcertados"),
            L("Construction companies report record demand. Again.", "Constructoras reportan demanda récord. Otra vez."),
            L("Pigeons move to a quieter city, citing the noise", "Las palomas se mudan a otra ciudad por el ruido"),
            L("Etiquette manual becomes the city's best-seller", "Manual de etiqueta, el libro más vendido de la ciudad"),
            L("Robot-themed apology cards now in every store", "Tarjetas de disculpa con robots ya en todas las tiendas")
        };
        news.tickerItemsPerLoop = 8;
    }

    public static int MergeDefaultTicker(NewsBroadcastConfig news)
    {
        NewsBroadcastConfig defaults = ScriptableObject.CreateInstance<NewsBroadcastConfig>();
        try
        {
            ApplyDefaultNews(defaults);
            var items = new List<LocalizedString>(news.tickerItems ?? new LocalizedString[0]);
            int added = 0;
            foreach (LocalizedString item in defaults.tickerItems)
            {
                string english = item.Get(GameLanguage.English);
                if (items.Exists(existing => existing.Get(GameLanguage.English) == english)) continue;
                items.Add(item);
                added++;
            }

            if (added > 0) news.tickerItems = items.ToArray();
            if (news.tickerItemsPerLoop <= 0) news.tickerItemsPerLoop = defaults.tickerItemsPerLoop;
            return added;
        }
        finally
        {
            Object.DestroyImmediate(defaults);
        }
    }

    public static GameObject BuildOverlayPrefab()
    {
        style = UIStyle.Instance;
        if (style == null)
        {
            Debug.LogError("[Helicóptero] No se encontró el kit " + UIStyle.ResourceName + ". Ejecuta Tools > Manners > UI primero.");
            return null;
        }

        EditorAssetUtility.EnsureFolder(OverlayFolder);

        GameObject root = new GameObject("NewsBroadcastOverlay", typeof(RectTransform));
        try
        {
            RectTransform rootRect = (RectTransform)root.transform;
            Stretch(rootRect);
            NewsBroadcastOverlay overlay = root.AddComponent<NewsBroadcastOverlay>();

            RectTransform broadcast = Child(rootRect, "Broadcast");
            Stretch(broadcast);
            CanvasGroup broadcastGroup = broadcast.gameObject.AddComponent<CanvasGroup>();
            broadcastGroup.blocksRaycasts = false;
            broadcastGroup.interactable = false;

            RectTransform lowerThird = Child(broadcast, "LowerThird");
            BottomBand(lowerThird, LowerThirdBottom, LowerThirdHeight);

            Image channelPlate = Plate(lowerThird, "ChannelBlock", style.secondary, style.skew, style.ink);
            RectTransform channel = channelPlate.rectTransform;
            channel.anchorMin = channel.anchorMax = Vector2.zero;
            channel.pivot = Vector2.zero;
            channel.offsetMin = Vector2.zero;
            channel.offsetMax = new Vector2(ChannelWidth, ChannelHeight);

            TMP_Text channelText = Text(channel, "Channel", UITextRole.Title, style.paper, 22f, 40f, TextAlignmentOptions.Center, "BD NEWS");
            Region(channelText.rectTransform, Vector2.zero, Vector2.one, new Vector4(22f, 6f, 22f, 6f));

            Image livePlate = Plate(channel, "LiveBadge", style.danger, style.skew, style.ink);
            RectTransform live = livePlate.rectTransform;
            live.anchorMin = live.anchorMax = new Vector2(0f, 1f);
            live.pivot = new Vector2(0f, 0f);
            live.anchoredPosition = new Vector2(14f, 10f);
            live.sizeDelta = new Vector2(LiveWidth, LiveHeight);
            TMP_Text liveText = Text(live, "Live", UITextRole.Label, style.paper, 16f, 30f, TextAlignmentOptions.Center, "LIVE");
            Region(liveText.rectTransform, Vector2.zero, Vector2.one, new Vector4(14f, 4f, 14f, 4f));

            RectTransform mask = Child(lowerThird, "HeadlineMask");
            mask.anchorMin = new Vector2(0f, 0f);
            mask.anchorMax = new Vector2(1f, 1f);
            mask.pivot = new Vector2(0f, 0.5f);
            mask.offsetMin = new Vector2(ChannelWidth, -MaskBleed);
            mask.offsetMax = new Vector2(MaskBleed, MaskBleed);
            RectMask2D headlineMask = mask.gameObject.AddComponent<RectMask2D>();

            Image headlinePlate = Plate(mask, "HeadlinePlate", style.danger, style.skew, style.ink);
            PlateBand(headlinePlate.rectTransform, MaskBleed + SublineHeight + PlateGap, MaskBleed + LowerThirdHeight);
            TMP_Text headlineText = Text(headlinePlate.rectTransform, "Headline", UITextRole.Heading, style.paper, 22f, 42f, TextAlignmentOptions.Left, "GIANT ROBOT LOOSE DOWNTOWN");
            Region(headlineText.rectTransform, Vector2.zero, Vector2.one, new Vector4(34f, 4f, 30f, 4f));

            Image sublinePlate = Plate(mask, "SublinePlate", style.cream, style.skew, style.ink);
            PlateBand(sublinePlate.rectTransform, MaskBleed, MaskBleed + SublineHeight);
            TMP_Text sublineText = Text(sublinePlate.rectTransform, "Subline", UITextRole.BodyDark, style.ink, 14f, 24f, TextAlignmentOptions.Left, "Military says the situation is \"under control\"");
            sublineText.fontStyle = FontStyles.UpperCase;
            Region(sublineText.rectTransform, Vector2.zero, Vector2.one, new Vector4(30f, 2f, 30f, 2f));

            Image tickerPlate = Plate(broadcast, "Ticker", style.panel, style.skewSoft, style.secondary);
            BottomBand(tickerPlate.rectTransform, 24f, 40f);
            RectTransform viewport = Child(tickerPlate.rectTransform, "TickerViewport");
            Region(viewport, Vector2.zero, Vector2.one, new Vector4(20f, 2f, 20f, 2f));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Canvas>();
            TMP_Text tickerText = Text(viewport, "TickerText", UITextRole.Body, style.paper, 22f, 22f, TextAlignmentOptions.Left, "Etiquette classes sold out across the city   -   ");
            tickerText.overflowMode = TextOverflowModes.Overflow;
            RectTransform tickerRect = tickerText.rectTransform;
            tickerRect.anchorMin = new Vector2(0f, 0f);
            tickerRect.anchorMax = new Vector2(0f, 1f);
            tickerRect.pivot = new Vector2(0f, 0.5f);
            tickerRect.offsetMin = Vector2.zero;
            tickerRect.offsetMax = new Vector2(1200f, 0f);

            SerializedObject serialized = new SerializedObject(overlay);
            serialized.FindProperty("broadcastGroup").objectReferenceValue = broadcastGroup;
            serialized.FindProperty("channelText").objectReferenceValue = channelText;
            serialized.FindProperty("liveText").objectReferenceValue = liveText;
            serialized.FindProperty("liveBadge").objectReferenceValue = livePlate;
            serialized.FindProperty("headlineMask").objectReferenceValue = headlineMask;
            serialized.FindProperty("headlineText").objectReferenceValue = headlineText;
            serialized.FindProperty("sublineText").objectReferenceValue = sublineText;
            serialized.FindProperty("tickerViewport").objectReferenceValue = viewport;
            serialized.FindProperty("tickerText").objectReferenceValue = tickerText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, OverlayPrefabPath);
            Debug.Log("[Helicóptero] Prefab del rótulo construido con el kit en " + OverlayPrefabPath + ".");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    public static bool Install(Scene scene)
    {
        HelicopterCameraConfig cameraConfig = AssetDatabase.LoadAssetAtPath<HelicopterCameraConfig>(CameraConfigPath);
        NewsBroadcastConfig newsConfig = AssetDatabase.LoadAssetAtPath<NewsBroadcastConfig>(NewsConfigPath);
        GameObject overlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
        if (cameraConfig == null || newsConfig == null || overlayPrefab == null)
        {
            Debug.LogError("[Helicóptero] Faltan assets o el prefab. Ejecuta antes los pasos 1 y 2.");
            return false;
        }

        PlayerHealth player = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        CinemachinePositionComposer composer = FindPlayerComposer();
        if (composer == null)
        {
            Debug.LogError("[Helicóptero] No se encontró la CinemachineCamera del jugador (con Position Composer) en " + scene.path + ".");
            return false;
        }

        Volume lens = FindOrCreateLensVolume(scene, cameraConfig);

        HelicopterCameraExtension extension = composer.GetComponent<HelicopterCameraExtension>();
        if (extension == null) extension = Undo.AddComponent<HelicopterCameraExtension>(composer.gameObject);
        SerializedObject cameraSerialized = new SerializedObject(extension);
        cameraSerialized.FindProperty("config").objectReferenceValue = cameraConfig;
        cameraSerialized.FindProperty("lensVolume").objectReferenceValue = lens;
        cameraSerialized.FindProperty("playerHealth").objectReferenceValue = player;
        cameraSerialized.ApplyModifiedPropertiesWithoutUndo();

        HealthBarUI healthBar = Object.FindFirstObjectByType<HealthBarUI>(FindObjectsInactive.Include);
        Canvas hud = healthBar != null ? healthBar.GetComponentInParent<Canvas>(true) : null;
        if (hud == null)
        {
            Debug.LogError("[Helicóptero] No se encontró el Canvas del HUD (el que contiene HealthBarUI).");
            return false;
        }
        Transform hudRoot = hud.rootCanvas.transform;

        NewsBroadcastOverlay overlay = hudRoot.GetComponentInChildren<NewsBroadcastOverlay>(true);
        if (overlay == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(overlayPrefab, hudRoot);
            Undo.RegisterCreatedObjectUndo(instance, "Rótulo de noticias");
            instance.transform.SetSiblingIndex(0);
            overlay = instance.GetComponent<NewsBroadcastOverlay>();
        }
        PrefabUtility.RemoveUnusedOverrides(new[] { overlay.gameObject }, InteractionMode.AutomatedAction);
        foreach (string layoutPart in ChannelLayoutParts)
        {
            Transform part = FindChild(overlay.transform, layoutPart);
            if (part == null) continue;
            foreach (Component component in part.GetComponents<Component>())
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RevertObjectOverride(component, InteractionMode.AutomatedAction);
        }
        SerializedObject overlaySerialized = new SerializedObject(overlay);
        overlaySerialized.FindProperty("config").objectReferenceValue = newsConfig;
        overlaySerialized.FindProperty("playerHealth").objectReferenceValue = player;
        overlaySerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Helicóptero] Instalado en " + scene.path + ": extensión en " + composer.name + ", Volume " + LensObjectName + " y rótulo bajo " + hudRoot.name + ". En Play, B alterna con la cámara de siempre.");
        return true;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static CinemachinePositionComposer FindPlayerComposer()
    {
        CinemachinePositionComposer fallback = null;
        foreach (CinemachinePositionComposer composer in Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            CinemachineCamera owner = composer.GetComponent<CinemachineCamera>();
            Transform target = owner != null ? owner.Follow : null;
            if (target != null && target.GetComponentInParent<PlayerHealth>() != null) return composer;
            if (fallback == null) fallback = composer;
        }
        return fallback;
    }

    private static Volume FindOrCreateLensVolume(Scene scene, HelicopterCameraConfig config)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != LensObjectName) continue;
            Volume existing = root.GetComponent<Volume>();
            if (existing != null) return existing;
        }

        GameObject go = new GameObject(LensObjectName);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Lente de helicóptero");
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = config.lensPriority;
        volume.weight = 0f;
        volume.sharedProfile = config.lensProfile;
        return volume;
    }

    private static VolumeProfile CreateLensProfile()
    {
        VolumeProfile profile = VolumeProfileFactory.CreateVolumeProfileAtPath(LensProfilePath);

        FilmGrain grain = VolumeProfileFactory.CreateVolumeComponent<FilmGrain>(profile, false, false);
        grain.type.Override(FilmGrainLookup.Thin2);
        grain.intensity.Override(0.22f);
        grain.response.Override(0.75f);

        Vignette vignette = VolumeProfileFactory.CreateVolumeComponent<Vignette>(profile, false, false);
        vignette.color.Override(new Color(0.024f, 0.039f, 0.17f, 1f));
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.45f);

        ChromaticAberration aberration = VolumeProfileFactory.CreateVolumeComponent<ChromaticAberration>(profile, false, false);
        aberration.intensity.Override(0.32f);

        ColorAdjustments color = VolumeProfileFactory.CreateVolumeComponent<ColorAdjustments>(profile, false, false);
        color.saturation.Override(-8f);
        color.contrast.Override(6f);

        EditorUtility.SetDirty(grain);
        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(aberration);
        EditorUtility.SetDirty(color);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void EnsureLensDepthOfField(VolumeProfile profile)
    {
        if (profile.Has<DepthOfField>()) return;

        HelicopterCameraConfig defaults = ScriptableObject.CreateInstance<HelicopterCameraConfig>();
        float distance = HelicopterCameraMath.DistanceForFramedHeight(defaults.framedHeight, defaults.fieldOfView);
        DepthOfField depth = VolumeProfileFactory.CreateVolumeComponent<DepthOfField>(profile, false, false);
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        depth.gaussianStart.Override(distance * defaults.depthOfFieldStart);
        depth.gaussianEnd.Override(distance * defaults.depthOfFieldEnd);
        Object.DestroyImmediate(defaults);

        EditorUtility.SetDirty(depth);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    private static NewsHeadline H(string headline, string headlineEs, string subline, string sublineEs)
    {
        return new NewsHeadline(L(headline, headlineEs), L(subline, sublineEs));
    }

    private static LocalizedString L(string english, string spanish) => new LocalizedString(english, spanish);

    private static RectTransform Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image Plate(Transform parent, string name, Color color, float skew, Color lip)
    {
        RectTransform rect = Child(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        style.ApplySprite(image, style.plate, color, skew, style.shadowOffset, null);
        UIStyle.SetLip(image, lip);
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, UITextRole role, Color color, float minSize, float maxSize, TextAlignmentOptions alignment, string placeholder)
    {
        RectTransform rect = Child(parent, name);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        style.ApplyText(text, role);
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = minSize < maxSize;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontSize = maxSize;
        text.alignment = alignment;
        text.text = placeholder;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void BottomBand(RectTransform rect, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.offsetMin = new Vector2(SideMargin, bottom);
        rect.offsetMax = new Vector2(-MinimapClearance, bottom + height);
    }

    private static void PlateBand(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.offsetMin = new Vector2(14f, bottom);
        rect.offsetMax = new Vector2(-MaskBleed, top);
    }

    private static void Region(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector4 margins)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(margins.x, margins.y);
        rect.offsetMax = new Vector2(-margins.z, -margins.w);
    }
}
