using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 게임 전체 상태 관리, 거리 추적, 부스트/무적 타이머, Near Miss 콤보.
/// 싱글턴 패턴으로 전역 접근.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Title, Playing, GameOver }

    // ── 상태 ──
    public GameState CurrentState { get; private set; } = GameState.Title;
    public float Distance { get; private set; }
    public float BestDistance { get; private set; }
    public float CurrentScrollSpeed { get; private set; }

    // ── 부스트 ──
    private float boostTimer;
    private float boostSpeedAdd;

    // ── 무적 (Safety Window) ──
    public bool IsInvincible { get; private set; }
    private float invincibilityTimer;

    // ── Near Miss 콤보 ──
    public int NearMissCombo { get; private set; }
    private float comboResetTimer;
    private const float COMBO_RESET_TIME = 2f;

    // ── 이벤트 ──
    public event Action OnGameStart;
    public event Action OnGameOver;
    public event Action<int> OnNearMiss;      // combo count
    public event Action<string> OnZoneChanged; // zone name

    private string currentZoneName = "";

    // ── 활성 장애물 추적 ──
    public List<Obstacle> ActiveObstacles = new List<Obstacle>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BestDistance = PlayerPrefs.GetFloat("VeerscapeBest", 0f);
    }

    void Update()
    {
        if (CurrentState != GameState.Playing) return;

        // 거리 갱신
        Distance += CurrentScrollSpeed * Time.deltaTime;

        // 난이도 구간 체크
        var tier = DifficultyConfig.GetTier(Distance);
        float baseSpeed = tier.scrollSpeed;

        if (tier.zoneName != currentZoneName)
        {
            currentZoneName = tier.zoneName;
            OnZoneChanged?.Invoke(currentZoneName);
        }

        // 부스트 감쇠
        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f) boostSpeedAdd = 0f;
        }
        CurrentScrollSpeed = baseSpeed + boostSpeedAdd;

        // 무적 감쇠
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f) IsInvincible = false;
        }

        // 콤보 리셋 타이머
        if (NearMissCombo > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f) NearMissCombo = 0;
        }
    }

    // ── Public API ──

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Distance = 0f;
        NearMissCombo = 0;
        boostTimer = 0f;
        boostSpeedAdd = 0f;
        invincibilityTimer = 0f;
        IsInvincible = false;
        currentZoneName = "";

        var tier = DifficultyConfig.GetTier(0f);
        CurrentScrollSpeed = tier.scrollSpeed;

        OnGameStart?.Invoke();
    }

    public void EndGame()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;

        if (Distance > BestDistance)
        {
            BestDistance = Distance;
            PlayerPrefs.SetFloat("VeerscapeBest", BestDistance);
            PlayerPrefs.Save();
        }

        CurrentScrollSpeed = 0f;
        OnGameOver?.Invoke();
        TriggerHitStop(0.15f);
    }

    public void RetryGame()
    {
        // 모든 활성 장애물 제거
        for (int i = ActiveObstacles.Count - 1; i >= 0; i--)
        {
            if (ActiveObstacles[i] != null) Destroy(ActiveObstacles[i].gameObject);
        }
        ActiveObstacles.Clear();

        CurrentState = GameState.Title;
    }

    public void ApplyBoost(float additionalSpeed, float duration)
    {
        boostSpeedAdd = additionalSpeed;
        boostTimer = duration;
    }

    /// <summary>
    /// Near Miss 판정 시 호출. 콤보 증가, 부스트, 무적 적용.
    /// </summary>
    public void TriggerNearMiss()
    {
        NearMissCombo++;
        comboResetTimer = COMBO_RESET_TIME;

        // 콤보에 비례한 부스트
        float boostAmount = 4f + NearMissCombo * 1.5f;
        ApplyBoost(boostAmount, 0.5f);

        // 0.2초 충돌 유예 (Safety Window)
        IsInvincible = true;
        invincibilityTimer = 0.2f;

        OnNearMiss?.Invoke(NearMissCombo);
        TriggerHitStop(0.05f);
    }

    public void TriggerHitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
