using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 플레이어 캐릭터 제어: 수직 자동 이동, 천장/바닥 바운스,
/// 클릭 시 Y축 방향 반전, 장애물 충돌 시 사망.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float verticalSpeed = 4f;
    private int verticalDirection = 1; // 1 = up, -1 = down

    [Header("Boundaries")]
    public float topBound = 4.2f;
    public float bottomBound = -4.2f;

    [Header("Visual")]
    [Tooltip("플레이어의 기본 크기. PNG 스프라이트에 맞게 조절하세요.")]
    public Vector3 normalScale = Vector3.one * 0.6f;

    private bool isDead = false;
    private GameManager gm;
    private float playStartTime;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += HandleGameStart;
            gm.OnGameOver += HandleGameOver;
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= HandleGameStart;
            gm.OnGameOver -= HandleGameOver;
        }
    }

    void HandleGameStart()
    {
        isDead = false;
        transform.position = new Vector3(-6f, 0f, 0f);
        verticalDirection = 1;
        transform.localScale = normalScale;
        playStartTime = Time.time;
    }

    void HandleGameOver()
    {
        isDead = true;
    }

    void Update()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing || isDead) return;

        // 클릭으로 방향 반전 (시작 직후 0.1초는 무시, UI 위 클릭도 무시)
        if (Input.GetMouseButtonDown(0) && Time.time - playStartTime > 0.1f
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            verticalDirection *= -1;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayClick();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnClickEffect(transform.position);
        }

        // 난이도에 따른 수직 속도
        var tier = DifficultyConfig.GetTier(gm.Distance);
        verticalSpeed = tier.verticalSpeed;

        // X축 앞으로 이동
        float newX = transform.position.x + gm.CurrentScrollSpeed * Time.deltaTime;

        // 수직 이동
        float newY = transform.position.y + verticalDirection * verticalSpeed * Time.deltaTime;

        // 천장/바닥 바운스
        if (newY >= topBound)
        {
            newY = topBound;
            verticalDirection = -1;
            OnBounce(new Vector3(newX, topBound, 0f));
        }
        else if (newY <= bottomBound)
        {
            newY = bottomBound;
            verticalDirection = 1;
            OnBounce(new Vector3(newX, bottomBound, 0f));
        }

        transform.position = new Vector3(newX, newY, 0f);
    }

    void OnBounce(Vector3 bouncePos)
    {
        if (VFXManager.Instance != null) VFXManager.Instance.SpawnBounceEffect(bouncePos);
        if (SFXManager.Instance != null) SFXManager.Instance.PlayBounce();
        StopAllCoroutines();
        StartCoroutine(SquashAnimation());
    }

    IEnumerator SquashAnimation()
    {
        float duration = 0.1f;
        float elapsed = 0f;
        // normalScale 기준으로 찌그러지는 비율 적용
        Vector3 squashed = new Vector3(normalScale.x * 1.33f, normalScale.y * 0.67f, normalScale.z);

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(squashed, normalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = normalScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || gm == null) return;

        // 사망 영역 (ObstacleKillZone) 충돌 체크
        var killZone = other.GetComponent<ObstacleKillZone>();
        if (killZone != null && !gm.IsInvincible)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        if (VFXManager.Instance != null) VFXManager.Instance.SpawnDeathEffect(transform.position);
        // 플레이어 메시 숨기기
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;
        gm.EndGame();
    }

    /// <summary>게임 리트라이 시 비주얼 리셋</summary>
    public void ResetVisual()
    {
        transform.position = new Vector3(-6f, 0f, 0f);
        transform.localScale = Vector3.one * 0.6f;
        isDead = false;
        verticalDirection = 1;
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;
    }
}
