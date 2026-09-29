using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class GameSetup : MonoBehaviour
{
    private GameObject cameraObj;

    private void Awake()
    {
        cameraObj = SetupCamera();
        SetupManagers();
        GameObject player = SetupPlayer();
        SetupObstaclePrefabAndSpawner();
        SetupUI();
        
        // BackgroundController
        var bgType = System.Type.GetType("BackgroundController");
        if (bgType != null)
        {
            new GameObject("BackgroundController").AddComponent(bgType);
        }

        // CameraController — 카메라 GO에 직접 부착
        var camType = System.Type.GetType("CameraController");
        if (camType != null && cameraObj != null)
        {
            cameraObj.AddComponent(camType);
        }

        // Cleanup
        Destroy(gameObject);
    }

    private GameObject SetupCamera()
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        ColorUtility.TryParseHtmlString("#0A0A1A", out Color bg);
        cam.backgroundColor = bg;
        camObj.AddComponent<AudioListener>();
        return camObj;
    }

    private void SetupManagers()
    {
        // GameManager
        new GameObject("GameManager").AddComponent<GameManager>();

        // SFXManager (AudioSource를 먼저 추가해야 Awake에서 찾을 수 있음)
        GameObject sfxObj = new GameObject("SFXManager");
        sfxObj.AddComponent<AudioSource>();
        var sfx = sfxObj.AddComponent<SFXManager>();

        // VFXManager
        new GameObject("VFXManager").AddComponent<VFXManager>();

        // PostProcessController
        var ppType = System.Type.GetType("PostProcessController");
        if (ppType != null)
        {
            new GameObject("PostProcessController").AddComponent(ppType);
        }
    }

    private GameObject SetupPlayer()
    {
        GameObject playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(-6f, 0f, 0f);
        playerObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        // Sprite
        SpriteRenderer sr = playerObj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(4, 4);
        Color[] cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = Color.white;

        // Physics
        CircleCollider2D col = playerObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Components
        playerObj.AddComponent<PlayerController>();
        playerObj.AddComponent<NearMissSystem>();
        
        var trailType = System.Type.GetType("TrailController");
        if (trailType != null)
        {
            playerObj.AddComponent(trailType);
        }

        // TrailRenderer
        TrailRenderer tr = playerObj.AddComponent<TrailRenderer>();
        tr.time = 0.3f;
        tr.startWidth = 0.15f;
        tr.endWidth = 0f;
        tr.startColor = Color.cyan;
        tr.endColor = new Color(0, 1, 1, 0);
        tr.material = new Material(Shader.Find("Sprites/Default"));

        return playerObj;
    }

    private void SetupObstaclePrefabAndSpawner()
    {
        // Obstacle Prefab
        GameObject prefabObj = new GameObject("ObstaclePrefab");
        prefabObj.SetActive(false);
        prefabObj.AddComponent<Obstacle>();
        
        // Near Miss 외곽 트리거 (Obstacle 루트에 부착)
        BoxCollider2D nearMissCol = prefabObj.AddComponent<BoxCollider2D>();
        nearMissCol.isTrigger = true;
        nearMissCol.size = new Vector2(1.4f, 1.1f);

        // Rigidbody2D (Kinematic) — 트리거 충돌 감지에 필요
        Rigidbody2D obstacleRb = prefabObj.AddComponent<Rigidbody2D>();
        obstacleRb.bodyType = RigidbodyType2D.Kinematic;

        // KillZone 자식 오브젝트 (실제 사망 충돌 영역)
        GameObject killZoneObj = new GameObject("KillZone");
        killZoneObj.transform.SetParent(prefabObj.transform);
        killZoneObj.transform.localPosition = Vector3.zero;

        BoxCollider2D killCol = killZoneObj.AddComponent<BoxCollider2D>();
        killCol.isTrigger = true;
        killCol.size = new Vector2(1f, 1f);

        killZoneObj.AddComponent<ObstacleKillZone>();

        SpriteRenderer sr = killZoneObj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(4, 4);
        Color[] cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = Color.red;

        DontDestroyOnLoad(prefabObj);

        // Spawner
        GameObject spawnerObj = new GameObject("ObstacleSpawner");
        ObstacleSpawner spawner = spawnerObj.AddComponent<ObstacleSpawner>();
        
        FieldInfo prefabField = typeof(ObstacleSpawner).GetField("obstaclePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        if (prefabField != null)
        {
            prefabField.SetValue(spawner, prefabObj);
        }
        else
        {
            Debug.LogError("Could not find obstaclePrefab field in ObstacleSpawner.");
        }
    }

    private void SetupUI()
    {
        // EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        UIManager uiManager = canvasObj.AddComponent<UIManager>();

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Title Panel
        GameObject titlePanelObj = CreatePanel(canvasObj.transform, "TitlePanel");
        var titleText = CreateText(titlePanelObj.transform, "TitleText", "VEERSCAPE", 120, Color.white, new Vector2(0, 200), defaultFont);
        var subtitleText = CreateText(titlePanelObj.transform, "SubtitleText", "CLICK TO START", 60, Color.gray, new Vector2(0, 0), defaultFont);
        var titleBestText = CreateText(titlePanelObj.transform, "BestDistanceText", "Best: 0m", 50, Color.white, new Vector2(0, -150), defaultFont);

        // HUD Panel
        GameObject hudPanelObj = CreatePanel(canvasObj.transform, "HUDPanel");
        var distanceText = CreateText(hudPanelObj.transform, "DistanceText", "0m", 80, Color.white, new Vector2(0, 450), defaultFont);
        var zoneText = CreateText(hudPanelObj.transform, "ZoneChangeText", "", 100, Color.white, new Vector2(0, 0), defaultFont);
        var nearMissText = CreateText(hudPanelObj.transform, "NearMissText", "", 60, Color.yellow, new Vector2(600, 0), defaultFont);

        // GameOver Panel
        GameObject gameOverPanelObj = CreatePanel(canvasObj.transform, "GameOverPanel");
        var goTitle = CreateText(gameOverPanelObj.transform, "GOTitleText", "GAME OVER", 120, Color.red, new Vector2(0, 300), defaultFont);
        var goFinalDist = CreateText(gameOverPanelObj.transform, "FinalDistanceText", "Distance: 0m", 70, Color.white, new Vector2(0, 100), defaultFont);
        var goBestDist = CreateText(gameOverPanelObj.transform, "FinalBestText", "Best: 0m", 70, Color.yellow, new Vector2(0, -50), defaultFont);
        
        GameObject retryBtnObj = new GameObject("RetryButton", typeof(RectTransform));
        retryBtnObj.transform.SetParent(gameOverPanelObj.transform, false);
        RectTransform btnRect = retryBtnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -250);
        btnRect.sizeDelta = new Vector2(400, 100);
        
        Image btnImg = retryBtnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f);
        
        Button retryButton = retryBtnObj.AddComponent<Button>();
        retryButton.targetGraphic = btnImg;
        
        CreateText(retryBtnObj.transform, "BtnText", "RETRY", 50, Color.white, Vector2.zero, defaultFont);

        SetPrivateField(uiManager, "titlePanel", titlePanelObj);
        SetPrivateField(uiManager, "hudPanel", hudPanelObj);
        SetPrivateField(uiManager, "gameOverPanel", gameOverPanelObj);
        
        SetPrivateField(uiManager, "distanceText", distanceText);
        SetPrivateField(uiManager, "zoneChangeText", zoneText);
        SetPrivateField(uiManager, "nearMissText", nearMissText);
        SetPrivateField(uiManager, "bestDistanceText", titleBestText);
        
        SetPrivateField(uiManager, "finalDistanceText", goFinalDist);
        SetPrivateField(uiManager, "finalBestText", goBestDist);
        
        SetPrivateField(uiManager, "retryButton", retryButton);
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return panel;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, Vector2 anchoredPos, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(800, 200);

        return tmp;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Field {fieldName} not found on {obj.GetType()}");
        }
    }
}
