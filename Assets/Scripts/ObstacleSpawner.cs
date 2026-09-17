using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 에디터 프리팹 및 인스펙터 설정 기반 장애물 스포너
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject obstaclePrefab;

    private GameManager gm;
    private float spawnTimer;

    private const float SPAWN_X = 12f;
    private const float TOP_BOUND = 4.5f;
    private const float BOTTOM_BOUND = -4.5f;

    private static readonly Dictionary<string, Color> zoneColors = new Dictionary<string, Color>
    {
        { "BASIC",    new Color(0.9f, 0.3f, 0.3f) },
        { "SHIFT",    new Color(0.9f, 0.2f, 0.6f) },
        { "FRACTURE", new Color(0.6f, 0.2f, 0.9f) },
        { "DISTORT",  new Color(0.9f, 0.5f, 0.1f) },
        { "VOID",     new Color(0.9f, 0.1f, 0.1f) }
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
        spawnTimer = 2.0f;
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

        List<int> patterns = new List<int> { 0 };
        if (tier.doubleObstacles) patterns.Add(1);
        if (tier.corridors) patterns.Add(2);
        if (tier.zigzag) patterns.Add(3);
        if (tier.randomMix) patterns.Add(Random.Range(0, 4));

        int pick = patterns[Random.Range(0, patterns.Count)];

        switch (pick)
        {
            case 0: SpawnSingle(tier); break;
            case 1: SpawnDouble(tier); break;
            case 2: SpawnCorridor(tier); break;
            case 3: SpawnZigzag(tier); break;
        }
    }

    void SpawnSingle(DifficultyConfig.TierData tier)
    {
        bool isTop = Random.value > 0.5f;
        float height = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
        float y = isTop ? TOP_BOUND - height / 2f : BOTTOM_BOUND + height / 2f;
        CreateObstacle(new Vector3(SPAWN_X, y, 0f), new Vector3(1.0f, height, 1.0f), tier.zoneName);
    }

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

    void SpawnCorridor(DifficultyConfig.TierData tier)
    {
        int segments = Random.Range(3, 5);
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 1f, TOP_BOUND - gap / 2f - 1f);

        for (int i = 0; i < segments; i++)
        {
            float x = SPAWN_X + i * 2.5f;

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

    void CreateObstacle(Vector3 position, Vector3 scale, string zoneName)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("[ObstacleSpawner] obstaclePrefab이 할당되지 않았습니다. 인스펙터를 확인해주세요.");
            return;
        }

        GameObject root = Instantiate(obstaclePrefab, position, Quaternion.identity);
        root.transform.localScale = scale;

        // 비주얼 스프라이트 색상 적용
        var sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (zoneColors.TryGetValue(zoneName, out Color color))
                sr.color = color;
            else
                sr.color = Color.red;
        }

        // VOID(800m+) 구간 이동 옵션 적용
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