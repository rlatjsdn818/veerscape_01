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
    [Tooltip("기본 가시(Spike) 장애물 프리팹 (공중 작은 회전 가시 등)")]
    [SerializeField] private GameObject obstaclePrefab;

    [Tooltip("바닥/천장 큰 가시(Big Spike) 전용 프리팹 (미할당 시 자동 탐색)")]
    [SerializeField] private GameObject bigSpikePrefab;

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

        // bigSpikePrefab 미할당 시 자동 로드
        if (bigSpikePrefab == null)
        {
#if UNITY_EDITOR
            bigSpikePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BigSpikePrefab.prefab");
#endif
            if (bigSpikePrefab == null) bigSpikePrefab = obstaclePrefab;
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
            // 복합 패턴 후의 딜레이를 0.55배로 축소하여 빈 구간 없이 긴장감 유지
            spawnTimer = Random.Range(tier.spawnIntervalMin, tier.spawnIntervalMax) + (extraDistance / speed * 0.55f);
        }
    }

    float SpawnPattern()
    {
        var tier = DifficultyConfig.GetTier(gm.Distance);

        // 패턴 목록:
        // 0: 바닥/천장 큰 가시 (단일 또는 연속 쌍)
        // 1: 위아래 대형 가시 게이트
        // 2: 대형 가시 복도 (3~5연속)
        // 3: 지그재그 대형 가시 (4~6연속)
        // 4: 공중 작은 회전 가시
        // 5: 공중 회전 톱니바퀴
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

    private GameObject BigSpikePrefabToUse => bigSpikePrefab != null ? bigSpikePrefab : obstaclePrefab;

    /// <summary>
    /// [타입 1] 바닥/천장 큰 가시: 35% 확률로 2연속 스폰하여 밀도 증가
    /// </summary>
    float SpawnSingleBigSpike(DifficultyConfig.TierData tier)
    {
        bool isTop = Random.value > 0.5f;
        float h = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
        float scaleX = h * 0.75f;
        float scaleY = h;
        float y = isTop ? TOP_BOUND - (scaleY * 0.5f) : BOTTOM_BOUND + (scaleY * 0.5f);
        Quaternion rot = isTop ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

        CreateBigSpike(BigSpikePrefabToUse, new Vector3(SPAWN_X, y, 0f), new Vector3(scaleX, scaleY, 1.0f), rot, tier.zoneName);

        // 35% 확률로 바로 뒤에 반대쪽 또는 같은 쪽에 추가 가시 배치
        if (Random.value < 0.35f)
        {
            bool secondTop = Random.value > 0.5f;
            float h2 = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
            float sY2 = h2;
            float sX2 = sY2 * 0.75f;
            float y2 = secondTop ? TOP_BOUND - (sY2 * 0.5f) : BOTTOM_BOUND + (sY2 * 0.5f);
            Quaternion rot2 = secondTop ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

            CreateBigSpike(BigSpikePrefabToUse, new Vector3(SPAWN_X + 2.2f, y2, 0f), new Vector3(sX2, sY2, 1.0f), rot2, tier.zoneName);
            return 2.2f;
        }

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
            float y = TOP_BOUND - (sY * 0.5f);
            CreateBigSpike(BigSpikePrefabToUse, new Vector3(SPAWN_X, y, 0f), new Vector3(sX, sY, 1.0f), Quaternion.Euler(0f, 0f, 180f), tier.zoneName);
        }
        if (botHeight > 0.6f)
        {
            float sY = botHeight;
            float sX = sY * 0.75f;
            float y = BOTTOM_BOUND + (sY * 0.5f);
            CreateBigSpike(BigSpikePrefabToUse, new Vector3(SPAWN_X, y, 0f), new Vector3(sX, sY, 1.0f), Quaternion.identity, tier.zoneName);
        }
        return 0f;
    }

    /// <summary>
    /// 가시 복도 패턴: 3~5개 기둥, 2.4 유닛 간격
    /// </summary>
    float SpawnCorridor(DifficultyConfig.TierData tier)
    {
        int segments = Random.Range(3, 6);
        float gap = Random.Range(tier.gapSizeMin, tier.gapSizeMax);
        float gapCenter = Random.Range(BOTTOM_BOUND + gap / 2f + 0.8f, TOP_BOUND - gap / 2f - 0.8f);

        for (int i = 0; i < segments; i++)
        {
            float x = SPAWN_X + i * 2.4f;
            gapCenter += Random.Range(-0.5f, 0.5f);
            gapCenter = Mathf.Clamp(gapCenter, BOTTOM_BOUND + gap / 2f + 0.5f, TOP_BOUND - gap / 2f - 0.5f);

            float topH = TOP_BOUND - (gapCenter + gap / 2f);
            float botH = (gapCenter - gap / 2f) - BOTTOM_BOUND;

            if (topH > 0.5f)
                CreateBigSpike(BigSpikePrefabToUse, new Vector3(x, TOP_BOUND - (topH * 0.5f), 0f),
                               new Vector3(topH * 0.75f, topH, 1.0f), Quaternion.Euler(0f, 0f, 180f), tier.zoneName);
            if (botH > 0.5f)
                CreateBigSpike(BigSpikePrefabToUse, new Vector3(x, BOTTOM_BOUND + (botH * 0.5f), 0f),
                               new Vector3(botH * 0.75f, botH, 1.0f), Quaternion.identity, tier.zoneName);
        }
        return (segments - 1) * 2.4f;
    }

    /// <summary>
    /// 지그재그 대형 가시 패턴: 4~6개 연속 지그재그
    /// </summary>
    float SpawnZigzag(DifficultyConfig.TierData tier)
    {
        int count = Random.Range(4, 7);
        bool isTop = Random.value > 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = SPAWN_X + i * 2.4f;
            float h = Random.Range(tier.obstacleHeightMin, tier.obstacleHeightMax);
            float sY = h;
            float sX = sY * 0.75f;
            float y = isTop ? TOP_BOUND - (sY * 0.5f) : BOTTOM_BOUND + (sY * 0.5f);
            Quaternion rot = isTop ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

            CreateBigSpike(BigSpikePrefabToUse, new Vector3(x, y, 0f), new Vector3(sX, sY, 1.0f), rot, tier.zoneName);
            isTop = !isTop;
        }
        return (count - 1) * 2.4f;
    }

    /// <summary>
    /// [타입 2] 공중 작은 회전 가시: 40% 확률로 2연속 출현
    /// </summary>
    float SpawnSmallSpinningSpike(DifficultyConfig.TierData tier)
    {
        if (obstaclePrefab == null) return 0f;

        float[] yPositions = new float[] { 0f, 1.3f, -1.3f, 2.0f, -2.0f };
        float targetY = yPositions[Random.Range(0, yPositions.Length)];
        float smallSize = Random.Range(0.5f, 0.68f);

        SpawnSingleSmallSpike(SPAWN_X, targetY, smallSize, tier.zoneName);

        // 40% 확률로 2연속 스폰
        if (Random.value < 0.40f)
        {
            float targetY2 = yPositions[Random.Range(0, yPositions.Length)];
            SpawnSingleSmallSpike(SPAWN_X + 2.0f, targetY2, smallSize, tier.zoneName);
            return 2.0f;
        }

        return 0f;
    }

    void SpawnSingleSmallSpike(float x, float y, float size, string zoneName)
    {
        GameObject root = Instantiate(obstaclePrefab, new Vector3(x, y, 0f), Quaternion.identity);
        root.transform.localScale = new Vector3(size, size, 1.0f);

        var obs = root.GetComponent<Obstacle>();
        if (obs != null)
        {
            obs.rotateSpeed = (Random.value > 0.5f ? 1 : -1) * Random.Range(200f, 320f);
            if (Random.value < 0.35f)
            {
                obs.isMoving = true;
                obs.movingAmplitude = Random.Range(0.4f, 0.8f);
                obs.movingFrequency = Random.Range(2f, 3.5f);
            }
        }
        ApplyColor(root, zoneName);
    }

    /// <summary>
    /// [타입 3] 공중 회전 톱니바퀴 (Spinning Saw Blade): 45% 확률로 2단 톱니 출현
    /// </summary>
    float SpawnSawBlade(DifficultyConfig.TierData tier)
    {
        GameObject targetPrefab = sawPrefab != null ? sawPrefab : obstaclePrefab;
        if (targetPrefab == null) return 0f;

        float[] yPositions = new float[] { 0f, 1.4f, -1.4f, 2.2f, -2.2f };
        float targetY = yPositions[Random.Range(0, yPositions.Length)];
        float sawSize = Random.Range(0.7f, 1.05f);

        SpawnSingleSaw(targetPrefab, SPAWN_X, targetY, sawSize, tier.zoneName);

        // 45% 확률로 2연속 톱니 또는 상/하 톱니 배치
        if (Random.value < 0.45f)
        {
            float targetY2 = yPositions[Random.Range(0, yPositions.Length)];
            SpawnSingleSaw(targetPrefab, SPAWN_X + 2.2f, targetY2, sawSize, tier.zoneName);
            return 2.2f;
        }

        return 0f;
    }

    void SpawnSingleSaw(GameObject targetPrefab, float x, float y, float size, string zoneName)
    {
        GameObject root = Instantiate(targetPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        root.transform.localScale = new Vector3(size, size, 1.0f);

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
        ApplyColor(root, zoneName);
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