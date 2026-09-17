using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인스펙터 바인딩 기반 UI 매니저 (TextMeshPro 지원)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;

    [Header("HUD Elements")]
    public TMP_Text distanceText;
    public TMP_Text zoneChangeText;
    public TMP_Text nearMissText;

    [Header("Title Elements")]
    public TMP_Text bestDistanceText;

    [Header("Game Over Elements")]
    public TMP_Text finalDistanceText;
    public TMP_Text finalBestText;
    public Button retryButton;

    private GameManager gm;
    private float nearMissDisplayTimer;
    private float zoneChangeTimer;
    private float titleShowTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += ShowHUD;
            gm.OnGameOver += ShowGameOver;
            gm.OnNearMiss += ShowNearMiss;
            gm.OnZoneChanged += ShowZoneChange;
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        ShowTitle();
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= ShowHUD;
            gm.OnGameOver -= ShowGameOver;
            gm.OnNearMiss -= ShowNearMiss;
            gm.OnZoneChanged -= ShowZoneChange;
        }
    }

    void Update()
    {
        if (gm == null) return;

        // 타이틀: 클릭 시 게임 시작
        if (gm.CurrentState == GameManager.GameState.Title)
        {
            if (Input.GetMouseButtonDown(0) && Time.time - titleShowTime > 0.3f)
            {
                gm.StartGame();
            }
        }

        // HUD: 거리 실시간 표시
        if (gm.CurrentState == GameManager.GameState.Playing)
        {
            if (distanceText != null)
                distanceText.text = Mathf.FloorToInt(gm.Distance) + "m";
        }

        // Near Miss 표시 페이드
        if (nearMissDisplayTimer > 0f)
        {
            nearMissDisplayTimer -= Time.deltaTime;
            if (nearMissDisplayTimer <= 0f && nearMissText != null)
                nearMissText.gameObject.SetActive(false);
        }

        // 구간 전환 표시 페이드
        if (zoneChangeTimer > 0f)
        {
            zoneChangeTimer -= Time.deltaTime;
            if (zoneChangeText != null)
            {
                float alpha = Mathf.Clamp01(zoneChangeTimer / 0.5f);
                zoneChangeText.color = new Color(zoneChangeText.color.r, zoneChangeText.color.g, zoneChangeText.color.b, alpha);
            }
            if (zoneChangeTimer <= 0f && zoneChangeText != null)
                zoneChangeText.gameObject.SetActive(false);
        }
    }

    public void ShowTitle()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        titleShowTime = Time.time;

        if (bestDistanceText != null && gm != null && gm.BestDistance > 0f)
            bestDistanceText.text = "BEST: " + Mathf.FloorToInt(gm.BestDistance) + "m";
        else if (bestDistanceText != null)
            bestDistanceText.text = "";
    }

    void ShowHUD()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (distanceText != null) distanceText.text = "0m";
        if (nearMissText != null) nearMissText.gameObject.SetActive(false);
        if (zoneChangeText != null) zoneChangeText.gameObject.SetActive(false);
    }

    void ShowGameOver()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (finalDistanceText != null)
            finalDistanceText.text = Mathf.FloorToInt(gm.Distance) + "m";
        if (finalBestText != null)
            finalBestText.text = "BEST: " + Mathf.FloorToInt(gm.BestDistance) + "m";
    }

    void ShowNearMiss(int combo)
    {
        if (nearMissText != null)
        {
            nearMissText.gameObject.SetActive(true);
            nearMissText.text = combo > 1
                ? "NEAR MISS x" + combo + "!"
                : "NEAR MISS!";
            nearMissText.color = new Color(1f, 0.85f, 0f, 1f);
            nearMissDisplayTimer = 1f;
        }
    }

    void ShowZoneChange(string zoneName)
    {
        if (zoneChangeText != null)
        {
            zoneChangeText.gameObject.SetActive(true);
            zoneChangeText.text = "// " + zoneName;
            zoneChangeText.color = Color.white;
            zoneChangeTimer = 2f;
        }
    }

    public void OnRetryClicked()
    {
        if (gm == null) return;
        gm.RetryGame();

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.ResetVisual();

        var trail = FindAnyObjectByType<TrailController>();
        if (trail != null)
        {
            var tr = trail.GetComponent<TrailRenderer>();
            if (tr != null) tr.Clear();
        }

        ShowTitle();
    }
}