using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어에 부착. 장애물 Near Miss 영역(외곽 트리거)의
/// Enter/Exit를 추적하여 성공적 근접 도피 시 부스트 발동.
/// </summary>
public class NearMissSystem : MonoBehaviour
{
    private GameManager gm;
    private HashSet<Obstacle> tracking = new HashSet<Obstacle>();

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += OnGameStart;
            gm.OnGameOver += OnGameOver;
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= OnGameStart;
            gm.OnGameOver -= OnGameOver;
        }
    }

    void OnGameStart()
    {
        tracking.Clear();
    }

    void OnGameOver()
    {
        tracking.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return;

        // Near Miss 외곽 영역 진입 (Obstacle 루트의 트리거)
        var obstacle = other.GetComponent<Obstacle>();
        if (obstacle != null && !obstacle.hasTriggeredNearMiss)
        {
            tracking.Add(obstacle);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return;

        // Near Miss 영역을 사망 없이 성공적으로 통과했을 때만 판정
        var obstacle = other.GetComponent<Obstacle>();
        if (obstacle != null && tracking.Contains(obstacle))
        {
            tracking.Remove(obstacle);
            if (!obstacle.hasTriggeredNearMiss)
            {
                obstacle.hasTriggeredNearMiss = true;
                gm.TriggerNearMiss();

                // Near Miss VFX 발동
                if (VFXManager.Instance != null)
                    VFXManager.Instance.SpawnNearMissEffect(transform.position);
            }
        }
    }
}
