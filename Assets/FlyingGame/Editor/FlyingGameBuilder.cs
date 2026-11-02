using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一键生成「星际穿梭」飞行躲避游戏场景（编辑器工具）。
/// </summary>
public static class FlyingGameBuilder
{
    const string Root = "Assets/FlyingGame";
    const string ScenePath = Root + "/Scenes/FlyingGame.unity";
    const string MatDir = Root + "/Materials";
    const string PrefabDir = Root + "/Prefabs";

    static Font uiFont;
    static Font uiFontBold;

    [MenuItem("Tools/星际穿梭/构建游戏场景")]
    public static void Build()
    {
        EnsureFolders();
        uiFont = LoadFont("NotoSansSC-Regular.otf");
        uiFontBold = LoadFont("NotoSansSC-Bold.otf");
        if (uiFontBold == null) uiFontBold = uiFont;
        if (uiFont == null) uiFont = uiFontBold;
        if (uiFont == null)
        {
            Debug.LogError("[FlyingGameBuilder] 未找到中文字体，请确认 Assets/FlyingGame/Fonts 下有 NotoSansSC 字体。");
            return;
        }

        Material hull = MakeMat("M_Hull", new Color(0.88f, 0.92f, 0.97f), 0.62f, 0.78f, Color.black);
        Material accent = MakeMat("M_Accent", new Color(1f, 0.40f, 0.09f), 0.20f, 0.58f, new Color(0.14f, 0.035f, 0.005f));
        Material dark = MakeMat("M_Dark", new Color(0.07f, 0.09f, 0.12f), 0.55f, 0.45f, Color.black);
        Material mid = MakeMat("M_Mid", new Color(0.34f, 0.40f, 0.48f), 0.72f, 0.82f, Color.black);
        Material rockA = MakeMat("M_RockA", new Color(0.47f, 0.48f, 0.51f), 0.12f, 0.24f, Color.black, true);
        Material rockB = MakeMat("M_RockB", new Color(0.33f, 0.34f, 0.37f), 0.12f, 0.20f, Color.black, true);
        Material glow = MakeMat("M_Glow", new Color(1f, 0.55f, 0.18f), 0f, 0.5f, new Color(2.6f, 0.85f, 0.16f));

        Texture2D dot = MakeDotTexture();
        Material starMat = MakeParticleMat("M_Star", dot, new Color(0.55f, 0.78f, 1f, 1f));
        Material trailMat = MakeParticleMat("M_Trail", dot, new Color(0.45f, 0.85f, 1f, 1f));

        // ---------- 场景 ----------
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.35f, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.07f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 120f;

        // ---------- 灯光 ----------
        GameObject lightGO = new GameObject("Directional Light");
        Light dl = lightGO.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(1f, 0.96f, 0.88f);
        dl.intensity = 1.15f;
        dl.shadows = LightShadows.Soft;
        dl.shadowStrength = 0.55f;
        lightGO.transform.rotation = Quaternion.Euler(32f, -34f, 0f);

        GameObject fillGO = new GameObject("Fill Light");
        Light fl = fillGO.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.35f, 0.55f, 1f);
        fl.intensity = 0.45f;
        fillGO.transform.rotation = Quaternion.Euler(140f, 150f, 0f);

        // ---------- 摄像机 ----------
        GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.012f, 0.016f, 0.035f);
        cam.fieldOfView = 64f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        camGO.transform.position = new Vector3(0f, 2.35f, -9.2f);

        // ---------- 星空 ----------
        CreateStarfield(starMat, new Vector3(0f, 2f, 58f), new Vector3(110f, 150f, 12f), 850f, 3.2f, 0.24f, 0.78f, new Vector2(-15f, -26f), 3400);

        // ---------- 玩家 ----------
        GameObject player = new GameObject("Player", typeof(PlayerController));
        player.transform.position = new Vector3(0f, 0.7f, 0f);

        GameObject pivot = new GameObject("ShipPivot");
        pivot.transform.SetParent(player.transform, false);

        GameObject shipModel = InstantiateModel("craft_speederA", pivot.transform, new Material[] { hull, accent, dark, mid });
        if (shipModel != null) shipModel.transform.localScale = Vector3.one * 1.45f;

        // 引擎光点
        GameObject glowGO = new GameObject("EngineGlow");
        glowGO.transform.SetParent(pivot.transform, false);
        glowGO.transform.localPosition = new Vector3(0f, 0.08f, -1.25f);
        Light eng = glowGO.AddComponent<Light>();
        eng.type = LightType.Point;
        eng.color = new Color(1f, 0.55f, 0.2f);
        eng.intensity = 3.2f;
        eng.range = 7f;
        eng.shadows = LightShadows.None;

        // 尾迹
        GameObject trailGO = new GameObject("Trail", typeof(TrailRenderer));
        trailGO.transform.SetParent(pivot.transform, false);
        trailGO.transform.localPosition = new Vector3(0f, 0f, -1.35f);
        TrailRenderer tr = trailGO.GetComponent<TrailRenderer>();
        tr.time = 0.32f;
        tr.startWidth = 0.42f;
        tr.endWidth = 0.02f;
        tr.sharedMaterial = trailMat;
        tr.startColor = new Color(0.55f, 0.85f, 1f, 0.85f);
        tr.endColor = new Color(0.2f, 0.5f, 1f, 0f);
        tr.numCapVertices = 2;
        tr.minVertexDistance = 0.08f;

        PlayerController pc = player.GetComponent<PlayerController>();
        pc.modelRoot = pivot.transform;
        pc.menuScale = 1.26f;
        pc.playScale = 1f;
        pc.engineGlow = glowGO;

        // ---------- 陨石预制体 ----------
        List<GameObject> rockPrefabs = new List<GameObject>();
        rockPrefabs.Add(MakeRockPrefab("rock_largeA", new Material[] { rockA, rockB }, 0.58f));
        rockPrefabs.Add(MakeRockPrefab("rock_largeB", new Material[] { rockA, rockB }, 0.58f));
        rockPrefabs.Add(MakeRockPrefab("meteor_detailed", new Material[] { rockA }, 0.64f));
        rockPrefabs.Add(MakeRockPrefab("meteor", new Material[] { rockB }, 0.52f));
        rockPrefabs.Add(MakeRockPrefab("rock_crystalsLargeA", new Material[] { rockA, rockB }, 0.56f));

        GameObject spawnerGO = new GameObject("ObstacleSpawner", typeof(ObstacleSpawner));
        ObstacleSpawner sp = spawnerGO.GetComponent<ObstacleSpawner>();
        sp.rockPrefabs = rockPrefabs.ToArray();

        // ---------- 音频 ----------
        GameObject audioGO = new GameObject("Audio");
        AudioSource flySrc = audioGO.AddComponent<AudioSource>();
        flySrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/fly_engine.ogg");
        flySrc.loop = true;
        flySrc.playOnAwake = false;
        flySrc.volume = 0f;
        flySrc.spatialBlend = 0f;

        AudioSource sfxSrc = audioGO.AddComponent<AudioSource>();
        sfxSrc.loop = false;
        sfxSrc.playOnAwake = false;
        sfxSrc.volume = 1f;
        sfxSrc.spatialBlend = 0f;

        // ---------- 界面 ----------
        GameObject uiRoot = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIChineseFont));
        Canvas canvas = uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        RectTransform ui = uiRoot.GetComponent<RectTransform>();
        Sprite round = MakeRoundedSprite();

        // ===== 暗角 =====
        Sprite vig = MakeVignetteSprite();
        if (vig != null)
        {
            RectTransform vg = AddImage(ui, new Color(1f, 1f, 1f, 0.72f), vig, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image vi = vg.GetComponent<Image>();
            vi.type = Image.Type.Simple;
            vg.SetAsFirstSibling();
        }

        // ===== 开始菜单 =====
        RectTransform menu = Panel("MenuPanel", ui, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(menu, new Color(0.008f, 0.012f, 0.028f, 0.56f), null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 装饰：上下细线
        AddImage(menu, new Color(0.16f, 0.55f, 0.78f, 0.55f), null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 314f), new Vector2(780f, 2f));
        AddImage(menu, new Color(0.16f, 0.55f, 0.78f, 0.28f), null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -376f), new Vector2(520f, 2f));

        Text title = NewText(menu, "Title", "星 际 穿 梭", 118, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 388f), new Vector2(1200f, 160f));
        Shadow sh = title.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0.05f, 0.12f, 0.85f);
        sh.effectDistance = new Vector2(0f, -9f);

        Text sub = NewText(menu, "SubTitle", "S T A R   R U N N E R", 30, new Color(0.35f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 262f), new Vector2(1000f, 50f));

        Text tag = NewText(menu, "Tagline", "驾驶飞行器穿越陨石带 · 60 秒不撞毁即通关", 34, new Color(0.72f, 0.83f, 0.92f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 198f), new Vector2(1400f, 60f));

        Button startBtn = NewButton(menu, "StartButton", "开始游戏", new Vector2(0f, -228f), new Vector2(430f, 96f),
            new Color(0.04f, 0.62f, 0.92f), Color.white, 42, round);
        Button quitBtn = NewButton(menu, "QuitButton", "退出游戏", new Vector2(0f, -338f), new Vector2(430f, 78f),
            new Color(0.06f, 0.10f, 0.16f, 0.92f), new Color(0.66f, 0.78f, 0.88f), 34, round);

        Text hint = NewText(menu, "Hint", "方向键 / WASD 操控飞行器　·　ESC 返回主菜单", 26, new Color(0.56f, 0.67f, 0.77f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -418f), new Vector2(1400f, 44f));

        Text credit = NewText(menu, "Credit", "3D 模型与音效：Kenney.nl（CC0 公共领域）", 20, new Color(0.32f, 0.40f, 0.48f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(credit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1200f, 30f));

        // ===== 游戏内 HUD =====
        RectTransform hud = Panel("HUD", ui, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(hud, new Color(0f, 0f, 0f, 0f), null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Text timeLabel = NewText(hud, "TimeLabel", "剩 余 时 间", 24, new Color(0.55f, 0.75f, 0.90f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(timeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(400f, 34f));

        Text timer = NewText(hud, "Timer", "60", 104, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        SetRect(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(400f, 120f));
        Shadow ts = timer.gameObject.AddComponent<Shadow>();
        ts.effectColor = new Color(0f, 0.02f, 0.06f, 0.8f);
        ts.effectDistance = new Vector2(0f, -5f);

        RectTransform barBg = AddImage(hud, new Color(0.10f, 0.16f, 0.22f, 0.75f), round, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(660f, 12f));
        RectTransform barFill = AddImage(hud, new Color(0.25f, 0.85f, 1f), round, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(660f, 12f));
        Image fillImg = barFill.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0f;

        Text passed = NewText(hud, "Passed", "已避开陨石  0  颗", 28, new Color(0.68f, 0.82f, 0.92f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(passed.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(600f, 40f));

        // ===== 结算 =====
        RectTransform result = Panel("ResultPanel", ui, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(result, new Color(0.008f, 0.012f, 0.028f, 0.94f), null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Text rTitle = NewText(result, "ResultTitle", "通 关 成 功", 104, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        SetRect(rTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1200f, 140f));
        Shadow rs = rTitle.gameObject.AddComponent<Shadow>();
        rs.effectColor = new Color(0f, 0.03f, 0.08f, 0.85f);
        rs.effectDistance = new Vector2(0f, -8f);

        Text rSub = NewText(result, "ResultSub", "", 32, new Color(0.75f, 0.86f, 0.94f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(rSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(1300f, 52f));

        Text rPassed = NewText(result, "ResultPassed", "", 30, new Color(0.55f, 0.78f, 0.92f), TextAnchor.MiddleCenter, FontStyle.Normal);
        SetRect(rPassed.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(900f, 46f));

        Button againBtn = NewButton(result, "AgainButton", "再来一局", new Vector2(0f, -110f), new Vector2(400f, 92f),
            new Color(0.04f, 0.62f, 0.92f), Color.white, 40, round);
        Button backBtn = NewButton(result, "BackButton", "返回主菜单", new Vector2(0f, -224f), new Vector2(400f, 76f),
            new Color(0.06f, 0.10f, 0.16f, 0.92f), new Color(0.66f, 0.78f, 0.88f), 32, round);

        // ---------- 总控 ----------
        GameObject gmGO = new GameObject("GameManager", typeof(GameManager));
        GameManager gm = gmGO.GetComponent<GameManager>();
        gm.player = pc;
        gm.spawner = sp;
        gm.cameraRig = camGO.GetComponent<CameraFollow>();
        gm.flySource = flySrc;
        gm.sfxSource = sfxSrc;
        gm.flyClip = flySrc.clip;
        gm.crashClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/crash.ogg");
        gm.passClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/pass.ogg");
        gm.menuPanel = menu.gameObject;
        gm.hudPanel = hud.gameObject;
        gm.timerText = timer;
        gm.hintText = timeLabel;
        gm.progressFill = fillImg;
        gm.passedText = passed;
        gm.resultPanel = result.gameObject;
        gm.resultTitle = rTitle;
        gm.resultSub = rSub;
        gm.resultPassed = rPassed;

        camGO.GetComponent<CameraFollow>().target = player.transform;
        camGO.GetComponent<CameraFollow>().cam = cam;

        // 按钮事件
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, gm.StartGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(againBtn.onClick, gm.StartGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(backBtn.onClick, gm.GoToMenu);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, gm.QuitGame);

        // 初始状态：菜单
        hud.gameObject.SetActive(false);
        result.gameObject.SetActive(false);

        // 保存
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FlyingGameBuilder] 场景构建完成：" + ScenePath);
    }

    // ================= 工具方法 =================

    static void EnsureFolders()
    {
        string[] dirs = { Root, Root + "/Scenes", MatDir, PrefabDir };
        foreach (string d in dirs)
        {
            if (!AssetDatabase.IsValidFolder(d))
            {
                string parent = Path.GetDirectoryName(d).Replace("\\", "/");
                string name = Path.GetFileName(d);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    static Font LoadFont(string fileName)
    {
        string path = Root + "/Fonts/" + fileName;
        Font f = AssetDatabase.LoadAssetAtPath<Font>(path);
        if (f == null) Debug.LogWarning("[FlyingGameBuilder] 字体未导入：" + path);
        return f;
    }

    static Material MakeMat(string name, Color color, float metallic, float smoothness, Color emission)
    {
        return MakeMat(name, color, metallic, smoothness, emission, false);
    }

    static Material MakeMat(string name, Color color, float metallic, float smoothness, Color emission, bool fade)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("Standard");
        m.color = color;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0.01f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }

        // 透明淡出模式（陨石靠近镜头时淡出用）
        if (fade)
        {
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            m.SetOverrideTag("RenderType", "Opaque");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D MakeDotTexture()
    {
        string path = MatDir + "/soft_dot.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Color[] px = new Color[S * S];
        Vector2 c = new Vector2(S / 2f - 0.5f, S / 2f - 0.5f);
        float maxR = S / 2f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxR;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                px[y * S + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Sprite MakeVignetteSprite()
    {
        string path = MatDir + "/vignette.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int S = 256;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Color[] px = new Color[S * S];
        float half = S * 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421356f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1f, d));
                px[y * S + x] = new Color(0f, 0f, 0f, a * 0.95f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite MakeRoundedSprite()
    {
        string path = MatDir + "/ui_round.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int S = 128;
        const float radius = 26f;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Color[] px = new Color[S * S];
        float half = S * 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float d;
                if (dx <= 0f && dy <= 0f) d = Mathf.Max(dx, dy);
                else
                {
                    float ox = Mathf.Max(dx, 0f);
                    float oy = Mathf.Max(dy, 0f);
                    d = Mathf.Sqrt(ox * ox + oy * oy) - radius;
                }
                float a = Mathf.Clamp01(0.5f - d);
                px[y * S + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spriteBorder = new Vector4(32f, 32f, 32f, 32f);
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material MakeParticleMat(string name, Texture2D tex, Color tint)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("Legacy Shaders/Particles/Additive");
        m.SetTexture("_MainTex", tex);
        m.SetColor("_TintColor", tint);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject InstantiateModel(string fbxName, Transform parent, Material[] mats)
    {
        string path = Root + "/Models/" + fbxName + ".fbx";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogError("[FlyingGameBuilder] 找不到模型 " + path);
            return null;
        }
        GameObject go = (GameObject)Object.Instantiate(asset);
        if (go == null)
        {
            Debug.LogError("[FlyingGameBuilder] 模型实例化失败 " + path);
            return null;
        }
        go.name = fbxName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        if (mats != null)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rends)
            {
                Material[] cur = r.sharedMaterials;
                for (int i = 0; i < cur.Length; i++)
                {
                    if (i < mats.Length && mats[i] != null) cur[i] = mats[i];
                    else if (mats.Length > 0) cur[i] = mats[mats.Length - 1];
                }
                r.sharedMaterials = cur;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
        return go;
    }

    static GameObject MakeRockPrefab(string fbxName, Material[] mats, float radius)
    {
        GameObject holder = new GameObject("Rock_" + fbxName);
        GameObject model = InstantiateModel(fbxName, holder.transform, mats);
        if (model != null)
        {
            // 让模型在局部空间居中（Kenney 模型带自身偏移）
            Renderer rend = model.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                model.transform.localPosition = new Vector3(-b.center.x, -b.center.y, -b.center.z);
            }
        }

        Obstacle ob = holder.AddComponent<Obstacle>();
        ob.radius = radius;
        ob.speed = 22f;

        string path = PrefabDir + "/" + holder.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(holder, path);
        Object.DestroyImmediate(holder);
        return prefab;
    }

    static void CreateStarfield(Material mat, Vector3 origin, Vector3 boxSize, float rate, float life, float minSize, float maxSize, Vector2 speedRange, int maxParticles)
    {
        GameObject go = new GameObject("Starfield");
        go.transform.position = origin;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.75f, 0.88f, 1f, 0.95f),
            new Color(1f, 0.95f, 0.85f, 0.95f));
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 0.88f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = mat;
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pr.receiveShadows = false;
    }

    static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        if (size == Vector2.zero) rt.sizeDelta = Vector2.zero;
        else rt.sizeDelta = size;
        return rt;
    }

    static RectTransform AddImage(Transform parent, Color color, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        if (sprite != null) img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return rt;
    }

    static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Text NewText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.text = content;
        bool useBoldAsset = style == FontStyle.Bold && uiFontBold != null;
        t.font = useBoldAsset ? uiFontBold : uiFont;
        t.fontStyle = useBoldAsset ? FontStyle.Normal : style;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button NewButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bg, Color textColor, int fontSize, Sprite round)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bg;
        img.sprite = round;
        img.type = Image.Type.Sliced;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        Text t = NewText(go.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        SetRect(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return btn;
    }

    static void AddSceneToBuild(string path)
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].path == path) { list[i].enabled = true; found = true; }
        }
        if (!found) list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
