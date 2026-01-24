using UnityEngine;
using System;
using System.Collections;

public class BlockAnimator : MonoBehaviour
{
    [Header("Blast Animation")]
    [SerializeField] private float blastDuration = 0.2f;
    [SerializeField] private float blastScaleMultiplier = 1.3f;
    [SerializeField] private AnimationCurve blastCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Spawn Animation")]
    [SerializeField] private float spawnDuration = 0.2f;
    [SerializeField] private AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Event fired when blast animation completes
    public event Action OnBlastAnimationComplete;

    // Expose duration for GridManager
    public float BlastDuration => blastDuration;

    private Block currentBlock;
    private Coroutine currentAnimation;
    private Vector3 originalScale;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void OnDisable()
    {
        if (currentBlock != null)
        {
            currentBlock.OnStateChanged -= HandleStateChanged;
            currentBlock = null;
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        // Clear event subscribers
        OnBlastAnimationComplete = null;
    }

    public void BindToBlock(Block block)
    {
        if (currentBlock != null)
        {
            currentBlock.OnStateChanged -= HandleStateChanged;
        }

        currentBlock = block;

        if (currentBlock != null)
        {
            currentBlock.OnStateChanged += HandleStateChanged;
            Debug.Log($"[BlockAnimator] Bound to Block ({block.x},{block.y})");
        }
    }

    private void HandleStateChanged(Block block, BlockState oldState, BlockState newState)
    {
        Debug.Log($"[BlockAnimator ({block.x},{block.y})] State changed: {oldState} → {newState}");

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        switch (newState)
        {
            case BlockState.Blasting:
                currentAnimation = StartCoroutine(PlayBlastAnimation());
                break;

            case BlockState.Spawning:
                currentAnimation = StartCoroutine(PlaySpawnAnimation());
                break;

            case BlockState.Falling:
                break;

            case BlockState.Idle:
                transform.localScale = originalScale;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
                break;
        }
    }

    private IEnumerator PlayBlastAnimation()
    {
        Debug.Log($"[BlockAnimator] Playing blast animation");

        Vector3 startScale = originalScale;
        Vector3 endScale = originalScale * blastScaleMultiplier;
        Color startColor = Color.white;
        Color endColor = new Color(1f, 1f, 1f, 0f);

        float elapsed = 0f;

        while (elapsed < blastDuration)
        {
            elapsed += Time.deltaTime;
            float t = blastCurve.Evaluate(elapsed / blastDuration);

            float scaleT = t < 0.5f ? t * 2f : (1f - t) * 2f;
            transform.localScale = Vector3.Lerp(startScale, endScale, scaleT);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            }

            yield return null;
        }

        transform.localScale = Vector3.zero;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = endColor;
        }

        currentAnimation = null;

        // Notify that blast animation is complete
        Debug.Log($"[BlockAnimator] Blast animation complete");
        OnBlastAnimationComplete?.Invoke();
    }

    private IEnumerator PlaySpawnAnimation()
    {
        Debug.Log($"[BlockAnimator] Playing spawn animation");

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = originalScale;

        transform.localScale = startScale;

        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = spawnCurve.Evaluate(elapsed / spawnDuration);

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
        currentAnimation = null;
    }

    public void SetOriginalScale(Vector3 scale)
    {
        originalScale = scale;
    }
}