using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 거리 기반 난이도에 따라 장애물 패턴을 스폰합니다.
/// 패턴: 단일, 복합(천장+바닥), 통로, 지그재그
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    private GameManager gm;
    private float spawnTimer;

    // 스폰 X 좌표 (화면 오른쪽 밖)
    private const float SPAWN_X = 12f;
    private const float TOP_BOUND = 4.5f;
    private const float BOTTOM_BOUND = -4.5f;

    // Near Miss 영역 마진 (킬존 대비 확장 크기)
    private const float NEAR_MISS_MARGIN = 1.0f;

    // 구간별 장애물 색상
    private static readonly Dictionary<string, Color> zoneColors = new Dictionary<string, Color>
    {
        { "BASIC",    new Color(0.9f, 0.3f, 0.3f) },   // 붉은색
        { "SHIFT",    new Color(0.9f, 0.2f, 0.6f) },   // 마젠타
        { "FRACTURE", new Color(0.6f, 0.2f, 0.9f) },   // 보라
        { "DISTORT",  new Color(0.9f, 0.5f, 0.1f) },   // 오렌지
        { "VOID",     new Color(0.9f, 0.1f, 0.1f) }    // 강렬한 빨강
    };

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += ResetSpawner;
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= ResetSpawner;
        }
    }

    void ResetSpawner()
    {
        spawnTimer = 2.0f; // 시작 후 첫 장애물 등장 딜레이
    }

    void Update()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnPattern();
            var tier = DifficultyConfig.GetTier(gm.Distance);
            spawnTimer = Random.Range(tier.spawnIntervalMin, tier.spawnIntervalMax);
        }
    }

    void SpawnPattern()
    {
        var tier = DifficultyConfig.GetTier(gm.Distance);

        // 사용 가능한 패턴 목록
        List<int> patterns = new List<int> { 0 }; // 단일 항상 가능
        if (tier.doubleObstacles) patterns.Add(1);
        if (tier.corridors) patterns.Add(2);
        if (tier.zigzag) patterns.Add(3);
        if (tier.randomMix) patterns.Add(Random.Range(0, 4)); // 무작위 추가

        int pick = patterns[Random.Range(0, patterns.Count)];

        switch (pick)
        {
            case 0: SpawnSingle(tier); break;
            case 1: SpawnDouble(tier); break;
            case 2: SpawnCorridor(tier); break;
            case 3: SpawnZigzag(tier); break;
        }
    }

    // ── 패턴: 단일 장애물 ──
    void SpawnSingle(DifficultyConfig.TierData tier)
    {
        bool isTop = Random.value > 0.5f;
        float height = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
        float y = isTop ? TOP_BOUND - height / 2f : BOTTOM_BOUND + height / 2f;
        CreateObstacle(new Vector3(SPAWN_X, y, 0f), new Vector3(1.0f, height, 1.0f), tier.zoneName);
    }

    // ── 패턴: 복합 (천장 + 바닥, 중앙 갭) ──
    void SpawnDouble(DifficultyConfig.TierData tier)
    {
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 1f, TOP_BOUND - gap / 2f - 1f);

        float topHeight = TOP_BOUND - (gapCenter + gap / 2f);
        float botHeight = (gapCenter - gap / 2f) - BOTTOM_BOUND;

        if (topHeight > 0.5f)
            CreateObstacle(new Vector3(SPAWN_X, TOP_BOUND - topHeight / 2f, 0f),
                           new Vector3(1.2f, topHeight, 1.0f), tier.zoneName);
        if (botHeight > 0.5f)
            CreateObstacle(new Vector3(SPAWN_X, BOTTOM_BOUND + botHeight / 2f, 0f),
                           new Vector3(1.2f, botHeight, 1.0f), tier.zoneName);
    }

    // ── 패턴: 통로 (연속 복합 장애물, 갭 위치 이동) ──
    void SpawnCorridor(DifficultyConfig.TierData tier)
    {
        int segments = Random.Range(3, 5);
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 1f, TOP_BOUND - gap / 2f - 1f);

        for (int i = 0; i < segments; i++)
        {
            float x = SPAWN_X + i * 2.5f;

            // 갭 위치를 조금씩 이동시켜 통로 형성
            gapCenter += Random.Range(-0.8f, 0.8f);
            gapCenter = Mathf.Clamp(gapCenter, BOTTOM_BOUND + gap / 2f + 0.5f, TOP_BOUND - gap / 2f - 0.5f);

            float topH = TOP_BOUND - (gapCenter + gap / 2f);
            float botH = (gapCenter - gap / 2f) - BOTTOM_BOUND;

            if (topH > 0.3f)
                CreateObstacle(new Vector3(x, TOP_BOUND - topH / 2f, 0f),
                               new Vector3(1.5f, topH, 1.0f), tier.zoneName);
            if (botH > 0.3f)
                CreateObstacle(new Vector3(x, BOTTOM_BOUND + botH / 2f, 0f),
                               new Vector3(1.5f, botH, 1.0f), tier.zoneName);
        }
    }

    // ── 패턴: 지그재그 (교대 단일 장애물) ──
    void SpawnZigzag(DifficultyConfig.TierData tier)
    {
        int count = Random.Range(3, 6);
        bool isTop = Random.value > 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = SPAWN_X + i * 2.5f;
            float height = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
            float y = isTop ? TOP_BOUND - height / 2f : BOTTOM_BOUND + height / 2f;

            CreateObstacle(new Vector3(x, y, 0f), new Vector3(1.0f, height, 1.0f), tier.zoneName);
            isTop = !isTop;
        }
    }

    // ── 장애물 오브젝트 생성 ──
    void CreateObstacle(Vector3 position, Vector3 scale, string zoneName)
    {
        // --- 루트 (Near Miss 영역) ---
        var root = new GameObject("Obstacle");
        root.transform.position = position;

        // Near Miss 외곽 트리거 콜라이더 (킬존보다 약간 큰)
        var nearMissCol = root.AddComponent<BoxCollider2D>();
        nearMissCol.isTrigger = true;
        nearMissCol.size = new Vector2(scale.x + NEAR_MISS_MARGIN, scale.y + NEAR_MISS_MARGIN);

        root.AddComponent<Obstacle>();

        // --- 킬존 (자식) ---
        var killGO = new GameObject("KillZone");
        killGO.transform.SetParent(root.transform, false);
        var killCol = killGO.AddComponent<BoxCollider2D>();
        killCol.isTrigger = true;
        killCol.size = new Vector2(scale.x, scale.y);
        killGO.AddComponent<ObstacleKillZone>();

        // --- 비주얼 (자식) ---
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = scale;
        
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = VFXManager.SquareSprite;

        // 구간별 색상 적용
        Color color;
        if (!zoneColors.TryGetValue(zoneName, out color))
            color = Color.red;
        sr.color = color;

        // VOID(800m+) 구간에서는 30% 확률로 상하로 움직이는 장애물 생성
        if (zoneName == "VOID" && Random.value < 0.3f)
        {
            var obs = root.GetComponent<Obstacle>();
            if (obs != null)
            {
                obs.isMoving = true;
                obs.movingAmplitude = Random.Range(0.5f, 1.5f);
                obs.movingFrequency = Random.Range(2f, 5f);
            }
        }
    }
}
