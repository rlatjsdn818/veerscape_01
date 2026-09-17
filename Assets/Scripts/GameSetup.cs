using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 자동 부트스트랩: RuntimeInitializeOnLoadMethod로 실행되어
/// 모든 게임 오브젝트를 코드에서 생성합니다.
/// 프리팹/에셋 파일 없이 SampleScene에서 바로 Play 가능.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        // 이미 존재하면 중복 생성 방지
        if (FindAnyObjectByType<GameSetup>() != null) return;
        if (FindAnyObjectByType<GameManager>() != null) return;

        var go = new GameObject("_GameSetup");
        go.AddComponent<GameSetup>();
    }

    void Awake()
    {
        // 초기화 순서 중요: 매니저 → 카메라 → 경계 → 플레이어 → UI
        SetupManagers();
        SetupCamera();
        SetupBoundaries();
        SetupPlayer();
        SetupUI();

        // 불필요한 기본 오브젝트 정리
        CleanupDefaultObjects();
    }

    void CleanupDefaultObjects()
    {
        // 기존 Directional Light 제거 (Unlit 머티리얼 사용)
        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            Destroy(light.gameObject);
        }

        // 기존 Global Volume은 유지 (향후 포스트프로세싱용)
    }

    // ══════════════════════════════════════
    //  매니저 생성 (가장 먼저 - Instance 설정)
    // ══════════════════════════════════════
    void SetupManagers()
    {
        // GameManager (최우선)
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();

        // VFXManager
        var vfxGO = new GameObject("VFXManager");
        vfxGO.AddComponent<VFXManager>();

        // SFXManager
        var sfxGO = new GameObject("SFXManager");
        var sfx = sfxGO.AddComponent<SFXManager>();
        var audioSrc = sfxGO.AddComponent<AudioSource>();
        sfx.Setup(audioSrc);

        // ObstacleSpawner
        var spawnerGO = new GameObject("ObstacleSpawner");
        spawnerGO.AddComponent<ObstacleSpawner>();

        // PostProcessController
        var ppGO = new GameObject("PostProcessController");
        ppGO.AddComponent<PostProcessController>();
    }

    // ══════════════════════════════════════
    //  카메라 설정 (기존 카메라 수정)
    // ══════════════════════════════════════
    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f, 1f); // 다크 네이비
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;

        // CameraController 추가
        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();
    }

    // ══════════════════════════════════════
    //  경계(천장/바닥) 생성
    // ══════════════════════════════════════
    void SetupBoundaries()
    {
        CreateBoundary("Ceiling", new Vector3(0f, 4.75f, 0f), new Vector3(30f, 0.5f, 1f));
        CreateBoundary("Floor",   new Vector3(0f, -4.75f, 0f), new Vector3(30f, 0.5f, 1f));
    }

    void CreateBoundary(string name, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = VFXManager.SquareSprite;
        sr.color = new Color(0.12f, 0.12f, 0.22f);
    }

    // ══════════════════════════════════════
    //  플레이어 생성
    // ══════════════════════════════════════
    void SetupPlayer()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(-6f, 0f, 0f);
        playerGO.transform.localScale = Vector3.one * 0.6f;

        var sr = playerGO.AddComponent<SpriteRenderer>();
        sr.sprite = VFXManager.CircleSprite;
        sr.color = new Color(0f, 1f, 1f);

        // Rigidbody2D (키네마틱 - 트리거 감지용)
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        var col = playerGO.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
        col.radius = 0.5f;

        // 스크립트
        playerGO.AddComponent<PlayerController>();
        playerGO.AddComponent<NearMissSystem>();

        // 트레일 잔상
        var trail = playerGO.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0f, 1f, 1f, 0.8f);
        trail.endColor = new Color(0f, 1f, 1f, 0f);
        trail.minVertexDistance = 0.1f;
        trail.numCornerVertices = 4;

        var trailCtrl = playerGO.AddComponent<TrailController>();
        trailCtrl.Setup(trail);
    }

    // ══════════════════════════════════════
    //  UI 캔버스 & 패널 생성
    // ══════════════════════════════════════
    void SetupUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var uiMgr = canvasGO.AddComponent<UIManager>();

        // ── HUD 패널 ──
        var hudPanel = CreatePanel(canvasGO.transform, "HUDPanel");
        uiMgr.hudPanel = hudPanel;

        uiMgr.distanceText = CreateText(hudPanel.transform, "DistanceText",
            "0m", 48, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(400f, 60f), Color.white);

        uiMgr.zoneChangeText = CreateText(hudPanel.transform, "ZoneChangeText",
            "", 36, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 100f), new Vector2(600f, 50f), Color.white);
        uiMgr.zoneChangeText.gameObject.SetActive(false);

        uiMgr.nearMissText = CreateText(hudPanel.transform, "NearMissText",
            "NEAR MISS!", 42, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -50f), new Vector2(500f, 60f), new Color(1f, 0.85f, 0f));
        uiMgr.nearMissText.gameObject.SetActive(false);

        // ── 타이틀 패널 ──
        var titlePanel = CreatePanel(canvasGO.transform, "TitlePanel");
        uiMgr.titlePanel = titlePanel;

        CreateText(titlePanel.transform, "TitleText",
            "VEERSCAPE", 80, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), new Vector2(800f, 120f), Color.white);

        CreateText(titlePanel.transform, "ClickToStart",
            "CLICK TO START", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -40f), new Vector2(500f, 50f), new Color(0.7f, 0.7f, 0.7f));

        uiMgr.bestDistanceText = CreateText(titlePanel.transform, "BestText",
            "", 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f), new Vector2(400f, 40f), new Color(0.5f, 0.5f, 0.5f));

        // ── 게임 오버 패널 ──
        var goPanel = CreatePanel(canvasGO.transform, "GameOverPanel");
        uiMgr.gameOverPanel = goPanel;
        goPanel.SetActive(false);

        CreateText(goPanel.transform, "GameOverTitle",
            "GAME OVER", 60, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 120f), new Vector2(600f, 80f), Color.white);

        uiMgr.finalDistanceText = CreateText(goPanel.transform, "FinalDistance",
            "0m", 72, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 40f), new Vector2(500f, 90f), Color.white);

        uiMgr.finalBestText = CreateText(goPanel.transform, "FinalBest",
            "BEST: 0m", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -30f), new Vector2(400f, 40f), new Color(0.7f, 0.7f, 0.7f));

        // Retry 버튼
        var retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(goPanel.transform, false);

        var retryImg = retryGO.AddComponent<Image>();
        retryImg.color = new Color(0f, 0.8f, 0.8f, 0.9f);

        var retryRect = retryGO.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.5f, 0.5f);
        retryRect.anchorMax = new Vector2(0.5f, 0.5f);
        retryRect.pivot = new Vector2(0.5f, 0.5f);
        retryRect.anchoredPosition = new Vector2(0f, -100f);
        retryRect.sizeDelta = new Vector2(250f, 60f);

        var retryBtn = retryGO.AddComponent<Button>();
        retryBtn.targetGraphic = retryImg;
        uiMgr.retryButton = retryBtn;

        var retryText = CreateText(retryGO.transform, "RetryLabel",
            "RETRY", 32, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.04f, 0.04f, 0.1f));
        // 버튼 내부 텍스트는 부모 크기에 맞춤
        var rtText = retryText.GetComponent<RectTransform>();
        rtText.offsetMin = Vector2.zero;
        rtText.offsetMax = Vector2.zero;

        retryBtn.onClick.AddListener(uiMgr.OnRetryClicked);

        // EventSystem (없으면 생성)
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ── 헬퍼: 빈 패널 생성 ──
    GameObject CreatePanel(Transform parent, string name)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    // ── 헬퍼: Text 컴포넌트 생성 ──
    Text CreateText(Transform parent, string name, string content, int fontSize,
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // 폰트 로드 (Unity 6 호환)
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);

        return text;
    }
}
