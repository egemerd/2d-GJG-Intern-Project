using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Prefabs")]
    [SerializeField] private GameObject blastParticlePrefab;

    [Header("Pooling Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private Transform poolParent;

    private static ParticleManager instance;
    public static ParticleManager Instance => instance;

    private System.Collections.Generic.Queue<ParticleSystem> particlePool = new System.Collections.Generic.Queue<ParticleSystem>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateParticleInstance();
        }

        Debug.Log($"[ParticleManager] Initialized pool with {initialPoolSize} particles");
    }

    private ParticleSystem CreateParticleInstance()
    {
        GameObject particleObj = Instantiate(blastParticlePrefab, poolParent);
        particleObj.name = $"BlastParticle_{particlePool.Count}";
        particleObj.SetActive(false);

        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError("[ParticleManager] Particle prefab missing ParticleSystem component!");
            Destroy(particleObj);
            return null;
        }

        particlePool.Enqueue(ps);
        return ps;
    }


    public void PlayBlastEffect(Vector3 position, Color blockColor, int particleCount = 15)
    {
        ParticleSystem ps = GetParticleFromPool();
        if (ps == null) return;

        ps.transform.position = position;
        ps.gameObject.SetActive(true);


        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(blockColor, blockColor * 0.7f);

        
        var emission = ps.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));

        
        ps.Play();

        
        StartCoroutine(ReturnToPoolAfterDuration(ps, main.duration + main.startLifetime.constantMax));
    }

    private ParticleSystem GetParticleFromPool()
    {
        if (particlePool.Count > 0)
        {
            return particlePool.Dequeue();
        }

        
        Debug.LogWarning("[ParticleManager] Particle pool exhausted, creating new instance");
        return CreateParticleInstance();
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDuration(ParticleSystem ps, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (ps != null)
        {
            ps.Stop();
            ps.Clear();
            ps.gameObject.SetActive(false);
            particlePool.Enqueue(ps);
        }
    }


    
    public void PlayBlastGroupEffect(System.Collections.Generic.List<Block> group, LevelConfig config)
    {
        if (group == null || group.Count == 0) return;

        foreach (Block block in group)
        {
            if (block?.VisualObject == null) continue;

            Vector3 position = block.VisualObject.transform.position;

            // Get block color
            BlockColorData colorData = config.GetColorData(block.ColorID);
            Color blockColor = colorData != null ? colorData.DisplayColor : Color.white;

            // More particles for larger groups
            int particleCount = Mathf.Clamp(10 + group.Count, 10, 25);

            PlayBlastEffect(position, blockColor, particleCount);
        }
    }
}