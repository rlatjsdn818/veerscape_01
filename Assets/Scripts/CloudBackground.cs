using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 배경 하늘에 무작위 구름(Clouds)을 띄우고 유유히 흘러가게 하는 배경 컴포넌트:
/// - 인스펙터에서 구름 스프라이트(PNG)들을 직접 원하는 만큼 등록 가능
/// - 하늘 영역(Y축 범위)에서 크기, 투명도, 위치를 무작위로 생성
/// - 카메라가 빠르게 질주해도 구름은 항상 느긋하고 평화로운 속도로 흐르도록 보정
/// - 화면 밖으로 벗어난 구름을 앞쪽으로 자동 순환(재배치)하여 무한 반복
/// </summary>
public class CloudBackground : MonoBehaviour
{
    [Header("Cloud Sprites (PNG를 여기에 드래그 앤 드롭)")]
    [Tooltip("사용할 구름 스프라이트 목록. 인스펙터에서 구름 PNG들을 넣어주세요.")]
    public Sprite[] cloudSprites;

    [Header("Cloud Speed (구름 흘러가는 속도)")]
    [Tooltip("구름이 화면 뒤로 흘러가는 속도. 0.2~0.4 추천 (수치를 낮출수록 아주 느긋하게 지나갑니다)")]
    [Range(0f, 2f)]
    public float cloudSpeed = 0.35f;

    [Header("Cloud Count & Area (개수 및 위치 범위)")]
    [Tooltip("화면에 동시에 떠 있을 구름의 개수")]
    [Range(3, 20)]
    public int cloudCount = 7;

    [Tooltip("구름이 떠 있을 최소 Y 높이 (하늘 영역)")]
    public float minY = 1.0f;

    [Tooltip("구름이 떠 있을 최대 Y 높이 (천장 영역)")]
    public float maxY = 4.0f;

    [Header("Size & Transparency (크기 및 투명도)")]
    [Tooltip("구름 최소 크기")]
    public float minScale = 0.5f;

    [Tooltip("구름 최대 크기")]
    public float maxScale = 1.2f;

    [Tooltip("구름 최소 불투명도 (은은한 배경감)")]
    [Range(0.1f, 1f)]
    public float minAlpha = 0.35f;

    [Tooltip("구름 최대 불투명도")]
    [Range(0.1f, 1f)]
    public float maxAlpha = 0.75f;

    [Header("Sorting Layer (정렬 순서)")]
    [Tooltip("스프라이트 정렬 순서. 천장/바닥보다 뒤에 오도록 -10 등 낮은 음수 지정")]
    public int sortingOrder = -10;

    // 내부 구름 오브젝트 풀
    private class CloudItem
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float currentScale;
    }

    private List<CloudItem> clouds = new List<CloudItem>();
    private Camera cam;
    private float prevCamX;

    void Start()
    {
        cam = Camera.main;
        if (cam != null) prevCamX = cam.transform.position.x;

        // 인스펙터에 스프라이트가 비어있는 경우 Sprites 폴더의 cloud PNG들을 자동 탐색해 기본 편의 제공
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
#if UNITY_EDITOR
            var autoList = new List<Sprite>();
            string[] defaultNames = new string[] { "cloud", "cloud02", "cloud03" };
            foreach (var name in defaultNames)
            {
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
                if (s != null) autoList.Add(s);
            }
            if (autoList.Count > 0)
            {
                cloudSprites = autoList.ToArray();
            }
#endif
        }

        InitializeClouds();
    }

    /// <summary>
    /// 게임 시작 시 구름들을 화면 전체에 미리 자연스럽게 흩뿌려 배치합니다.
    /// </summary>
    void InitializeClouds()
    {
        float camX = cam != null ? cam.transform.position.x : 0f;
        float screenLeft = camX - 16f;
        float screenRight = camX + 22f;

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloudObj = new GameObject($"Cloud_{i}");
            cloudObj.transform.SetParent(this.transform);

            var sr = cloudObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            CloudItem item = new CloudItem
            {
                obj = cloudObj,
                sr = sr,
                currentScale = 1f
            };

            // 화면 가로 범위에 골고루 초기 분산 배치
            float startX = Mathf.Lerp(screenLeft, screenRight, (float)i / cloudCount) + Random.Range(-1.5f, 1.5f);
            RandomizeCloud(item, startX);

            clouds.Add(item);
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
            prevCamX = cam.transform.position.x;
        }

        // 카메라 이동량 추적
        float camDeltaX = cam.transform.position.x - prevCamX;
        prevCamX = cam.transform.position.x;

        // ★ 핵심: 구름이 카메라를 따라가면서, 화면 기준으로는 오직 cloudSpeed 만큼만 은은하게 뒤로 흐름
        // (플레이어가 아무리 빠르게 질주해도 구름이 휭 날아가지 않고 항상 느긋하게 흐릅니다)
        float moveX = camDeltaX - (cloudSpeed * Time.deltaTime);

        float recycleThresholdX = cam.transform.position.x - 18f;
        float spawnAheadX = cam.transform.position.x + 22f;

        for (int i = 0; i < clouds.Count; i++)
        {
            var item = clouds[i];
            if (item.obj == null) continue;

            item.obj.transform.position += new Vector3(moveX, 0f, 0f);

            // 화면 왼쪽 뒤로 벗어나면 오른쪽 앞쪽으로 재배치 (무한 순환)
            if (item.obj.transform.position.x < recycleThresholdX)
            {
                float newX = spawnAheadX + Random.Range(0f, 6f);
                RandomizeCloud(item, newX);
            }
        }
    }

    /// <summary>
    /// 구름의 위치(Y), 크기, 불투명도, 스프라이트를 무작위로 설정합니다.
    /// </summary>
    void RandomizeCloud(CloudItem item, float posX)
    {
        float posY = Random.Range(minY, maxY);
        item.obj.transform.position = new Vector3(posX, posY, 0f);

        // 크기 랜덤
        float scale = Random.Range(minScale, maxScale);
        item.currentScale = scale;
        item.obj.transform.localScale = new Vector3(scale, scale, 1f);

        // 스프라이트 랜덤 선택
        if (cloudSprites != null && cloudSprites.Length > 0)
        {
            Sprite chosenSprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            item.sr.sprite = chosenSprite;
        }

        // 투명도(알파) 랜덤 (은은한 분위기)
        float alpha = Random.Range(minAlpha, maxAlpha);
        item.sr.color = new Color(1f, 1f, 1f, alpha);
        item.sr.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 인스펙터에서 수치를 바꿀 때 실시간 반영
    /// </summary>
    private void OnValidate()
    {
        if (minY > maxY) maxY = minY + 0.5f;
        if (minScale > maxScale) maxScale = minScale;
        if (minAlpha > maxAlpha) maxAlpha = minAlpha;

        foreach (var c in clouds)
        {
            if (c != null && c.sr != null)
            {
                c.sr.sortingOrder = sortingOrder;
            }
        }
    }
}
