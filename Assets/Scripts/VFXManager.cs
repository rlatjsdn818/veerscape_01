using UnityEngine;

/// <summary>
/// 유니티 Particle System 프리팹 제어 매니저
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Particle System Prefabs")]
    public ParticleSystem bounceEffectPrefab;
    public ParticleSystem nearMissEffectPrefab;
    public ParticleSystem deathEffectPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnBounceEffect(Vector3 position)
    {
        PlayParticle(bounceEffectPrefab, position);
    }

    public void SpawnNearMissEffect(Vector3 position)
    {
        PlayParticle(nearMissEffectPrefab, position);
    }

    public void SpawnDeathEffect(Vector3 position)
    {
        PlayParticle(deathEffectPrefab, position);
    }

    private void PlayParticle(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;

        ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity);
        ps.Play();

        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
        Destroy(ps.gameObject, duration);
    }
}