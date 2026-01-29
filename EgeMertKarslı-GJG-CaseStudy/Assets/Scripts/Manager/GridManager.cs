using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    
    private Board board;
    private BlockPool blockPool;
    private LevelConfig config;
    private SortingOrderManager sortingOrderManager;

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


    private bool isProcessing = false;
    public bool IsProcessing => isProcessing;

    
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
        board = FindObjectOfType<Board>();
        blockPool = GetComponent<BlockPool>();
        config = board.Config;
        sortingOrderManager = GetComponent<SortingOrderManager>();

        
    }

    private void InitializeSystems()
    {
        gridData = new GridData(board.Width, board.Height);

        blockSpawner = new BlockSpawner(gridData, board, blockPool, config, sortingOrderManager);
        groupDetector = new GroupDetector(gridData, config, minGroupSize);
        deadlockChecker = new DeadlockChecker(gridData, groupDetector, minGroupSize);
        blastSystem = new BlastSystem(gridData, blockPool, config, this);
        gravitySystem = new GravitySystem(gridData, board, blockSpawner, sortingOrderManager, this);
        shuffleSystem = new ShuffleSystem(gridData, board, config, sortingOrderManager, this, minGroupSize, guaranteedColorCount);

        blastSystem.OnBlastComplete += OnBlastComplete;
        gravitySystem.OnGravityComplete += OnGravityComplete;
        shuffleSystem.OnShuffleComplete += OnShuffleComplete;

        Debug.Log($"[GridManager] Systems initialized: {Columns}x{Rows} grid");
    }

    

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



    private void OnDestroy()
    {
        if (blastSystem != null) blastSystem.OnBlastComplete -= OnBlastComplete;
        if (gravitySystem != null) gravitySystem.OnGravityComplete -= OnGravityComplete;
        if (shuffleSystem != null) shuffleSystem.OnShuffleComplete -= OnShuffleComplete;
    }
}