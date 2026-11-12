using UnityEngine;

/// <summary>
/// 맵 곳곳에 배치되는 노란색 빛나는 삼각형 샤드(코인).
/// 플레이어 접촉 시 샤드 수량 증가, 경쾌한 차임 사운드 및 이펙트 출력 후 제거됩니다.
/// </summary>
public class Shard : MonoBehaviour
{
    [Header("Value")]
    [Tooltip("획득 시 지급되는 샤드(코인) 수량")]
    public int shardValue = 1;

    [Header("Visual & Motion")]
    [Tooltip("초당 자전 회전 속도 (도/초)")]
    public float rotateSpeed = 180f;

    [Tooltip("상하 부유 진폭 (둥둥 떠다니는 높이)")]
    public float floatAmplitude = 0.15f;

    [Tooltip("상하 부유 주기 속도")]
    public float floatFrequency = 3.5f;

    private float startY;
    private float floatPhase;
    private bool isCollected = false;

    void Start()
    {
        startY = transform.position.y;
        // 여러 샤드가 동시에 로봇처럼 똑같이 움직이지 않도록 랜덤 위상 부여
        floatPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (isCollected) return;

        // 1. 반짝이며 천천히 회전
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // 2. 부드러운 상하 부유 운동 (플로팅)
        float newY = startY + Mathf.Sin(Time.time * floatFrequency + floatPhase) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 3. 카메라 왼쪽으로 지나가면 자동 정리
        if (Camera.main != null && transform.position.x < Camera.main.transform.position.x - 15f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        // 플레이어 접촉 감지
        var player = other.GetComponent<PlayerController>();
        if (player != null || other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectShard(shardValue);
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayShardCollect();
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnShardCollectEffect(transform.position);
        }

        Destroy(gameObject);
    }
}
