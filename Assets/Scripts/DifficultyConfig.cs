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
        // 0~400m: 기본 구간 (BASIC) - 여유롭게 기본 조작과 바운스 리듬에 적응하는 단계
        new TierData
        {
            zoneName = "BASIC",
            minDistance = 0f,
            scrollSpeed = 5.0f,
            verticalSpeed = 4.0f,
            spawnIntervalMin = 2.2f,
            spawnIntervalMax = 3.5f,
            gapSizeMin = 4.0f,
            gapSizeMax = 5.0f,
            obstacleHeightMin = 1.5f,
            obstacleHeightMax = 2.5f,
            doubleObstacles = false,
            corridors = false,
            zigzag = false,
            randomMix = false
        },
        // 400~900m: SHIFT - 더블 가시와 회전 톱니바퀴가 서서히 등장하며 긴장감이 시작되는 단계
        new TierData
        {
            zoneName = "SHIFT",
            minDistance = 400f,
            scrollSpeed = 6.5f,
            verticalSpeed = 4.8f,
            spawnIntervalMin = 1.8f,
            spawnIntervalMax = 2.8f,
            gapSizeMin = 3.6f,
            gapSizeMax = 4.6f,
            obstacleHeightMin = 1.8f,
            obstacleHeightMax = 2.8f,
            doubleObstacles = true,
            corridors = false,
            zigzag = false,
            randomMix = false
        },
        // 900~1500m: FRACTURE - 복도(Corridor) 패턴과 함께 속도감이 한 단계 오르는 단계
        new TierData
        {
            zoneName = "FRACTURE",
            minDistance = 900f,
            scrollSpeed = 8.0f,
            verticalSpeed = 5.6f,
            spawnIntervalMin = 1.5f,
            spawnIntervalMax = 2.3f,
            gapSizeMin = 3.4f,
            gapSizeMax = 4.2f,
            obstacleHeightMin = 2.0f,
            obstacleHeightMax = 3.2f,
            doubleObstacles = true,
            corridors = true,
            zigzag = false,
            randomMix = false
        },
        // 1500~2300m: DISTORT - 지그재그 패턴과 무빙 톱니바퀴가 출현하는 숙련자 코스
        new TierData
        {
            zoneName = "DISTORT",
            minDistance = 1500f,
            scrollSpeed = 9.5f,
            verticalSpeed = 6.4f,
            spawnIntervalMin = 1.3f,
            spawnIntervalMax = 1.9f,
            gapSizeMin = 3.2f,
            gapSizeMax = 3.8f,
            obstacleHeightMin = 2.2f,
            obstacleHeightMax = 3.5f,
            doubleObstacles = true,
            corridors = true,
            zigzag = true,
            randomMix = false
        },
        // 2300m+: VOID - 모든 복합 패턴이 섞여 나오는 궁극의 마스터 챌린지 구간
        new TierData
        {
            zoneName = "VOID",
            minDistance = 2300f,
            scrollSpeed = 11.5f,
            verticalSpeed = 7.2f,
            spawnIntervalMin = 1.0f,
            spawnIntervalMax = 1.5f,
            gapSizeMin = 3.0f,
            gapSizeMax = 3.6f,
            obstacleHeightMin = 2.5f,
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
