using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BlastSystem
{
    private readonly GridData gridData;
    private readonly BlockPool blockPool;
    private readonly LevelConfig config;
    private readonly MonoBehaviour coroutineRunner;

    public event System.Action OnBlastComplete;

    public BlastSystem(
        GridData gridData,
        BlockPool blockPool,
        LevelConfig config,
        MonoBehaviour coroutineRunner)
    {
        this.gridData = gridData;
        this.blockPool = blockPool;
        this.config = config;
        this.coroutineRunner = coroutineRunner;
    }

    
    public void BlastGroup(List<Block> group)
    {
        if (group == null || group.Count < 2) return;

        Debug.Log($"[BlastSystem] Blasting {group.Count} blocks");

        foreach (Block block in group)
        {
            block.SetState(BlockState.Blasting);
        }

        coroutineRunner.StartCoroutine(BlastRoutine(group));
    }

    private IEnumerator BlastRoutine(List<Block> group)
    {
        // Get blast duration from animator
        float blastDuration = 0.2f;
        if (group.Count > 0 && group[0].VisualObject != null)
        {
            BlockAnimator animator = group[0].VisualObject.GetComponent<BlockAnimator>();
            if (animator != null)
            {
                blastDuration = animator.BlastDuration;
            }
        }

        
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayBlastGroupEffect(group, config);
        }

        // Wait for blast animation
        yield return new WaitForSeconds(blastDuration);

        // Return blocks to pool and clear grid
        foreach (Block block in group)
        {
            if (block.VisualObject != null)
            {
                blockPool.ReturnBlock(block.VisualObject);
            }
            gridData.ClearBlock(block.x, block.y);
        }

        Debug.Log("[BlastSystem] Blocks destroyed");

        yield return new WaitForSeconds(config.blastDelay);

        OnBlastComplete?.Invoke();
    }
}