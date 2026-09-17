using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 관리: 타이틀 화면, 인게임 HUD, 게임 오버 패널.
/// GameSetup에서 UI 요소 참조 주입.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── UI 요소 참조 (GameSetup에서 주입) ──
    [HideInInspector] public Text distanceText;
    [HideInInspector] public Text bestDistanceText;
    [HideInInspector] public Text nearMissText;
    [HideInInspector] public Text zoneChangeText;
    [HideInInspector] public Text finalDistanceText;
    [HideInInspector] public Text finalBestText;
    [HideInInspector] public Button retryButton;

    [HideInInspector] public GameObject titlePanel;
    [HideInInspector] public GameObject hudPanel;
    [HideInInspector] public GameObject gameOverPanel;

    private GameManager gm;
    private float nearMissDisplayTimer;
    private float zoneChangeTimer;
    private float titleShowTime;

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
                zoneChangeText.color = new Color(1f, 1f, 1f, alpha);
            }
            if (zoneChangeTimer <= 0f && zoneChangeText != null)
                zoneChangeText.gameObject.SetActive(false);
        }
    }

    // ── 패널 전환 ──

    void ShowTitle()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        titleShowTime = Time.time;

        // 최고 기록 표시
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

    /// <summary>Retry 버튼 클릭 핸들러</summary>
    public void OnRetryClicked()
    {
        if (gm == null) return;
        gm.RetryGame();

        // 플레이어 비주얼 리셋
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.ResetVisual();

        // 트레일 클리어
        var trail = FindAnyObjectByType<TrailController>();
        if (trail != null)
        {
            var tr = trail.GetComponent<TrailRenderer>();
            if (tr != null) tr.Clear();
        }

        ShowTitle();
    }
}
