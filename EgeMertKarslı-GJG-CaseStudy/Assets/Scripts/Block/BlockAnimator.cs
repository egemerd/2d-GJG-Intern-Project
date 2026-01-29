using UnityEngine;
using System;
using System.Collections;


public class BlockAnimator : MonoBehaviour
{
    [Header("Animation Data (Shared)")]
    [Tooltip("Shared animation settings - references same asset for all blocks")]
    [SerializeField] private BlockAnimationData animationData;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    public event Action OnBlastAnimationComplete;
    public event Action OnShuffleAnimationComplete;

    public float BlastDuration => animationData != null ? animationData.blastDuration : 0.2f;
    public float ShuffleDuration => animationData != null ? animationData.shuffleDuration : 0.4f;

    private Block currentBlock;
    private Coroutine currentAnimation;
    private Vector3 originalScale;

    
    private Vector3 shuffleTargetPosition;
    private bool hasShuffleTarget = false;

    private void Awake()
    {
        
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;

        // Validate animation data
        if (animationData == null)
        {
            Debug.LogWarning($"[BlockAnimator] {gameObject.name} missing BlockAnimationData! Using defaults.");
        }
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        hasShuffleTarget = false;
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

        OnBlastAnimationComplete = null;
        OnShuffleAnimationComplete = null;
        hasShuffleTarget = false;
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

    public void SetShuffleTarget(Vector3 targetPosition)
    {
        shuffleTargetPosition = targetPosition;
        hasShuffleTarget = true;
    }

    private void HandleStateChanged(Block block, BlockState oldState, BlockState newState)
    {
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

            case BlockState.Shuffling:
                if (hasShuffleTarget)
                {
                    currentAnimation = StartCoroutine(PlayShuffleAnimation(shuffleTargetPosition));
                }
                else
                {
                    Debug.LogWarning($"[BlockAnimator] No shuffle target set!");
                    block.SetState(BlockState.Idle);
                }
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
        if (animationData == null) yield break;

        Vector3 startScale = originalScale;
        Vector3 endScale = originalScale * animationData.blastScaleMultiplier;
        Color startColor = Color.white;
        Color endColor = new Color(1f, 1f, 1f, 0f);

        float elapsed = 0f;

        while (elapsed < animationData.blastDuration)
        {
            elapsed += Time.deltaTime;
            float t = animationData.blastCurve.Evaluate(elapsed / animationData.blastDuration);

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
        OnBlastAnimationComplete?.Invoke();
    }

    private IEnumerator PlaySpawnAnimation()
    {
        if (animationData == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = originalScale;

        transform.localScale = startScale;

        float elapsed = 0f;

        while (elapsed < animationData.spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = animationData.spawnCurve.Evaluate(elapsed / animationData.spawnDuration);

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
        currentAnimation = null;
    }

    private IEnumerator PlayShuffleAnimation(Vector3 targetPos)
    {
        if (animationData == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 startScale = originalScale;
        Vector3 punchScale = originalScale * animationData.shuffleScalePunch;

        float elapsed = 0f;

        while (elapsed < animationData.shuffleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationData.shuffleDuration;

            // Movement with easing
            float moveT = animationData.shuffleMoveCurve.Evaluate(t);
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, moveT);

            // Add jump arc
            float jumpT = animationData.shuffleJumpCurve != null
                ? animationData.shuffleJumpCurve.Evaluate(t)
                : Mathf.Sin(t * Mathf.PI); // Fallback if curve missing

            currentPos.y += jumpT * animationData.shuffleJumpHeight;

            transform.position = currentPos;

            // Scale punch
            if (t < 0.2f)
            {
                float scaleT = t / 0.2f;
                transform.localScale = Vector3.Lerp(startScale, punchScale, scaleT);
            }
            else if (t > 0.8f)
            {
                float scaleT = (t - 0.8f) / 0.2f;
                transform.localScale = Vector3.Lerp(punchScale, startScale, scaleT);
            }
            else
            {
                transform.localScale = punchScale;
            }

            yield return null;
        }

        transform.position = targetPos;
        transform.localScale = originalScale;

        hasShuffleTarget = false;
        currentAnimation = null;

        OnShuffleAnimationComplete?.Invoke();

        if (currentBlock != null)
        {
            currentBlock.SetState(BlockState.Idle);
        }
    }

    public void SetOriginalScale(Vector3 scale)
    {
        originalScale = scale;
    }
}