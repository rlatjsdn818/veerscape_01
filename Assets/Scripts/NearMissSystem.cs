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
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= OnGameStart;
        }
    }

    void OnGameStart()
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
        if (gm == null) return;

        // Near Miss 영역을 사망 없이 통과 = 성공!
        var obstacle = other.GetComponent<Obstacle>();
        if (obstacle != null && tracking.Contains(obstacle))
        {
            tracking.Remove(obstacle);
            if (gm.CurrentState == GameManager.GameState.Playing && !obstacle.hasTriggeredNearMiss)
            {
                obstacle.hasTriggeredNearMiss = true;
                gm.TriggerNearMiss();
            }
        }
    }
}
