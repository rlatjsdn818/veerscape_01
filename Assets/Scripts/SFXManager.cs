using UnityEngine;

/// <summary>
/// 사운드 이펙트 관리: 런타임 AudioClip 생성으로 외부 에셋 없이 동작.
/// 바운스, 클릭, Near Miss 스우시, 사망 효과음.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource audioSource;
    private AudioClip bounceClip;
    private AudioClip clickClip;
    private AudioClip nearMissClip;
    private AudioClip deathClip;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>GameSetup에서 AudioSource 주입 후 초기화</summary>
    public void Setup(AudioSource source)
    {
        audioSource = source;
        GenerateClips();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss += HandleNearMiss;
            gm.OnGameOver += HandleGameOver;
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss -= HandleNearMiss;
            gm.OnGameOver -= HandleGameOver;
        }
    }

    void GenerateClips()
    {
        bounceClip = GenerateTone(440f, 0.05f, 0.3f);
        clickClip = GenerateTone(880f, 0.03f, 0.2f);
        nearMissClip = GenerateSwoosh(0.15f);
        deathClip = GenerateNoise(0.3f, 0.5f);
    }

    // ── 톤 생성 (사인파 + 페이드아웃) ──
    AudioClip GenerateTone(float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
        }

        var clip = AudioClip.Create("tone_" + frequency, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ── 스우시 생성 (하강 주파수 + 노이즈) ──
    AudioClip GenerateSwoosh(float duration)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];

        // 시드 고정으로 일관된 사운드 생성
        System.Random rng = new System.Random(42);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(2000f, 200f, t / duration);
            float envelope = (1f - t / duration) * 0.4f;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.1f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope + noise * envelope;
        }

        var clip = AudioClip.Create("swoosh", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ── 노이즈 생성 (사망 효과음) ──
    AudioClip GenerateNoise(float duration, float volume)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];

        System.Random rng = new System.Random(99);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = (1f - t / duration) * volume;
            samples[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * envelope;
        }

        var clip = AudioClip.Create("noise", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ── 이벤트 핸들러 ──

    void HandleNearMiss(int combo)
    {
        if (audioSource == null) return;
        audioSource.pitch = 1f + (combo - 1) * 0.15f; // 콤보당 음정 상승
        audioSource.PlayOneShot(nearMissClip, 0.5f + combo * 0.1f);
    }

    void HandleGameOver()
    {
        if (audioSource == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(deathClip, 0.7f);
    }

    public void PlayBounce()
    {
        if (audioSource == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(bounceClip, 0.3f);
    }

    public void PlayClick()
    {
        if (audioSource == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clickClip, 0.2f);
    }
}
