using UnityEngine;

/// <summary>
/// 시각 이펙트 관리: 바운스 파티클, Near Miss 플래시, 사망 파편 폭발.
/// 모든 이펙트는 코드로 2D 프리미티브(Sprite)를 생성하여 구현.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private static Sprite _squareSprite;
    public static Sprite SquareSprite
    {
        get
        {
            if (_squareSprite == null)
            {
                Texture2D tex = new Texture2D(16, 16);
                Color[] colors = new Color[16 * 16];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            }
            return _squareSprite;
        }
    }

    private static Sprite _circleSprite;
    public static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null)
            {
                int res = 64;
                Texture2D tex = new Texture2D(res, res);
                Color[] colors = new Color[res * res];
                float radius = res / 2f;
                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                        colors[y * res + x] = dist <= radius ? Color.white : Color.clear;
                    }
                }
                tex.SetPixels(colors);
                tex.Apply();
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
            }
            return _circleSprite;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>천장/바닥 바운스 시 파티클 버스트</summary>
    public void SpawnBounceEffect(Vector3 position)
    {
        CreateParticleBurst(position, new Color(0f, 1f, 1f), 6, 0.3f);
    }

    /// <summary>Near Miss 성공 시 네온 플래시</summary>
    public void SpawnNearMissEffect(Vector3 position)
    {
        CreateParticleBurst(position, new Color(1f, 0.85f, 0f), 12, 0.5f);
    }

    /// <summary>사망 시 기하학적 파편 폭발</summary>
    public void SpawnDeathEffect(Vector3 position)
    {
        Color[] colors = {
            new Color(0f, 1f, 1f),    // 시안
            new Color(1f, 0.2f, 0.2f), // 빨강
            new Color(1f, 0f, 0.6f),   // 마젠타
            new Color(1f, 0.5f, 0f)    // 오렌지
        };

        for (int i = 0; i < 15; i++)
        {
            var go = new GameObject("DeathFragment");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.22f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Random.value > 0.5f ? SquareSprite : CircleSprite;
            sr.color = colors[Random.Range(0, colors.Length)];

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.linearVelocity = new Vector2(Random.Range(-6f, 6f), Random.Range(3f, 12f));
            rb.angularVelocity = Random.Range(-360f, 360f);

            go.AddComponent<ParticleFader>().lifetime = Random.Range(0.8f, 1.5f);
            Destroy(go, 2f);
        }
    }

    void CreateParticleBurst(Vector3 pos, Color color, int count, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Particle");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.04f, 0.12f) * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SquareSprite;
            sr.color = color;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = Random.insideUnitCircle * Random.Range(3f, 8f);
            rb.angularVelocity = Random.Range(-360f, 360f);

            go.AddComponent<ParticleFader>().lifetime = Random.Range(0.2f, 0.5f);
            Destroy(go, 1f);
        }
    }
}

/// <summary>
/// 파티클 조각의 페이드아웃 및 자동 제거.
/// </summary>
public class ParticleFader : MonoBehaviour
{
    public float lifetime = 0.5f;
    private float timer;
    private SpriteRenderer sr;
    private Color startColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (sr != null)
        {
            float alpha = Mathf.Max(0f, 1f - timer / lifetime);
            Color c = startColor;
            c.a = alpha;
            sr.color = c;
        }
        if (timer >= lifetime) Destroy(gameObject);
    }
}
