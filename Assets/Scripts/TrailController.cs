using UnityEngine;

/// <summary>
/// 플레이어 이동 잔상(Trail) 색상 제어.
/// Near Miss 시 네온/골드 색상으로 전환, 이후 기본 시안으로 복귀.
/// </summary>
public class TrailController : MonoBehaviour
{
    private TrailRenderer trail;

    private readonly Color normalStartColor = new Color(0f, 1f, 1f, 0.8f);   // 시안
    private readonly Color normalEndColor = new Color(0f, 1f, 1f, 0f);
    private readonly Color nearMissStartColor = new Color(1f, 0.85f, 0f, 1f); // 골드
    private readonly Color nearMissEndColor = new Color(1f, 0.85f, 0f, 0f);

    private float colorResetTimer;

    /// <summary>GameSetup에서 TrailRenderer 주입</summary>
    public void Setup(TrailRenderer tr)
    {
        trail = tr;
        SetNormalColors();
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss += HandleNearMiss;
            gm.OnGameStart += HandleGameStart;
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss -= HandleNearMiss;
            gm.OnGameStart -= HandleGameStart;
        }
    }

    void HandleNearMiss(int combo)
    {
        if (trail == null) return;
        trail.startColor = nearMissStartColor;
        trail.endColor = nearMissEndColor;
        colorResetTimer = 0.5f;
    }

    void HandleGameStart()
    {
        if (trail != null) trail.Clear();
        SetNormalColors();
    }

    void Update()
    {
        if (colorResetTimer > 0f)
        {
            colorResetTimer -= Time.deltaTime;
            if (colorResetTimer <= 0f)
            {
                SetNormalColors();
            }
        }
    }

    void SetNormalColors()
    {
        if (trail == null) return;
        trail.startColor = normalStartColor;
        trail.endColor = normalEndColor;
    }
}
