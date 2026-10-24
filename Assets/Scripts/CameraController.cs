using UnityEngine;

/// <summary>
/// 카메라 제어:
/// - 플레이어 위치 추적 (X축 오프셋 +6)
/// - 장애물 회피(Near Miss) 시 화면 줌 대신 일관되고 가벼운 쉐이크(Shake) 효과만 발생
/// - 게임 오버 시 사망 쉐이크
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Camera Settings")]
    [Tooltip("기본 카메라 시야 크기 (고정)")]
    public float defaultOrthoSize = 5f;

    [Header("Shake Settings")]
    [Tooltip("장애물(가시/톱니바퀴) Near Miss 시 가벼운 쉐이크 강도")]
    public float nearMissShakeIntensity = 0.08f;
    [Tooltip("Near Miss 쉐이크 지속 시간 (초)")]
    public float nearMissShakeDuration = 0.12f;

    [Tooltip("사망 시 쉐이크 강도")]
    public float deathShakeIntensity = 0.3f;
    [Tooltip("사망 쉐이크 지속 시간 (초)")]
    public float deathShakeDuration = 0.3f;
    public float shakeFrequency = 18f;

    private Vector3 basePosition = new Vector3(0, 0, -10);
    
    private float currentShakeIntensity;
    private float shakeTimer;
    private float initialShakeDuration;

    private Camera cam;
    private Transform target;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        transform.position = basePosition;
        if (cam != null) cam.orthographicSize = defaultOrthoSize;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss += HandleNearMiss;
            gm.OnGameOver += HandleGameOver;
            gm.OnGameStart += HandleGameStart;
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss -= HandleNearMiss;
            gm.OnGameOver -= HandleGameOver;
            gm.OnGameStart -= HandleGameStart;
        }
    }

    void Update()
    {
        if (target == null)
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) target = p.transform;
        }
        else
        {
            basePosition = new Vector3(target.position.x + 6f, 0, -10f);
        }

        HandleShake();
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime;

            float xNoise = Mathf.PerlinNoise(Time.unscaledTime * shakeFrequency, 0f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(0f, Time.unscaledTime * shakeFrequency) * 2f - 1f;

            float fadeProgress = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, initialShakeDuration));
            float actualIntensity = currentShakeIntensity * fadeProgress;

            Vector3 shakeOffset = new Vector3(xNoise, yNoise, 0f) * actualIntensity;
            transform.position = basePosition + shakeOffset;
        }
        else
        {
            transform.position = basePosition;
        }
    }

    public void Shake(float intensity, float duration)
    {
        currentShakeIntensity = intensity;
        shakeTimer = duration;
        initialShakeDuration = duration;
    }

    void HandleNearMiss(int combo)
    {
        // 화면 확대(줌) 효과를 완전히 제거하고, 가볍고 경쾌한 쉐이크만 발생
        Shake(nearMissShakeIntensity, nearMissShakeDuration);
    }

    void HandleGameOver()
    {
        Shake(deathShakeIntensity, deathShakeDuration);
    }

    void HandleGameStart()
    {
        shakeTimer = 0f;
        transform.position = basePosition;
        if (cam != null) cam.orthographicSize = defaultOrthoSize;
    }
}
