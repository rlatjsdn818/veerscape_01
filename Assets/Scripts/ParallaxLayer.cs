using UnityEngine;

/// <summary>
/// 패럴랙스 배경 레이어.
/// SpriteRenderer가 붙은 오브젝트에 이 컴포넌트를 추가하면,
/// 자동으로 타일링 복제본을 만들어 끊김 없이 무한 스크롤합니다.
///
/// [사용법]
/// 1. 빈 오브젝트 또는 Sprite 오브젝트를 만든다.
/// 2. SpriteRenderer에 원하는 PNG 스프라이트를 할당한다.
///    (Sprite의 Wrap Mode를 Repeat로 설정하면 더 깔끔합니다)
/// 3. 이 스크립트를 Add Component로 추가한다.
/// 4. Inspector에서 speedMultiplier를 조절한다.
///    - 0.1 = 아주 먼 배경 (느리게 움직임)
///    - 0.5 = 중간 거리
///    - 1.0 = 게임 속도와 동일
/// 5. 타이틀 화면에서도 움직이게 하려면 scrollOnTitle을 체크한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    public enum LayerDepth
    {
        Custom,
        Layer1_Foreground, // 앞쪽 (빠름)
        Layer2_Midground,  // 중간
        Layer3_Background, // 뒤쪽 (느림)
        Layer4_Sky         // 하늘 (카메라와 똑같이 이동)
    }

    [Header("Easy Layer Setup")]
    [Tooltip("원하는 층수를 선택하면 속도와 렌더링 순서가 자동으로 세팅됩니다.")]
    public LayerDepth presetLayer = LayerDepth.Custom;

    [Header("Parallax Settings")]
    [Tooltip("스크롤 속도 배율. 0에 가까울수록 먼 배경(느림), 1에 가까울수록 가까운 배경(빠름)")]
    [Range(0.0f, 1.5f)]
    public float speedMultiplier = 0.5f;

    [Header("Sorting & Tuning")]
    [Tooltip("Sorting Order를 직접 지정합니다. 낮을수록 뒤에 그려집니다 (예: -10)")]
    public int sortingOrder = -5;
    
    [Tooltip("타일 사이에 미세한 틈(Gap)이 보인다면 이 값을 약간 올려주세요 (예: 0.02)")]
    public float overlapCorrection = 0.02f;

    private SpriteRenderer sr;
    private float spriteWidth;

    // 타일링을 위한 복제본
    private Transform tileA;
    private Transform tileB;

    // 초기 위치 저장 (리셋용)
    private Vector3 initialPosition;

    // 상태 변화 감지용
    private GameManager.GameState lastKnownState = GameManager.GameState.Title;

    void OnValidate()
    {
        // 프리셋을 선택하면 자동으로 값 세팅
        if (presetLayer != LayerDepth.Custom)
        {
            switch (presetLayer)
            {
                case LayerDepth.Layer1_Foreground:
                    speedMultiplier = 0.8f;
                    sortingOrder = -1;
                    break;
                case LayerDepth.Layer2_Midground:
                    speedMultiplier = 0.5f;
                    sortingOrder = -2;
                    break;
                case LayerDepth.Layer3_Background:
                    speedMultiplier = 0.2f;
                    sortingOrder = -3;
                    break;
                case LayerDepth.Layer4_Sky:
                    speedMultiplier = 0.0f;
                    sortingOrder = -5;
                    break;
            }
        }
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;

        if (sr.sprite == null)
        {
            Debug.LogWarning($"[ParallaxLayer] '{gameObject.name}'에 스프라이트가 할당되지 않았습니다. 인스펙터에서 SpriteRenderer의 Sprite를 설정해주세요.");
            enabled = false;
            return;
        }

        // 초기 위치 기억
        initialPosition = transform.position;

        // 스프라이트의 월드 기준 가로 길이 계산
        spriteWidth = (sr.sprite.bounds.size.x * transform.localScale.x) - overlapCorrection;

        SetupTiling();
    }

    /// <summary>
    /// 타일을 초기 위치로 되돌립니다.
    /// </summary>
    void ResetTiles()
    {
        if (tileA != null)
        {
            tileA.position = initialPosition;
        }
        if (tileB != null)
        {
            tileB.position = new Vector3(
                initialPosition.x + spriteWidth,
                initialPosition.y,
                initialPosition.z
            );
        }
    }

    /// <summary>
    /// 원본 + 복제본 1개를 나란히 배치하여 무한 루프 타일링 구성
    /// </summary>
    void SetupTiling()
    {
        tileA = transform;

        GameObject clone = new GameObject(gameObject.name + "_Tile");
        clone.transform.SetParent(transform.parent);
        clone.transform.position = new Vector3(
            transform.position.x + spriteWidth,
            transform.position.y,
            transform.position.z
        );
        clone.transform.localScale = transform.localScale;

        SpriteRenderer cloneSR = clone.AddComponent<SpriteRenderer>();
        cloneSR.sprite = sr.sprite;
        cloneSR.color = sr.color;
        cloneSR.sortingOrder = sortingOrder;
        cloneSR.sortingLayerName = sr.sortingLayerName;
        cloneSR.drawMode = sr.drawMode;
        cloneSR.flipX = sr.flipX;
        cloneSR.flipY = sr.flipY;

        tileB = clone.transform;
    }

    void Update()
    {
        // 상태 변화 감지: 타이틀로 돌아오면 즉시 타일 리셋
        if (GameManager.Instance != null)
        {
            var currentState = GameManager.Instance.CurrentState;
            if (currentState == GameManager.GameState.Title && lastKnownState != GameManager.GameState.Title)
            {
                ResetTiles();
            }
            lastKnownState = currentState;
        }

        float scrollSpeed = GetCurrentScrollSpeed();
        if (scrollSpeed <= 0f) return;

        float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;

        float moveAmount = scrollSpeed * (1f - speedMultiplier) * Time.deltaTime;

        tileA.position += Vector3.right * moveAmount;
        tileB.position += Vector3.right * moveAmount;

        if (tileA.position.x < camX - spriteWidth)
        {
            tileA.position = new Vector3(
                tileB.position.x + spriteWidth,
                tileA.position.y,
                tileA.position.z
            );
        }

        if (tileB.position.x < camX - spriteWidth)
        {
            tileB.position = new Vector3(
                tileA.position.x + spriteWidth,
                tileB.position.y,
                tileB.position.z
            );
        }
    }

    float GetCurrentScrollSpeed()
    {
        if (GameManager.Instance == null) return 0f;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            return GameManager.Instance.CurrentScrollSpeed;
        }

        return 0f;
    }
}
