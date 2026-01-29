using UnityEngine;

[CreateAssetMenu(fileName = "BlockAnimationData", menuName = "Anim/BlockAnimationData", order = 1)]
public class BlockAnimationData : ScriptableObject
{
    [Header("Blast Animation")]
    public float blastDuration = 0.2f;
    public float blastScaleMultiplier = 1.3f;
    public AnimationCurve blastCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Spawn Animation")]
    public float spawnDuration = 0.2f;
    public AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shuffle Animation")]
    public float shuffleDuration = 0.4f;
    public float shuffleJumpHeight = 0.5f;
    public float shuffleScalePunch = 1.1f;
    public AnimationCurve shuffleMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve shuffleJumpCurve;
}
