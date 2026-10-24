using UnityEngine;

/// <summary>
/// 플레이어 이동 잔상(Trail) 제어:
/// - 트레일이 항상 플레이어 스프라이트 뒤에 그려지도록 정렬 순서(Sorting Order) 보장
/// - 게임 리트라이/시작 시 이전 트레일 자국 깨끗하게 초기화(Clear)
/// - 트레일 색상 자동 변경 로직은 사용자 요청으로 제거됨 (인스펙터의 색상 그대로 유지)
/// </summary>
public class TrailController : MonoBehaviour
{
    private TrailRenderer trail;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        EnsureSortingOrder();
    }

    void Start()
    {
        EnsureSortingOrder();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += HandleGameStart;
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart -= HandleGameStart;
        }
    }

    void HandleGameStart()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    }

    /// <summary>
    /// 트레일이 플레이어 스프라이트보다 무조건 뒤에 오도록 렌더링 순서를 설정합니다.
    /// </summary>
    private void EnsureSortingOrder()
    {
        if (trail != null)
        {
            trail.sortingOrder = -1;
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sortingOrder <= 0)
        {
            sr.sortingOrder = 5;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSortingOrder();
    }
#endif
}
