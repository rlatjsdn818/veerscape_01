using UnityEngine;

/// <summary>
/// 거리(m) 기반 난이도 구간별 파라미터 정의:
/// - 단계별 거리 갭을 넉넉하게 확장하여 조급함 없이 각 구간을 충분히 즐길 수 있도록 밸런싱
/// - 속도 및 스폰 난이도 상승 곡선을 완만하고 자연스럽게 조정
/// </summary>
public static class DifficultyConfig
{
    [System.Serializable]
    public struct TierData
    {
        public string zoneName;
        public float minDistance;
        public float scrollSpeed;
        public float verticalSpeed;
        public float spawnIntervalMin;
        public float spawnIntervalMax;
        public float gapSizeMin;
        public float gapSizeMax;
        public float obstacleHeightMin;
        public float obstacleHeightMax;
        public bool doubleObstacles;
        public bool corridors;
        public bool zigzag;
        public bool randomMix;
    }

    private static readonly TierData[] tiers = new TierData[]
    {
        // 0~400m: 기본 구간 (BASIC) - 기존 FRACTURE 수준의 높은 긴장감 (더블 가시, 복도 패턴 기본 적용)
        new TierData
        {
            zoneName = "BASIC",
            minDistance = 0f,
            scrollSpeed = 5.0f,
            verticalSpeed = 4.0f,
            spawnIntervalMin = 1.0f,
            spawnIntervalMax = 1.5f,
            gapSizeMin = 3.2f,
            gapSizeMax = 3.9f,
            obstacleHeightMin = 2.0f,
            obstacleHeightMax = 3.0f,
            doubleObstacles = true,
            corridors = true,
            zigzag = false,
            randomMix = false
        },
        // 400~900m: SHIFT - 지그재그 패턴 해금 및 장애물 빈도 추가 상승
        new TierData
        {
            zoneName = "SHIFT",
            minDistance = 400f,
            scrollSpeed = 6.5f,
            verticalSpeed = 4.8f,
            spawnIntervalMin = 0.85f,
            spawnIntervalMax = 1.3f,
            gapSizeMin = 2.9f,
            gapSizeMax = 3.6f,
            obstacleHeightMin = 2.2f,
            obstacleHeightMax = 3.4f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = false
        },
        // 900~1500m: FRACTURE - 복합 랜덤 패턴 활성화 및 간격 대폭 축소
        new TierData
        {
            zoneName = "FRACTURE",
            minDistance = 900f,
            scrollSpeed = 8.0f,
            verticalSpeed = 5.6f,
            spawnIntervalMin = 0.75f,
            spawnIntervalMax = 1.15f,
            gapSizeMin = 2.7f,
            gapSizeMax = 3.3f,
            obstacleHeightMin = 2.4f,
            obstacleHeightMax = 3.6f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = true
        },
        // 1500~2300m: DISTORT - 쉴 틈 없는 연속 장애물과 좁은 통로
        new TierData
        {
            zoneName = "DISTORT",
            minDistance = 1500f,
            scrollSpeed = 9.5f,
            verticalSpeed = 6.4f,
            spawnIntervalMin = 0.65f,
            spawnIntervalMax = 1.0f,
            gapSizeMin = 2.5f,
            gapSizeMax = 3.1f,
            obstacleHeightMin = 2.6f,
            obstacleHeightMax = 3.8f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = true
        },
        // 2300m+: VOID - 극한의 연속 밀집 탄막 패턴
        new TierData
        {
            zoneName = "VOID",
            minDistance = 2300f,
            scrollSpeed = 11.5f,
            verticalSpeed = 7.2f,
            spawnIntervalMin = 0.55f,
            spawnIntervalMax = 0.85f,
            gapSizeMin = 2.4f,
            gapSizeMax = 2.9f,
            obstacleHeightMin = 2.8f,
            obstacleHeightMax = 4.0f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = true
        }
    };

    /// <summary>
    /// 주어진 거리에 해당하는 난이도 구간 데이터를 반환합니다.
    /// </summary>
    public static TierData GetTier(float distance)
    {
        TierData result = tiers[0];
        for (int i = tiers.Length - 1; i >= 0; i--)
        {
            if (distance >= tiers[i].minDistance)
            {
                result = tiers[i];
                break;
            }
        }
        return result;
    }
}
