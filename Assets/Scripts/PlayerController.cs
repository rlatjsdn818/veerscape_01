using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 플레이어 캐릭터 제어:
/// - 평상시: 바닥 또는 천장에서 농구공/바운스볼처럼 통통 바운스
/// - 클릭 시: 중력 반전 (바닥 중력 <-> 천장 중력)으로 궤도를 틀어 반대편으로 이동
/// - 전방 부스트 시 수직 물리 속도와 시간을 비례 동기화하여 궤적 꺾임(Kink) 방지
/// - 스프라이트 및 히트박스(콜라이더) 크기 제어
/// - 장애물 충돌 시 사망
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement & Speed Settings")]
    [Tooltip("전체 수직 속도/템포 배율. 값이 클수록 이동과 바운스가 빨라집니다.")]
    [Range(0.5f, 3f)]
    public float speedMultiplier = 1.6f;

    [Tooltip("바운스 높이 (바닥/천장에서 튀어오르는 높이)")]
    public float bounceHeight = 2.0f;

    [Tooltip("중력 가속도 기본값 (난이도 및 속도 배율과 곱해짐)")]
    public float baseGravity = 45f;

    [Header("Boundaries")]
    public float topBound = 4.2f;
    public float bottomBound = -4.2f;

    [Header("Visual & Hitbox (Size)")]
    [Tooltip("플레이어 스프라이트의 기본 크기. 0.35~0.4 권장 (기존 0.6에서 축소)")]
    public Vector3 normalScale = Vector3.one * 0.38f;

    [Tooltip("히트박스(콜라이더) 크기 배율. 1.0이면 기본, 0.8이면 살짝 빗겨맞아도 살려주는 너그러운 판정.")]
    [Range(0.3f, 1.5f)]
    public float hitboxScale = 0.85f;

    // 현재 수직 속도
    private float verticalVelocity = 0f;

    // 중력 방향: -1 = 바닥(아래) 중력, 1 = 천장(위) 중력
    // 바닥/천장에 닿아도 바뀌지 않으며, 플레이어가 클릭할 때만 반전됩니다.
    private int gravityDirection = -1;

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

        ApplyHitboxSize();
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= HandleGameStart;
            gm.OnGameOver -= HandleGameOver;
        }
    }

    /// <summary>
    /// 인스펙터의 hitboxScale 값에 맞추어 콜라이더 크기를 갱신합니다.
    /// </summary>
    public void ApplyHitboxSize()
    {
        var col = GetComponent<Collider2D>();
        if (col is BoxCollider2D boxCol)
        {
            boxCol.size = new Vector2(hitboxScale, hitboxScale);
        }
        else if (col is CircleCollider2D circleCol)
        {
            circleCol.radius = 0.5f * hitboxScale;
        }
    }

    void HandleGameStart()
    {
        isDead = false;
        transform.position = new Vector3(-6f, 0f, 0f);
        gravityDirection = -1; // 시작 시 아래(바닥) 중력
        verticalVelocity = 0f;
        transform.localScale = normalScale;
        playStartTime = Time.time;
        ApplyHitboxSize();
    }

    void HandleGameOver()
    {
        isDead = true;
    }

    void Update()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing || isDead) return;

        // 1. 등록된 키(좌클릭, 스페이스바 등)로 중력 반전 (궤도 전환)
        // 시작 직후 0.1초 및 UI 터치는 무시
        bool inputTriggered = false;
        if (KeybindManager.Instance != null)
        {
            inputTriggered = KeybindManager.Instance.IsActionTriggered();
        }
        else
        {
            inputTriggered = (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        }

        if (inputTriggered && Time.time - playStartTime > 0.1f)
        {
            // 중력 방향 반전 (-1 <-> 1)
            gravityDirection *= -1;

            // 클릭 시 반전된 중력 방향으로 즉각적인 추진력을 부여해 반응성을 높임
            var tierData = DifficultyConfig.GetTier(gm.Distance);
            float baseSpeed = tierData.verticalSpeed * speedMultiplier;
            verticalVelocity = gravityDirection * Mathf.Max(Mathf.Abs(verticalVelocity), baseSpeed * 1.2f);

            if (SFXManager.Instance != null) SFXManager.Instance.PlayClick();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnClickEffect(transform.position);
        }

        // 2. 난이도 및 배율 기반 물리 파라미터 계산
        var tier = DifficultyConfig.GetTier(gm.Distance);
        float baseScrollSpeed = tier.scrollSpeed * gm.scrollSpeedMultiplier;

        // 전방 부스트 비율 계산: 전방 속도가 평상시 대비 몇 배 빠른지 측정
        float boostRatio = (baseScrollSpeed > 0.001f) ? Mathf.Max(1f, gm.CurrentScrollSpeed / baseScrollSpeed) : 1f;

        // ★ 핵심: 전방 이동 속도가 빨라진 만큼 수직 물리 시간(effectiveDeltaTime)도 동일한 비율로 가속
        // 이로 인해 전방 가속 시 dy/dx(궤도 각도와 곡률)가 왜곡되거나 꺾이지 않고 매끄럽게 유지됩니다.
        float effectiveDeltaTime = Time.deltaTime * boostRatio;

        float currentSpeedScale = (tier.verticalSpeed / 4f) * speedMultiplier;
        float gravity = baseGravity * currentSpeedScale;
        float bounceSpeed = Mathf.Sqrt(2f * gravity * bounceHeight);

        // 3. 중력 가속도 적용 (부스트 동기화 시간 기준)
        verticalVelocity += gravityDirection * gravity * effectiveDeltaTime;

        // 최대 낙하 속도 제한 (터미널 벨로시티)
        float maxSpeed = bounceSpeed * 1.5f;
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxSpeed, maxSpeed);

        // 4. X축 전진 이동
        float newX = transform.position.x + gm.CurrentScrollSpeed * Time.deltaTime;

        // 5. Y축 이동 및 천장/바닥 바운스 (부스트 동기화 시간 기준)
        float newY = transform.position.y + verticalVelocity * effectiveDeltaTime;

        // 천장 충돌 체크
        if (newY >= topBound)
        {
            newY = topBound;
            // 위로 향하던 속도를 아래로 반사 (중력 방향은 유지)
            verticalVelocity = -bounceSpeed;
            OnBounce(new Vector3(newX, topBound, 0f));
        }
        // 바닥 충돌 체크
        else if (newY <= bottomBound)
        {
            newY = bottomBound;
            // 아래로 떨어지던 속도를 위로 반사 (중력 방향은 유지)
            verticalVelocity = bounceSpeed;
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
        float duration = 0.08f;
        float elapsed = 0f;
        // 찌그러지는 비율 (Y축 축소, X축 확대)
        Vector3 squashed = new Vector3(normalScale.x * 1.3f, normalScale.y * 0.7f, normalScale.z);

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
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;
        gm.EndGame();
    }

    /// <summary>게임 리트라이 시 비주얼 및 상태 리셋</summary>
    public void ResetVisual()
    {
        transform.position = new Vector3(-6f, 0f, 0f);
        transform.localScale = normalScale;
        isDead = false;
        gravityDirection = -1;
        verticalVelocity = 0f;
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;
        ApplyHitboxSize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 수치를 조절할 때 실시간 반영
        if (!Application.isPlaying)
        {
            transform.localScale = normalScale;
            ApplyHitboxSize();
        }
    }
#endif
}
