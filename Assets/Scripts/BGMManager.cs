using UnityEngine;
using System.Collections;

/// <summary>
/// 배경음악(BGM) 전용 매니저.
/// - 인스펙터에서 AudioClip을 지정하면 자동으로 루프 재생.
/// - 타이틀/게임 시작 시 FadeIn.
/// - 게임 오버(죽음) 시 FadeOut 후 정지.
/// - 재시작/타이틀 복귀 시 다시 FadeIn.
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio Clip")]
    [Tooltip("재생할 배경음악 클립")]
    public AudioClip bgmClip;

    [Header("Volume")]
    [Tooltip("배경음악 최대 볼륨")]
    [Range(0f, 1f)]
    public float maxVolume = 1f;

    [Header("Fade Settings")]
    [Tooltip("페이드 인/아웃 시간 (초)")]
    public float fadeDuration = 1.0f;

    private AudioSource audioSource;
    private Coroutine currentFade; // 페이드 코루틴만 추적 (다른 코루틴 영향 없음)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        if (bgmClip != null) audioSource.clip = bgmClip;

        // 저장된 BGM 볼륨 불러오기 (기본값 1f)
        maxVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
    }

    /// <summary>
    /// 외부(옵션 UI 등)에서 실시간으로 BGM 볼륨을 조절할 때 호출
    /// </summary>
    public void SetVolume(float volume)
    {
        maxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", maxVolume);

        // 현재 재생 중이고 페이드가 끝난 상태라면 즉시 볼륨 반영
        if (audioSource != null && audioSource.isPlaying && currentFade == null)
        {
            audioSource.volume = maxVolume;
        }
    }

    void Start()
    {
        // Start()는 모든 Awake() 이후에 실행되므로 GameManager가 이미 초기화됨
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += HandleGameStart;
            gm.OnGameOver += HandleGameOver;
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart -= HandleGameStart;
            gm.OnGameOver -= HandleGameOver;
        }
    }

    void HandleGameStart()
    {
        FadeIn();
    }

    void HandleGameOver()
    {
        // 즉시 정지
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = null;
        audioSource.Stop();
        audioSource.volume = 0f;
    }

    /// <summary>
    /// 볼륨을 0 → maxVolume 로 서서히 올리면서 재생
    /// </summary>
    public void FadeIn()
    {
        if (bgmClip == null) return;
        if (!audioSource.isPlaying)
        {
            audioSource.clip = bgmClip;
            audioSource.Play();
        }
        // 이전 페이드만 중단 (다른 코루틴에 영향 없음)
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeVolume(audioSource.volume, maxVolume, fadeDuration));
    }

    /// <summary>
    /// 볼륨을 현재 → 0 로 서서히 낮춘 뒤 정지
    /// </summary>
    public void FadeOut()
    {
        if (!audioSource.isPlaying) return;
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeOutAndStop(fadeDuration));
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
        currentFade = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVol = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();
        currentFade = null;
    }
}
