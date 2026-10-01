using UnityEngine;

/// <summary>
/// 에디터에서 직접 오디오 클립을 할당받아 재생하는 사운드 매니저
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Audio Source")]
    [Tooltip("이 오브젝트에 부착된 AudioSource를 자동으로 가져옵니다.")]
    private AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip bounceClip;
    public AudioClip clickClip;
    public AudioClip nearMissClip;
    public AudioClip deathClip;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("[SFXManager] AudioSource 컴포넌트가 없습니다. 인스펙터에서 추가해주세요.");
        }

        StartCoroutine(SubscribeEvents());
    }

    private System.Collections.IEnumerator SubscribeEvents()
    {
        // GameManager가 초기화될 때까지 1프레임 대기
        yield return null;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnNearMiss -= HandleNearMiss;
            gm.OnGameOver -= HandleGameOver;
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

    void HandleNearMiss(int combo)
    {
        if (audioSource == null || nearMissClip == null) return;
        audioSource.pitch = 1f + (combo - 1) * 0.15f; // 콤보당 음정 상승
        audioSource.PlayOneShot(nearMissClip, 0.5f + combo * 0.1f);
    }

    void HandleGameOver()
    {
        if (audioSource == null || deathClip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(deathClip, 0.7f);
    }

    public void PlayBounce()
    {
        if (audioSource == null || bounceClip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(bounceClip, 0.3f);
    }

    public void PlayClick()
    {
        if (audioSource == null || clickClip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clickClip, 0.2f);
    }
}
