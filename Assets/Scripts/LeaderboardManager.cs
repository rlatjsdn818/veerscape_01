using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 로컬 리더보드(랭킹) 데이터 관리 컴포넌트:
/// - PlayerPrefs에 JSON 형태로 랭킹 목록 영구 저장
/// - 상위 순위(거리 기준 내림차순) 정렬 및 최대 보관 개수 제한
/// - 마지막으로 사용한 플레이어 닉네임 기억
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [System.Serializable]
    public class Entry
    {
        public string playerName;
        public float distance;
        public int shards;
        public string date; // "yyyy-MM-dd"
    }

    [System.Serializable]
    private class LeaderboardData
    {
        public List<Entry> entries = new List<Entry>();
    }

    [Header("Settings")]
    [Tooltip("리더보드에 보관할 최대 랭킹 순위 개수 (기본 10위)")]
    public int maxEntries = 10;

    private const string PREF_KEY_LEADERBOARD = "Veerscape_Leaderboard_Data";
    private const string PREF_KEY_LAST_NAME = "Veerscape_Last_Player_Name";

    private LeaderboardData currentData = new LeaderboardData();

    public event Action OnLeaderboardUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadData();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // [F8] 키: 에디터/테스트 환경에서 하이스코어 및 리더보드 즉시 초기화 단축키
        if (Input.GetKeyDown(KeyCode.F8))
        {
            ResetAllRecords();
            Debug.Log("[LeaderboardManager] (F8 Pressed) All Highscore & Leaderboard Records Reset!");
        }
#endif
    }

    /// <summary>
    /// 현재 저장된 리더보드 랭킹 목록 복사본 반환
    /// </summary>
    public List<Entry> GetEntries()
    {
        return new List<Entry>(currentData.entries);
    }

    /// <summary>
    /// 주어진 거리가 리더보드 랭킹(Top N)에 진입 가능한 기록인지 확인
    /// </summary>
    public bool IsEligibleForLeaderboard(float distance)
    {
        if (distance <= 0f) return false;
        if (currentData.entries.Count < maxEntries) return true;
        return distance > currentData.entries[currentData.entries.Count - 1].distance;
    }

    /// <summary>
    /// 새 랭킹 기록 등록 (치트 미사용 시 호출)
    /// </summary>
    public bool AddEntry(string playerName, float distance, int shards)
    {
        string cleanName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        if (cleanName.Length > 12) cleanName = cleanName.Substring(0, 12);

        SetLastPlayerName(cleanName);

        Entry newEntry = new Entry
        {
            playerName = cleanName,
            distance = Mathf.Floor(distance),
            shards = shards,
            date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        currentData.entries.Add(newEntry);
        // 거리 기준 내림차순 정렬 (동점 시 샤드 많은 순)
        currentData.entries.Sort((a, b) =>
        {
            int distCompare = b.distance.CompareTo(a.distance);
            if (distCompare != 0) return distCompare;
            return b.shards.CompareTo(a.shards);
        });

        // 최대 개수 초과분 정리
        if (currentData.entries.Count > maxEntries)
        {
            currentData.entries.RemoveRange(maxEntries, currentData.entries.Count - maxEntries);
        }

        SaveData();
        OnLeaderboardUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 이전에 사용했던 플레이어 이름 반환
    /// </summary>
    public string GetLastPlayerName()
    {
        return PlayerPrefs.GetString(PREF_KEY_LAST_NAME, "Player");
    }

    /// <summary>
    /// 플레이어 이름 기억
    /// </summary>
    public void SetLastPlayerName(string name)
    {
        PlayerPrefs.SetString(PREF_KEY_LAST_NAME, name);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 리더보드 전체 초기화
    /// </summary>
    public void ClearLeaderboard()
    {
        currentData.entries.Clear();
        PlayerPrefs.DeleteKey(PREF_KEY_LEADERBOARD);
        PlayerPrefs.Save();
        OnLeaderboardUpdated?.Invoke();
        Debug.Log("[LeaderboardManager] Leaderboard data cleared.");
    }

    /// <summary>
    /// 리더보드와 게임 최고 기록(Best Distance)을 한 번에 모두 초기화
    /// </summary>
    public void ResetAllRecords()
    {
        ClearLeaderboard();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetBestDistance();
        }
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(currentData);
        PlayerPrefs.SetString(PREF_KEY_LEADERBOARD, json);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        string json = PlayerPrefs.GetString(PREF_KEY_LEADERBOARD, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                currentData = JsonUtility.FromJson<LeaderboardData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardManager] Failed to load data: {ex.Message}");
                currentData = new LeaderboardData();
            }
        }
        else
        {
            currentData = new LeaderboardData();
        }
    }
}
