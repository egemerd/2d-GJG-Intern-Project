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

    [Header("Shuffle Animation")]
    [SerializeField] private float shuffleDuration = 0.4f;
    [SerializeField] private float shuffleJumpHeight = 0.5f;
    [SerializeField] private float shuffleScalePunch = 1.1f;
    [SerializeField] private AnimationCurve shuffleMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve shuffleJumpCurve;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    
    public event Action OnBlastAnimationComplete;
    public event Action OnShuffleAnimationComplete;

    
    public float BlastDuration => blastDuration;
    public float ShuffleDuration => shuffleDuration;

    private Block currentBlock;
    private Coroutine currentAnimation;
    private Vector3 originalScale;

    
    private Vector3 shuffleTargetPosition;
    private bool hasShuffleTarget = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;

        // Initialize shuffle jump curve if not set
        if (shuffleJumpCurve == null || shuffleJumpCurve.keys.Length == 0)
        {
            shuffleJumpCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            );
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
        Debug.Log($"[BlockAnimator] Shuffle target set to {targetPosition}");
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

            case BlockState.Shuffling:
                if (hasShuffleTarget)
                {
                    currentAnimation = StartCoroutine(PlayShuffleAnimation(shuffleTargetPosition));
                }
                else
                {
                    Debug.LogWarning($"[BlockAnimator] No shuffle target set for block ({block.x},{block.y})!");
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

    private IEnumerator PlayShuffleAnimation(Vector3 targetPos)
    {
        Debug.Log($"[BlockAnimator] Playing shuffle animation to {targetPos}");

        Vector3 startPos = transform.position;
        Vector3 startScale = originalScale;
        Vector3 punchScale = originalScale * shuffleScalePunch;

        float elapsed = 0f;

        while (elapsed < shuffleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shuffleDuration;

            // Movement with easing
            float moveT = shuffleMoveCurve.Evaluate(t);
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, moveT);

            // Add jump arc (Y offset)
            float jumpT = shuffleJumpCurve.Evaluate(t);
            currentPos.y += jumpT * shuffleJumpHeight;

            transform.position = currentPos;

            // Scale punch (grow at start, shrink at end)
            float scaleT;
            if (t < 0.2f)
            {
                scaleT = t / 0.2f;
                transform.localScale = Vector3.Lerp(startScale, punchScale, scaleT);
            }
            else if (t > 0.8f)
            {                
                scaleT = (t - 0.8f) / 0.2f;
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

        Debug.Log($"[BlockAnimator] Shuffle animation complete");
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