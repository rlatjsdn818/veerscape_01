using UnityEngine;

/// <summary>
/// 거리(m) 기반 난이도 구간별 파라미터 정의
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
        // 0~150m: 기본 구간 (BASIC)
        new TierData
        {
            zoneName = "BASIC",
            minDistance = 0f,
            scrollSpeed = 5f,
            verticalSpeed = 4f,
            spawnIntervalMin = 2.0f,
            spawnIntervalMax = 3.0f,
            gapSizeMin = 4.0f,
            gapSizeMax = 5.0f,
            obstacleHeightMin = 1.5f,
            obstacleHeightMax = 2.5f,
            doubleObstacles = false,
            corridors = false,
            zigzag = false,
            randomMix = false
        },
        // 150~300m: SHIFT
        new TierData
        {
            zoneName = "SHIFT",
            minDistance = 150f,
            scrollSpeed = 7f,
            verticalSpeed = 5f,
            spawnIntervalMin = 1.5f,
            spawnIntervalMax = 2.5f,
            gapSizeMin = 3.5f,
            gapSizeMax = 4.5f,
            obstacleHeightMin = 2.0f,
            obstacleHeightMax = 3.0f,
            doubleObstacles = true,
            corridors = false,
            zigzag = false,
            randomMix = false
        },
        // 300~500m: FRACTURE
        new TierData
        {
            zoneName = "FRACTURE",
            minDistance = 300f,
            scrollSpeed = 9f,
            verticalSpeed = 6f,
            spawnIntervalMin = 1.2f,
            spawnIntervalMax = 2.0f,
            gapSizeMin = 3.0f,
            gapSizeMax = 4.0f,
            obstacleHeightMin = 2.0f,
            obstacleHeightMax = 3.5f,
            doubleObstacles = true,
            corridors = true,
            zigzag = false,
            randomMix = false
        },
        // 500~800m: DISTORT
        new TierData
        {
            zoneName = "DISTORT",
            minDistance = 500f,
            scrollSpeed = 11f,
            verticalSpeed = 7f,
            spawnIntervalMin = 1.0f,
            spawnIntervalMax = 1.5f,
            gapSizeMin = 2.5f,
            gapSizeMax = 3.5f,
            obstacleHeightMin = 2.5f,
            obstacleHeightMax = 4.0f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = false
        },
        // 800m+: VOID
        new TierData
        {
            zoneName = "VOID",
            minDistance = 800f,
            scrollSpeed = 13f,
            verticalSpeed = 8f,
            spawnIntervalMin = 0.8f,
            spawnIntervalMax = 1.2f,
            gapSizeMin = 2.2f,
            gapSizeMax = 3.0f,
            obstacleHeightMin = 2.5f,
            obstacleHeightMax = 4.5f,
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
