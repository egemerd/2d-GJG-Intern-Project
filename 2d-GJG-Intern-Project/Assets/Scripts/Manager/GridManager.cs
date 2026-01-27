using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Coordinates all grid systems. Acts as a facade for grid operations.
/// Follows Single Responsibility Principle - only coordinates, doesn't implement logic.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private LevelConfig config;
    [SerializeField] private SortingOrderManager sortingOrderManager;

    [Header("Gameplay Settings")]
    [SerializeField] private int minGroupSize = 2;

    [Header("Shuffle Settings")]
    [SerializeField][Range(1, 4)] private int guaranteedColorCount = 1;

    // Systems
    private GridData gridData;
    private BlockSpawner blockSpawner;
    private GroupDetector groupDetector;
    private DeadlockChecker deadlockChecker;
    private BlastSystem blastSystem;
    private GravitySystem gravitySystem;
    private ShuffleSystem shuffleSystem;

    // State
    private bool isProcessing = false;
    public bool IsProcessing => isProcessing;

    // Public accessors
    public int Rows => gridData.Rows;
    public int Columns => gridData.Columns;

    private void Awake()
    {
        InitializeReferences();
        InitializeSystems();
    }

    private void Start()
    {
        Debug.Log("[GridManager] === GAME START ===");

        blockSpawner.SpawnAllBlocks();
        groupDetector.UpdateAllGroupIcons();

        Debug.Log("[GridManager] === READY FOR INPUT ===");
    }

    private void InitializeReferences()
    {
        if (board == null) board = FindObjectOfType<Board>();
        if (blockPool == null) blockPool = GetComponent<BlockPool>();
        if (config == null && board != null) config = board.Config;
        if (sortingOrderManager == null) sortingOrderManager = GetComponent<SortingOrderManager>();

        if (board == null || blockPool == null || config == null)
        {
            Debug.LogError("[GridManager] Missing required references!");
            enabled = false;
        }
    }

    private void InitializeSystems()
    {
        // Create data container
        gridData = new GridData(board.Width, board.Height);

        // Create systems
        blockSpawner = new BlockSpawner(gridData, board, blockPool, config, sortingOrderManager);
        groupDetector = new GroupDetector(gridData, config, minGroupSize);
        deadlockChecker = new DeadlockChecker(gridData, groupDetector, minGroupSize);
        blastSystem = new BlastSystem(gridData, blockPool, config, this);
        gravitySystem = new GravitySystem(gridData, board, blockSpawner, sortingOrderManager, this);
        shuffleSystem = new ShuffleSystem(gridData, board, config, sortingOrderManager, this, minGroupSize, guaranteedColorCount);

        // Wire up events
        blastSystem.OnBlastComplete += OnBlastComplete;
        gravitySystem.OnGravityComplete += OnGravityComplete;
        shuffleSystem.OnShuffleComplete += OnShuffleComplete;

        Debug.Log($"[GridManager] Systems initialized: {Columns}x{Rows} grid");
    }

    #region Public API

    public bool CanProcessInput()
    {
        if (isProcessing)
        {
            Debug.Log("[GridManager] Input blocked - processing");
            return false;
        }
        return true;
    }

    public List<Block> FindConnectedGroup(int x, int y)
    {
        return groupDetector.FindConnectedGroup(x, y);
    }

    public void BlastGroup(List<Block> group)
    {
        if (group == null || group.Count < minGroupSize) return;

        isProcessing = true;
        Debug.Log($"[GridManager] === BLAST START === ({group.Count} blocks)");

        blastSystem.BlastGroup(group);
    }

    public Block GetBlock(int x, int y)
    {
        return gridData.GetBlock(x, y);
    }

    public bool IsValidPosition(int x, int y)
    {
        return gridData.IsValidPosition(x, y);
    }

    #endregion

    #region Event Handlers

    private void OnBlastComplete()
    {
        Debug.Log("[GridManager] === GRAVITY START ===");
        gravitySystem.ApplyGravityAndRefill();
    }

    private void OnGravityComplete()
    {
        Debug.Log("[GridManager] === UPDATING ICONS ===");
        groupDetector.UpdateAllGroupIcons();

        if (deadlockChecker.IsDeadlock())
        {
            Debug.LogWarning("[GridManager] === DEADLOCK - SHUFFLING ===");
            shuffleSystem.Shuffle();
        }
        else
        {
            FinishProcessing();
        }
    }

    private void OnShuffleComplete()
    {
        groupDetector.UpdateAllGroupIcons();

        // Check again after shuffle
        if (deadlockChecker.IsDeadlock())
        {
            Debug.LogError("[GridManager] Still deadlocked after shuffle!");
            // Apply emergency fix or handle error
        }

        FinishProcessing();
    }

    private void FinishProcessing()
    {
        isProcessing = false;
        Debug.Log("[GridManager] === READY FOR INPUT ===");
    }

    #endregion

    private void OnDestroy()
    {
        // Unsubscribe events
        if (blastSystem != null) blastSystem.OnBlastComplete -= OnBlastComplete;
        if (gravitySystem != null) gravitySystem.OnGravityComplete -= OnGravityComplete;
        if (shuffleSystem != null) shuffleSystem.OnShuffleComplete -= OnShuffleComplete;
    }
}