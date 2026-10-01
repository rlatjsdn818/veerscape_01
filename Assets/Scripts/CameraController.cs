using UnityEngine;

/// <summary>
/// 카메라 쉐이크 및 Near Miss 줌 효과 제어.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Zoom Settings")]
    public float defaultOrthoSize = 5f;
    public float nearMissZoomSize = 4.5f;
    public float boostZoomSize = 5.5f;
    public float zoomLerpSpeed = 5f;
    public float baseScrollSpeed = 5f;

    [Header("Shake Settings")]
    public float nearMissShakeIntensity = 0.1f;
    public float nearMissShakeDuration = 0.15f;
    public float deathShakeIntensity = 0.3f;
    public float deathShakeDuration = 0.3f;
    public float shakeFrequency = 15f;

    private Vector3 basePosition = new Vector3(0, 0, -10);
    
    private float currentShakeIntensity;
    private float shakeTimer;
    private float targetOrthoSize;
    private float nearMissZoomTimer;
    private float initialShakeDuration;

    private Camera cam;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        transform.position = basePosition;
        targetOrthoSize = defaultOrthoSize;

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

    private Transform target;

    void Update()
    {
        if (target == null)
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) target = p.transform;
        }
        else
        {
            // 플레이어가 -6에서 시작하므로, 카메라가 0에서 시작하도록 offset을 +6 둡니다.
            basePosition = new Vector3(target.position.x + 6f, 0, -10f);
        }

        HandleZoom();
        HandleShake();
    }

    private void HandleZoom()
    {
        if (cam == null) return;

        // Determine target ortho size
        if (nearMissZoomTimer > 0)
        {
            targetOrthoSize = nearMissZoomSize;
            nearMissZoomTimer -= Time.unscaledDeltaTime;
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentScrollSpeed > baseScrollSpeed)
        {
            targetOrthoSize = boostZoomSize;
        }
        else
        {
            targetOrthoSize = defaultOrthoSize;
        }

        // Smooth transition to target size
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Time.unscaledDeltaTime * zoomLerpSpeed);
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime;

            // Perlin noise shake based on unscaled time
            float xNoise = Mathf.PerlinNoise(Time.unscaledTime * shakeFrequency, 0f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(0f, Time.unscaledTime * shakeFrequency) * 2f - 1f;

            // Optional fade out
            float fadeProgress = Mathf.Clamp01(shakeTimer / initialShakeDuration);
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
        Shake(nearMissShakeIntensity, nearMissShakeDuration);
        nearMissZoomTimer = 0.5f; // Near Miss 줌
    }

    void HandleGameOver()
    {
        Shake(deathShakeIntensity, deathShakeDuration);
    }

    void HandleGameStart()
    {
        shakeTimer = 0f;
        nearMissZoomTimer = 0f;
        transform.position = basePosition;
        if (cam != null) cam.orthographicSize = defaultOrthoSize;
    }
}
