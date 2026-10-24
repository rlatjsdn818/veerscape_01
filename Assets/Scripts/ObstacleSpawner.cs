using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 에디터 프리팹 및 인스펙터 설정 기반 장애물 스포너:
/// 1. 바닥/천장 큰 가시 (Big Spike): 바닥과 천장에 든든하게 고정되어 경로를 차단하는 대형 가시
/// 2. 공중 작은 회전 가시 (Small Spinning Spike): 공중에 떠서 훨씬 작은 크기(0.5~0.7)로 빙글빙글 회전하는 가시
/// 3. 공중 회전 톱니바퀴 (Spinning Saw): 원형으로 맹렬하게 회전하는 톱날 장애물
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [Tooltip("기본 가시(Spike) 장애물 프리팹")]
    [SerializeField] private GameObject obstaclePrefab;

    [Tooltip("회전 톱니바퀴(Saw) 장애물 프리팹 (미할당 시 자동 탐색)")]
    [SerializeField] private GameObject sawPrefab;

    private GameManager gm;
    private float spawnTimer;

    private float SPAWN_X
    {
        get { return Camera.main != null ? Camera.main.transform.position.x + 20f : 15f; }
    }
    private const float TOP_BOUND = 4.2f;
    private const float BOTTOM_BOUND = -4.2f;

    private static readonly Dictionary<string, Color> zoneColors = new Dictionary<string, Color>
    {
        { "BASIC",    new Color(0.95f, 0.25f, 0.25f) },
        { "SHIFT",    new Color(0.9f, 0.2f, 0.6f) },
        { "FRACTURE", new Color(0.6f, 0.2f, 0.9f) },
        { "DISTORT",  new Color(0.95f, 0.45f, 0.1f) },
        { "VOID",     new Color(1.0f, 0.1f, 0.1f) }
    };

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += ResetSpawner;
        }

        // sawPrefab이 인스펙터에서 미할당된 경우 자동으로 Prefabs 탐색
        if (sawPrefab == null)
        {
#if UNITY_EDITOR
            sawPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SawPrefab.prefab");
#endif
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
            float extraDistance = SpawnPattern();
            var tier = DifficultyConfig.GetTier(gm.Distance);

            float speed = Mathf.Max(gm.CurrentScrollSpeed, 1f);
            spawnTimer = Random.Range(tier.spawnIntervalMin, tier.spawnIntervalMax) + (extraDistance / speed);
        }
    }

    float SpawnPattern()
    {
        var tier = DifficultyConfig.GetTier(gm.Distance);

        // 패턴 목록:
        // 0: 바닥/천장 큰 가시 (Big Spike)
        // 1: 위아래 대형 가시 게이트 (Double Big Spikes)
        // 2: 대형 가시 복도 (Corridor)
        // 3: 지그재그 가시 (Zigzag)
        // 4: 공중 작은 회전 가시 (Small Spinning Spike) ⭐
        // 5: 공중 회전 톱니바퀴 (Spinning Saw Blade) ⭐
        List<int> patterns = new List<int> { 0, 4, 5 };
        if (tier.doubleObstacles) patterns.Add(1);
        if (tier.corridors) patterns.Add(2);
        if (tier.zigzag) patterns.Add(3);
        if (tier.randomMix) patterns.Add(Random.Range(0, 6));

        int pick = patterns[Random.Range(0, patterns.Count)];

        switch (pick)
        {
            case 0: return SpawnSingleBigSpike(tier);
            case 1: return SpawnDoubleBigSpikes(tier);
            case 2: return SpawnCorridor(tier);
            case 3: return SpawnZigzag(tier);
            case 4: return SpawnSmallSpinningSpike(tier);
            case 5: return SpawnSawBlade(tier);
        }
        return 0f;
    }

    /// <summary>
    /// [타입 1] 바닥/천장 큰 가시: 든든하게 고정되어 있는 대형 가시
    /// </summary>
    float SpawnSingleBigSpike(DifficultyConfig.TierData tier)
    {
        bool isTop = Random.value > 0.5f;
        float h = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);

        float scaleX = h * 0.75f;
        float scaleY = h;
        float y = isTop ? TOP_BOUND - (scaleY * 0.45f) : BOTTOM_BOUND + (scaleY * 0.45f);

        Quaternion rot = isTop ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
        CreateBigSpike(obstaclePrefab, new Vector3(SPAWN_X, y, 0f), new Vector3(scaleX, scaleY, 1.0f), rot, tier.zoneName);
        return 0f;
    }

    /// <summary>
    /// [타입 1] 위아래 대형 가시 게이트
    /// </summary>
    float SpawnDoubleBigSpikes(DifficultyConfig.TierData tier)
    {
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 0.8f, TOP_BOUND - gap / 2f - 0.8f);

        float topHeight = TOP_BOUND - (gapCenter + gap / 2f);
        float botHeight = (gapCenter - gap / 2f) - BOTTOM_BOUND;

        if (topHeight > 0.6f)
        {
            float sY = topHeight;
            float sX = sY * 0.75f;
            float y = TOP_BOUND - (sY * 0.45f);
            CreateBigSpike(obstaclePrefab, new Vector3(SPAWN_X, y, 0f), new Vector3(sX, sY, 1.0f), Quaternion.Euler(0f, 0f, 180f), tier.zoneName);
        }
        if (botHeight > 0.6f)
        {
            float sY = botHeight;
            float sX = sY * 0.75f;
            float y = BOTTOM_BOUND + (sY * 0.45f);
            CreateBigSpike(obstaclePrefab, new Vector3(SPAWN_X, y, 0f), new Vector3(sX, sY, 1.0f), Quaternion.identity, tier.zoneName);
        }
        return 0f;
    }

    /// <summary>
    /// 가시 복도 패턴
    /// </summary>
    float SpawnCorridor(DifficultyConfig.TierData tier)
    {
        int segments = Random.Range(3, 5);
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 0.8f, TOP_BOUND - gap / 2f - 0.8f);

        for (int i = 0; i < segments; i++)
        {
            float x = SPAWN_X + i * 2.8f;
            gapCenter += Random.Range(-0.5f, 0.5f);
            gapCenter = Mathf.Clamp(gapCenter, BOTTOM_BOUND + gap / 2f + 0.5f, TOP_BOUND - gap / 2f - 0.5f);

            float topH = TOP_BOUND - (gapCenter + gap / 2f);
            float botH = (gapCenter - gap / 2f) - BOTTOM_BOUND;

            if (topH > 0.5f)
                CreateBigSpike(obstaclePrefab, new Vector3(x, TOP_BOUND - (topH * 0.45f), 0f),
                               new Vector3(topH * 0.75f, topH, 1.0f), Quaternion.Euler(0f, 0f, 180f), tier.zoneName);
            if (botH > 0.5f)
                CreateBigSpike(obstaclePrefab, new Vector3(x, BOTTOM_BOUND + (botH * 0.45f), 0f),
                               new Vector3(botH * 0.75f, botH, 1.0f), Quaternion.identity, tier.zoneName);
        }
        return (segments - 1) * 2.8f;
    }

    /// <summary>
    /// 지그재그 대형 가시 패턴
    /// </summary>
    float SpawnZigzag(DifficultyConfig.TierData tier)
    {
        int count = Random.Range(3, 5);
        bool isTop = Random.value > 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = SPAWN_X + i * 2.8f;
            float h = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
            float sY = h;
            float sX = sY * 0.75f;
            float y = isTop ? TOP_BOUND - (sY * 0.45f) : BOTTOM_BOUND + (sY * 0.45f);
            Quaternion rot = isTop ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

            CreateBigSpike(obstaclePrefab, new Vector3(x, y, 0f), new Vector3(sX, sY, 1.0f), rot, tier.zoneName);
            isTop = !isTop;
        }
        return (count - 1) * 2.8f;
    }

    /// <summary>
    /// [타입 2] 공중 작은 회전 가시:
    /// - 공중에 떠서 훨씬 아담하고 작은 크기(0.5 ~ 0.7)로 슁슁 회전하며 다가오는 가시
    /// </summary>
    float SpawnSmallSpinningSpike(DifficultyConfig.TierData tier)
    {
        if (obstaclePrefab == null) return 0f;

        // 공중 위치 (중앙 및 상/하 라인)
        float[] yPositions = new float[] { 0f, 1.3f, -1.3f, 2.0f, -2.0f };
        float targetY = yPositions[Random.Range(0, yPositions.Length)];

        // 기존 큰 가시(1.5~3.0)보다 훨씬 작고 아담한 크기 (공 0.38 크기에 맞춘 0.5~0.68)
        float smallSize = Random.Range(0.5f, 0.68f);

        GameObject root = Instantiate(obstaclePrefab, new Vector3(SPAWN_X, targetY, 0f), Quaternion.identity);
        root.transform.localScale = new Vector3(smallSize, smallSize, 1.0f);

        var obs = root.GetComponent<Obstacle>();
        if (obs != null)
        {
            // 공중에서 빙글빙글 회전 (초당 200~320도)
            obs.rotateSpeed = (Random.value > 0.5f ? 1 : -1) * Random.Range(200f, 320f);

            // 35% 확률로 상하 미세 부유 진동
            if (Random.value < 0.35f)
            {
                obs.isMoving = true;
                obs.movingAmplitude = Random.Range(0.4f, 0.8f);
                obs.movingFrequency = Random.Range(2f, 3.5f);
            }
        }

        ApplyColor(root, tier.zoneName);
        return 0f;
    }

    /// <summary>
    /// [타입 3] 공중 회전 톱니바퀴 (Spinning Saw Blade)
    /// </summary>
    float SpawnSawBlade(DifficultyConfig.TierData tier)
    {
        GameObject targetPrefab = sawPrefab != null ? sawPrefab : obstaclePrefab;
        if (targetPrefab == null) return 0f;

        float[] yPositions = new float[] { 0f, 1.4f, -1.4f, 2.2f, -2.2f };
        float targetY = yPositions[Random.Range(0, yPositions.Length)];

        // 아담한 톱니바퀴 크기
        float sawSize = Random.Range(0.7f, 1.05f);

        GameObject root = Instantiate(targetPrefab, new Vector3(SPAWN_X, targetY, 0f), Quaternion.identity);
        root.transform.localScale = new Vector3(sawSize, sawSize, 1.0f);

        var obs = root.GetComponent<Obstacle>();
        if (obs != null)
        {
            obs.rotateSpeed = (Random.value > 0.5f ? 1 : -1) * Random.Range(220f, 360f);

            if (Random.value < 0.4f)
            {
                obs.isMoving = true;
                obs.movingAmplitude = Random.Range(0.5f, 1.0f);
                obs.movingFrequency = Random.Range(1.8f, 3f);
            }
        }

        ApplyColor(root, tier.zoneName);
        return 0f;
    }

    /// <summary>
    /// 대형 가시 전용 생성 메서드 (바닥/천장 고정, 회전하지 않음)
    /// </summary>
    void CreateBigSpike(GameObject prefab, Vector3 position, Vector3 scale, Quaternion rotation, string zoneName)
    {
        if (prefab == null) return;

        GameObject root = Instantiate(prefab, position, rotation);
        root.transform.localScale = scale;

        var obs = root.GetComponent<Obstacle>();
        if (obs != null)
        {
            // 바닥/천장의 큰 가시는 돌지 않고 바닥에 고정
            obs.rotateSpeed = 0f;
        }

        ApplyColor(root, zoneName);

        // VOID 구간 추가 이동 옵션
        if (zoneName == "VOID" && Random.value < 0.3f)
        {
            if (obs != null)
            {
                obs.isMoving = true;
                obs.movingAmplitude = Random.Range(0.4f, 1.0f);
                obs.movingFrequency = Random.Range(2f, 3.5f);
            }
        }
    }

    private void ApplyColor(GameObject root, string zoneName)
    {
        var sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (zoneColors.TryGetValue(zoneName, out Color color))
                sr.color = color;
            else
                sr.color = Color.red;
        }
    }
}