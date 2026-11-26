using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인스펙터 바인딩 기반 리더보드 UI 제어 컴포넌트:
/// - 게임오버 시 이름(닉네임) 입력 및 랭킹 등록
/// - 치트(PgUp/PgDn) 사용 시 리더보드 등록 차단 안내
/// - 랭킹 팝업 창 열기/닫기 및 랭킹 목록 텍스트 갱신
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Leaderboard Popup Panel")]
    [Tooltip("리더보드 전체 팝업 창 패널")]
    public GameObject leaderboardPanel;

    [Tooltip("리더보드 창 닫기 버튼")]
    public Button closeButton;

    [Tooltip("리더보드 창 열기 버튼 (타이틀 화면 또는 게임오버 화면에 배치)")]
    public Button openLeaderboardButton;

    [Tooltip("기록 전체 초기화 버튼 (선택사항 - 랭킹 및 최고기록 초기화)")]
    public Button clearRecordsButton;

    [Header("Name Submission (Game Over)")]
    [Tooltip("게임 오버 시 이름 입력 및 등록 버튼을 감싸는 컨테이너 (치트 시 숨김 처리)")]
    public GameObject nameInputSection;

    [Tooltip("플레이어 닉네임 입력 필드")]
    public TMP_InputField nameInputField;

    [Tooltip("랭킹 기록 제출/등록 버튼")]
    public Button submitButton;

    [Header("Display Texts")]
    [Tooltip("리더보드 순위 목록이 표시되는 텍스트 (줄바꿈 포맷팅)")]
    public TMP_Text leaderboardContentText;

    [Tooltip("등록 완료 메시지 또는 치트 감지 경고 안내 문구 텍스트")]
    public TMP_Text statusNoticeText;

    private bool isSubmittedThisRun = false;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseLeaderboard);
        }

        if (openLeaderboardButton != null)
        {
            openLeaderboardButton.onClick.AddListener(OpenLeaderboard);
        }

        if (clearRecordsButton != null)
        {
            clearRecordsButton.onClick.AddListener(OnClearRecordsClicked);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (nameInputField != null)
        {
            nameInputField.onSubmit.AddListener(val => OnSubmitClicked());
        }

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += HandleGameStart;
            gm.OnGameOver += HandleGameOver;
        }

        var lm = LeaderboardManager.Instance;
        if (lm != null)
        {
            lm.OnLeaderboardUpdated += RefreshLeaderboardDisplay;
        }

        // 시작 시 리더보드 패널 기본 비활성화
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart -= HandleGameStart;
            gm.OnGameOver -= HandleGameOver;
        }

        var lm = LeaderboardManager.Instance;
        if (lm != null)
        {
            lm.OnLeaderboardUpdated -= RefreshLeaderboardDisplay;
        }
    }

    private void HandleGameStart()
    {
        isSubmittedThisRun = false;
        if (statusNoticeText != null) statusNoticeText.text = "";
        if (nameInputSection != null) nameInputSection.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    private void HandleGameOver()
    {
        isSubmittedThisRun = false;
        var gm = GameManager.Instance;
        var lm = LeaderboardManager.Instance;

        // 1. 치트가 사용된 런인 경우
        if (gm != null && gm.IsCheatUsed)
        {
            if (nameInputSection != null) nameInputSection.SetActive(false);
            if (statusNoticeText != null)
            {
                statusNoticeText.gameObject.SetActive(true);
                statusNoticeText.text = "<color=#FF5555>⚠ 치트(구간 스킵)가 감지되어 리더보드에 기록되지 않습니다.</color>";
            }
            return;
        }

        // 2. 정상 플레이인 경우: 이름 입력창 활성화
        if (nameInputSection != null)
        {
            nameInputSection.SetActive(true);
        }

        if (nameInputField != null)
        {
            string lastName = lm != null ? lm.GetLastPlayerName() : "Player";
            nameInputField.text = lastName;
            nameInputField.interactable = true;
        }

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        if (statusNoticeText != null)
        {
            statusNoticeText.text = "";
        }
    }

    /// <summary>
    /// 이름 등록 버튼 클릭 처리
    /// </summary>
    public void OnSubmitClicked()
    {
        if (isSubmittedThisRun) return;

        var gm = GameManager.Instance;
        var lm = LeaderboardManager.Instance;
        if (gm == null || lm == null) return;

        if (gm.IsCheatUsed)
        {
            if (statusNoticeText != null)
                statusNoticeText.text = "<color=#FF5555>치트가 사용된 판은 등록할 수 없습니다.</color>";
            return;
        }

        string playerName = nameInputField != null ? nameInputField.text : "Player";
        lm.AddEntry(playerName, gm.Distance, gm.ShardsCollected);
        isSubmittedThisRun = true;

        if (nameInputField != null) nameInputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;

        if (statusNoticeText != null)
        {
            statusNoticeText.gameObject.SetActive(true);
            statusNoticeText.text = "<color=#55FF88>✓ 랭킹에 등록되었습니다!</color>";
        }

        // 팝업 창이 열려 있다면 갱신
        RefreshLeaderboardDisplay();
    }

    /// <summary>
    /// 기록 초기화 버튼 클릭
    /// </summary>
    public void OnClearRecordsClicked()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.ResetAllRecords();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetBestDistance();
        }

        if (statusNoticeText != null)
        {
            statusNoticeText.gameObject.SetActive(true);
            statusNoticeText.text = "<color=#FFFF55>모든 기록이 초기화되었습니다.</color>";
        }
        RefreshLeaderboardDisplay();
    }

    /// <summary>
    /// 리더보드 팝업 열기
    /// </summary>
    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            RefreshLeaderboardDisplay();
        }
    }

    /// <summary>
    /// 리더보드 팝업 닫기
    /// </summary>
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 리더보드 순위 목록 텍스트 갱신
    /// </summary>
    public void RefreshLeaderboardDisplay()
    {
        if (leaderboardContentText == null) return;

        var lm = LeaderboardManager.Instance;
        if (lm == null)
        {
            leaderboardContentText.text = "Leaderboard not available.";
            return;
        }

        var entries = lm.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            leaderboardContentText.text = "<color=#888888>아직 등록된 기록이 없습니다.\n첫 번째 주인공이 되어보세요!</color>";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string rankColor = i switch
            {
                0 => "#FFD700", // 1위: 금색
                1 => "#C0C0C0", // 2위: 은색
                2 => "#CD7F32", // 3위: 동색
                _ => "#E0E0E0"  // 나머지: 밝은 회색
            };

            string rankStr = (i + 1).ToString().PadLeft(2, ' ');
            string nameStr = e.playerName.PadRight(10, ' ');
            string distStr = $"{Mathf.FloorToInt(e.distance)}m".PadLeft(7, ' ');
            string shardStr = $"★{e.shards}".PadLeft(5, ' ');

            sb.AppendLine($"<color={rankColor}>#{rankStr}  {nameStr}  {distStr}  {shardStr}</color>");
        }

        leaderboardContentText.text = sb.ToString();
    }
}
