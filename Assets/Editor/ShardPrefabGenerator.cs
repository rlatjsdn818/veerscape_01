#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ShardPrefabGenerator
{
    private const string PrefabPath = "Assets/Prefabs/ShardPrefab.prefab";

    [MenuItem("Veerscape/Generate Shard Prefab", false, 2)]
    public static void GeneratePrefab()
    {
        // 1. Root GameObject
        GameObject root = new GameObject("ShardPrefab");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // Shard Component
        Shard shard = root.AddComponent<Shard>();
        shard.shardValue = 1;
        shard.rotateSpeed = 180f;
        shard.floatAmplitude = 0.15f;
        shard.floatFrequency = 3.5f;

        // Trigger Collider (획득 판정)
        CircleCollider2D triggerCol = root.AddComponent<CircleCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.radius = 0.35f;

        // 2. Visual Child (노란색 빛나는 삼각형 샤드)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        // 크기: 보석 샤드 크기 (0.28배)
        visual.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();

        // 전용 샤드 스프라이트 로드 (화이트 크리스탈 -> 노란색 틴팅 완벽 반영)
        Sprite shardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/shard.png");
        if (shardSprite == null)
        {
            shardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/spike02.png");
        }
        sr.sprite = shardSprite;
        // 눈부시게 빛나는 선명한 골든 옐로우 (URP 블룸 발광)
        sr.color = new Color(1f, 0.94f, 0.15f, 1f);
        sr.sortingOrder = 5; // 배경보다 앞, 장애물과 동등 레벨로 렌더링

        // 3. 프리팹 에셋으로 저장
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ShardPrefabGenerator] '{PrefabPath}' 생성 완료!");
    }
}
#endif
