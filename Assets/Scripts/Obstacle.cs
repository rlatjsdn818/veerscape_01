using UnityEngine;

/// <summary>
/// 장애물 오브젝트:
/// - 왼쪽으로 스크롤 이동 및 화면 밖 자동 제거
/// - 회전 옵션 (톱니바퀴 등)
/// - 상하 이동(진동) 옵션
/// - 루트에 Near Miss 영역 콜라이더(trigger) 보유
/// </summary>
public class Obstacle : MonoBehaviour
{
    /// <summary>이 장애물이 이미 Near Miss를 발동했는지 여부</summary>
    [HideInInspector] public bool hasTriggeredNearMiss = false;

    private GameManager gm;
    private const float DESTROY_X_OFFSET = 15f;

    [Header("Movement")]
    public bool isMoving = false;
    public float movingAmplitude = 1f;
    public float movingFrequency = 2f;

    [Header("Rotation (Saw Blade etc.)")]
    [Tooltip("초당 회전 속도 (도/초). 톱니바퀴는 360 또는 -360 등으로 회전")]
    public float rotateSpeed = 0f;

    private float startY;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null) gm.ActiveObstacles.Add(this);
        startY = transform.position.y;
    }

    void OnDestroy()
    {
        if (gm != null) gm.ActiveObstacles.Remove(this);
    }

    void Update()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return;

        // 1. 회전 애니메이션 (톱니바퀴 등)
        if (rotateSpeed != 0f)
        {
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                visual.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            }
            else
            {
                transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            }
        }

        // 2. 상하 진동 이동
        if (isMoving)
        {
            float moveY = Mathf.Sin(Time.time * movingFrequency) * movingAmplitude;
            transform.position = new Vector3(
                transform.position.x,
                startY + moveY,
                transform.position.z
            );
        }

        // 3. 화면 밖이면 제거 (카메라 기준)
        if (Camera.main != null && transform.position.x < Camera.main.transform.position.x - DESTROY_X_OFFSET)
        {
            Destroy(gameObject);
        }
    }
}
