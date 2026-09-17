using UnityEngine;

/// <summary>
/// 카메라 쉐이크 및 Near Miss 줌 효과 제어.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private Vector3 originalPosition;
    private float shakeTimer;
    private float shakeMagnitude;

    private Camera cam;
    private float originalSize;
    private float zoomTimer;
    private float targetSize;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        originalPosition = transform.position;
        originalSize = cam != null ? cam.orthographicSize : 5f;
        targetSize = originalSize;

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

    void HandleNearMiss(int combo)
    {
        Shake(0.15f + combo * 0.05f, 0.15f);
        // Near Miss 시 카메라 살짝 줌아웃 (시야 확보)
        targetSize = originalSize + 0.5f;
        zoomTimer = 0.5f;
    }

    void HandleGameOver()
    {
        Shake(0.4f, 0.3f);
    }

    void HandleGameStart()
    {
        if (cam != null) cam.orthographicSize = originalSize;
        targetSize = originalSize;
        zoomTimer = 0f;
        shakeTimer = 0f;
        transform.position = originalPosition;
    }

    public void Shake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeTimer = duration;
    }

    void Update()
    {
        // 카메라 쉐이크
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
            float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = new Vector3(
                originalPosition.x + offsetX,
                originalPosition.y + offsetY,
                originalPosition.z);
        }
        else
        {
            transform.position = originalPosition;
        }

        // 줌 효과
        if (cam == null) return;
        if (zoomTimer > 0f)
        {
            zoomTimer -= Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * 5f);
        }
        else
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, originalSize, Time.deltaTime * 3f);
        }
    }
}
