using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 런타임에 URP Volume을 찾아(또는 생성하여)
/// Bloom, Chromatic Aberration, Lens Distortion을 동적으로 제어합니다.
/// Near Miss 시 강력한 시각적 피드백 제공.
/// </summary>
public class PostProcessController : MonoBehaviour
{
    private Volume globalVolume;
    private Bloom bloom;
    private ChromaticAberration chromatic;
    private LensDistortion lensDistortion;

    private float targetBloomIntensity = 1.5f;
    private float defaultBloomIntensity = 1.5f;
    private float targetChromatic = 0.1f;
    private float targetLens = 0f;
    
    private float effectTimer;

    void Start()
    {
        SetupVolume();

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

    void SetupVolume()
    {
        globalVolume = FindAnyObjectByType<Volume>();
        if (globalVolume == null)
        {
            var go = new GameObject("Global Volume");
            globalVolume = go.AddComponent<Volume>();
            globalVolume.isGlobal = true;
        }

        if (globalVolume.profile == null)
        {
            globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Bloom 설정
        if (!globalVolume.profile.TryGet(out bloom))
            bloom = globalVolume.profile.Add<Bloom>(false);
        bloom.active = true;
        bloom.intensity.Override(defaultBloomIntensity);
        bloom.threshold.Override(0.5f);

        // Chromatic Aberration 설정
        if (!globalVolume.profile.TryGet(out chromatic))
            chromatic = globalVolume.profile.Add<ChromaticAberration>(false);
        chromatic.active = true;
        chromatic.intensity.Override(0.1f);

        // Lens Distortion 설정
        if (!globalVolume.profile.TryGet(out lensDistortion))
            lensDistortion = globalVolume.profile.Add<LensDistortion>(false);
        lensDistortion.active = true;
        lensDistortion.intensity.Override(0f);
    }

    void HandleGameStart()
    {
        targetBloomIntensity = defaultBloomIntensity;
        targetChromatic = 0.1f;
        targetLens = 0f;
        effectTimer = 0f;
    }

    void HandleNearMiss(int combo)
    {
        // 콤보에 비례하여 효과 증폭
        targetBloomIntensity = 5f + (combo * 1.5f);
        targetChromatic = 0.5f + (combo * 0.1f);
        targetLens = -0.3f;
        
        effectTimer = 0.4f; // 효과 유지 시간
    }

    void HandleGameOver()
    {
        targetBloomIntensity = defaultBloomIntensity;
        targetChromatic = 1f; // 사망 시 화면 왜곡 강하게
        targetLens = -0.6f;
        effectTimer = 1f;
    }

    void Update()
    {
        if (bloom == null || chromatic == null || lensDistortion == null) return;

        // 효과 타이머 처리 (부드러운 복귀)
        if (effectTimer > 0f)
        {
            effectTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            targetBloomIntensity = defaultBloomIntensity;
            targetChromatic = 0.1f;
            targetLens = 0f;
        }

        // Unscaled time 사용하여 Hit Stop 중에도 부드럽게 이펙트 보간
        float dt = Time.unscaledDeltaTime;
        
        bloom.intensity.Override(Mathf.Lerp(bloom.intensity.value, targetBloomIntensity, dt * 10f));
        chromatic.intensity.Override(Mathf.Lerp(chromatic.intensity.value, targetChromatic, dt * 8f));
        lensDistortion.intensity.Override(Mathf.Lerp(lensDistortion.intensity.value, targetLens, dt * 6f));
    }
}
