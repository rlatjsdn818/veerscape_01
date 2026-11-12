using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 맵 곳곳에 빛나는 노란색 샤드(코인) 패턴을 동적으로 생성하는 스포너:
/// 1. 아크(Arc) 패턴: 플레이어의 바운스 궤적을 안내하는 포물선형 샤드 무리 (3~5개)
/// 2. 직선 스트림(Line) 패턴: 안전한 중앙 통로를 지나는 수평 샤드 라인 (3~5개)
/// 3. 리스크-리턴(Risk) 패턴: 가시 위쪽 등 아슬아슬한 위치에 놓이는 고가치 보너스 샤드
/// </summary>
public class ShardSpawner : MonoBehaviour
{
    [Header("Prefab Reference")]
    [Tooltip("생성할 샤드(코인) 프리팹 (미할당 시 자동 탐색)")]
    [SerializeField] private GameObject shardPrefab;

    [Header("Spawn Settings")]
    [Tooltip("샤드 패턴 스폰 간격 (초)")]
    public float minSpawnInterval = 1.8f;
    public float maxSpawnInterval = 3.2f;

    [Tooltip("샤드 패턴 스폰 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float spawnChance = 0.85f;

    private float spawnTimer;
    private GameManager gm;
    private List<GameObject> activeShards = new List<GameObject>();

    private float SPAWN_X => Camera.main != null ? Camera.main.transform.position.x + 22f : 20f;
    private const float TOP_BOUND = 3.6f;
    private const float BOTTOM_BOUND = -3.6f;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += ResetShards;
            gm.OnGameOver += ResetShards;
        }

        if (shardPrefab == null)
        {
#if UNITY_EDITOR
            shardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShardPrefab.prefab");
#endif
        }

        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= ResetShards;
            gm.OnGameOver -= ResetShards;
        }
    }

    void Update()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            if (Random.value <= spawnChance && shardPrefab != null)
            {
                SpawnRandomPattern();
            }
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    void SpawnRandomPattern()
    {
        int patternType = Random.Range(0, 3);
        switch (patternType)
        {
            case 0:
                SpawnArcPattern();
                break;
            case 1:
                SpawnLinePattern();
                break;
            case 2:
                SpawnHighRiskCluster();
                break;
        }
    }

    /// <summary>
    /// 포물선형(아크) 바운스 유도 샤드 라인 (3~5개)
    /// </summary>
    void SpawnArcPattern()
    {
        int count = Random.Range(3, 6);
        bool goingUp = Random.value > 0.5f;
        float startY = goingUp ? BOTTOM_BOUND + 0.8f : TOP_BOUND - 0.8f;
        float endY = goingUp ? TOP_BOUND - 0.8f : BOTTOM_BOUND + 0.8f;
        float spacingX = 1.6f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            // 부드러운 사인 곡선 보간
            float y = Mathf.Lerp(startY, endY, Mathf.Sin(t * Mathf.PI * 0.5f));
            CreateShard(SPAWN_X + i * spacingX, y);
        }
    }

    /// <summary>
    /// 수평 직선 샤드 스트림 (3~5개)
    /// </summary>
    void SpawnLinePattern()
    {
        int count = Random.Range(3, 6);
        float[] yCandidates = new float[] { -1.6f, 0f, 1.6f };
        float y = yCandidates[Random.Range(0, yCandidates.Length)];
        float spacingX = 1.4f;

        for (int i = 0; i < count; i++)
        {
            CreateShard(SPAWN_X + i * spacingX, y);
        }
    }

    /// <summary>
    /// 고위험 보너스 샤드 (상단이나 하단 위험 구역에 단독 또는 2개 배치)
    /// </summary>
    void SpawnHighRiskCluster()
    {
        float y = Random.value > 0.5f ? TOP_BOUND - 0.4f : BOTTOM_BOUND + 0.4f;
        CreateShard(SPAWN_X, y);
        if (Random.value < 0.5f)
        {
            CreateShard(SPAWN_X + 1.2f, y);
        }
    }

    void CreateShard(float x, float y)
    {
        if (shardPrefab == null) return;
        GameObject shard = Instantiate(shardPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        activeShards.Add(shard);
    }

    public void ResetShards()
    {
        for (int i = activeShards.Count - 1; i >= 0; i--)
        {
            if (activeShards[i] != null)
            {
                Destroy(activeShards[i]);
            }
        }
        activeShards.Clear();
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}
