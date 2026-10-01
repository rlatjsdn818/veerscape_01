using UnityEngine;
using System.Collections.Generic;

public class BackgroundController : MonoBehaviour
{
    private Sprite whiteSprite;
    
    [Header("Bounds & Spacing")]
    public float spawnX = 12f;
    public float destroyX = -12f;
    public float topY = 4.2f;
    public float bottomY = -4.2f;
    public float lineSpacing = 3f;
    
    private List<Transform> gridLines = new List<Transform>();
    private float distanceSinceLastSpawn = 0f;
    
    [Header("Colors")]
    public float colorTransitionSpeed = 1f;
    private Color currentColor;
    private Color targetColor;

    // Optional Ambient Particles
    private ParticleSystem ambientParticles;

    private void Awake()
    {
        CreateProceduralAssets();
        CreateAmbientParticles();

        SetZoneColor("BASIC");
        currentColor = targetColor;
        UpdateColors();
        
        // Pre-warm grid lines
        float startX = Camera.main != null ? Camera.main.transform.position.x - 15f : -15f;
        float endX = Camera.main != null ? Camera.main.transform.position.x + 20f : 20f;
        for (float x = startX; x <= endX; x += lineSpacing)
        {
            SpawnVerticalLineAt(x);
        }
    }
    
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnZoneChanged += HandleZoneChanged;
            GameManager.Instance.OnGameStart += HandleGameStart;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnZoneChanged -= HandleZoneChanged;
            GameManager.Instance.OnGameStart -= HandleGameStart;
        }
    }

    private void HandleGameStart()
    {
        // 게임 시작 시 그리드 라인 리셋
        for (int i = gridLines.Count - 1; i >= 0; i--)
        {
            if (gridLines[i] != null) Destroy(gridLines[i].gameObject);
        }
        gridLines.Clear();
        distanceSinceLastSpawn = 0f;

        SetZoneColor("BASIC");
        currentColor = targetColor;
        UpdateColors();

        float startX = Camera.main != null ? Camera.main.transform.position.x - 15f : -15f;
        float endX = Camera.main != null ? Camera.main.transform.position.x + 20f : 20f;
        for (float x = startX; x <= endX; x += lineSpacing)
        {
            SpawnVerticalLineAt(x);
        }
    }

    private void CreateProceduralAssets()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private SpriteRenderer CreateLine(Vector2 position, Vector2 size)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.position = position;
        lineObj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = lineObj.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingOrder = -100;
        return sr;
    }

    private void CreateAmbientParticles()
    {
        GameObject psObj = new GameObject("AmbientParticles");
        psObj.transform.SetParent(transform);
        psObj.transform.position = Vector3.zero;
        
        ambientParticles = psObj.AddComponent<ParticleSystem>();
        var main = ambientParticles.main;
        main.loop = true;
        main.startLifetime = 5f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = ambientParticles.emission;
        emission.rateOverTime = 10f;
        
        var shape = ambientParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25f, 10f, 1f);
        
        var renderer = ambientParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        
        ambientParticles.Play();
    }

    // Called via Unity Event or GameManager
    public void HandleZoneChanged(string zoneName)
    {
        SetZoneColor(zoneName);
    }

    private void SetZoneColor(string zoneName)
    {
        switch (zoneName)
        {
            case "BASIC": ColorUtility.TryParseHtmlString("#0A3030", out targetColor); break;
            case "SHIFT": ColorUtility.TryParseHtmlString("#300A30", out targetColor); break;
            case "FRACTURE": ColorUtility.TryParseHtmlString("#1A0A30", out targetColor); break;
            case "DISTORT": ColorUtility.TryParseHtmlString("#301A0A", out targetColor); break;
            case "VOID": ColorUtility.TryParseHtmlString("#300A0A", out targetColor); break;
            default: ColorUtility.TryParseHtmlString("#0A3030", out targetColor); break;
        }
    }

    private void Update()
    {
        if (ambientParticles != null && Camera.main != null)
        {
            ambientParticles.transform.position = new Vector3(Camera.main.transform.position.x, 0, 0);
        }
        
        UpdateColorTransition();
        
        // We'll safely check if GameManager exists and if state is Playing
        bool isPlaying = false;
        float currentSpeed = 0f;

        if (GameManager.Instance != null)
        {
            isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
            currentSpeed = GameManager.Instance.CurrentScrollSpeed;
        }

        if (isPlaying && currentSpeed > 0f)
        {
            ScrollLines(currentSpeed);
            HandleSpawning(currentSpeed);
            UpdateParticles(currentSpeed);
        }
    }

    private void UpdateColorTransition()
    {
        if (currentColor != targetColor)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            UpdateColors();
        }
    }

    private void UpdateColors()
    {
        Color gridColor = currentColor;
        gridColor.a = 0.08f;
        for (int i = 0; i < gridLines.Count; i++)
        {
            if (gridLines[i] != null)
            {
                gridLines[i].GetComponent<SpriteRenderer>().color = gridColor;
            }
        }

        if (ambientParticles != null)
        {
            var main = ambientParticles.main;
            Color pColor = currentColor;
            pColor.a = 0.3f;
            main.startColor = pColor;
        }
    }

    private void ScrollLines(float speed)
    {
        float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;
        for (int i = gridLines.Count - 1; i >= 0; i--)
        {
            Transform line = gridLines[i];
            if (line == null)
            {
                gridLines.RemoveAt(i);
                continue;
            }
            
            // 더 이상 왼쪽으로 이동하지 않습니다 (플레이어가 앞으로 이동하므로)
            // 카메라보다 훨씬 뒤처지면 삭제
            if (line.position.x < camX - 15f)
            {
                Destroy(line.gameObject);
                gridLines.RemoveAt(i);
            }
        }
    }

    private void HandleSpawning(float speed)
    {
        distanceSinceLastSpawn += speed * Time.deltaTime;
        if (distanceSinceLastSpawn >= lineSpacing)
        {
            float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;
            SpawnVerticalLineAt(camX + 20f);
            distanceSinceLastSpawn -= lineSpacing;
        }
    }

    private void SpawnVerticalLineAt(float xPos)
    {
        SpriteRenderer sr = CreateLine(new Vector2(xPos, 0), new Vector2(0.02f, 10f)); // 1 unit = 100px roughly, scale very thin
        Color gridColor = currentColor;
        gridColor.a = 0.08f;
        sr.color = gridColor;
        gridLines.Add(sr.transform);
    }

    private void UpdateParticles(float speed)
    {
        if (ambientParticles != null)
        {
            // Particles don't need to manually move if simulation space is World,
            // but if we want a parallax effect against the moving camera,
            // we can slightly move them right so they appear to move slower than the background.
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ambientParticles.main.maxParticles];
            int numParticlesAlive = ambientParticles.GetParticles(particles);
            
            float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;
            
            for (int i = 0; i < numParticlesAlive; i++)
            {
                particles[i].position += Vector3.right * (speed * 0.5f) * Time.deltaTime; // Parallax effect
                
                // Wrap particles ahead if they fall too far behind
                if (particles[i].position.x < camX - 15f)
                {
                    Vector3 pos = particles[i].position;
                    pos.x += 35f;
                    particles[i].position = pos;
                }
            }
            ambientParticles.SetParticles(particles, numParticlesAlive);
        }
    }
}
