using UnityEngine;

/// <summary>
/// 장애물 오브젝트: 왼쪽으로 스크롤 이동, 화면 밖 자동 제거.
/// 루트에 Near Miss 영역 BoxCollider(trigger) 보유.
/// </summary>
public class Obstacle : MonoBehaviour
{
    /// <summary>이 장애물이 이미 Near Miss를 발동했는지 여부</summary>
    [HideInInspector] public bool hasTriggeredNearMiss = false;

    private GameManager gm;
    private const float DESTROY_X = -15f;

    public bool isMoving = false;
    public float movingAmplitude = 1f;
    public float movingFrequency = 2f;
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

        float moveY = 0f;
        if (isMoving)
        {
            moveY = Mathf.Sin(Time.time * movingFrequency) * movingAmplitude;
        }

        // 왼쪽으로 스크롤 + 상하 진동
        transform.position = new Vector3(
            transform.position.x - gm.CurrentScrollSpeed * Time.deltaTime,
            isMoving ? startY + moveY : transform.position.y,
            transform.position.z
        );

        // 화면 밖이면 제거
        if (transform.position.x < DESTROY_X)
        {
            Destroy(gameObject);
        }
    }
}
