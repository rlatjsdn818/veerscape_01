#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class BigSpikePrefabGenerator
{
    private const string PrefabPath = "Assets/Prefabs/BigSpikePrefab.prefab";

    [MenuItem("Veerscape/Generate Big Spike Prefab", false, 1)]
    public static void GeneratePrefab()
    {
        // 1. Root GameObject
        GameObject root = new GameObject("BigSpikePrefab");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // Obstacle Component
        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.rotateSpeed = 0f;
        obstacle.isMoving = false;

        // Near Miss Trigger (외곽 다각형 콜라이더)
        PolygonCollider2D nearMissCol = root.AddComponent<PolygonCollider2D>();
        nearMissCol.isTrigger = true;
        Vector2[] nearMissPoints = new Vector2[]
        {
            new Vector2(0f, 0.72f),       // 상단 꼭짓점 여유 공간 (Near Miss 감지)
            new Vector2(0.82f, -0.50f),    // 우하단 외곽
            new Vector2(-0.82f, -0.50f)    // 좌하단 외곽
        };
        nearMissCol.SetPath(0, nearMissPoints);

        // 2. Visual Child
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        // spike02 스프라이트 원본(534x406)을 높이 1.0 유닛에 딱 맞추는 스케일 (1 / 4.06 = 0.2463)
        visual.transform.localScale = new Vector3(0.2463f, 0.2463f, 1f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        // spike02_0 스프라이트 로드
        Sprite spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/spike02.png");
        if (spikeSprite == null)
        {
            // fallback: 서브 애셋 탐색
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/spike02.png");
            foreach (var asset in subAssets)
            {
                if (asset is Sprite s)
                {
                    spikeSprite = s;
                    break;
                }
            }
        }
        sr.sprite = spikeSprite;
        sr.color = Color.white;

        // 3. KillZone Child
        GameObject killZone = new GameObject("KillZone");
        killZone.transform.SetParent(root.transform, false);
        killZone.transform.localPosition = Vector3.zero;
        killZone.transform.localRotation = Quaternion.identity;
        killZone.transform.localScale = Vector3.one;

        killZone.AddComponent<ObstacleKillZone>();

        // 실제 가시 모양에 맞춘 정밀 삼각 판정 (PolygonCollider2D)
        PolygonCollider2D killCol = killZone.AddComponent<PolygonCollider2D>();
        killCol.isTrigger = true;
        Vector2[] killPoints = new Vector2[]
        {
            new Vector2(0f, 0.46f),       // 꼭짓점 살짝 아래 (억울한 죽음 방지)
            new Vector2(0.58f, -0.48f),   // 우하단
            new Vector2(-0.58f, -0.48f)   // 좌하단
        };
        killCol.SetPath(0, killPoints);

        // 4. 프리팹 에셋으로 저장
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BigSpikePrefabGenerator] '{PrefabPath}' 생성 완료!");
    }
}
#endif
