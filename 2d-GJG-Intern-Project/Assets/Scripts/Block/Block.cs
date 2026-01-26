using System;
using UnityEngine;

[System.Serializable]
public enum BlockState
{
    Idle,       // Ready for interaction
    Blasting,   // Being destroyed
    Falling,    // Dropping down
    Spawning,   // Just created, animating in
    Shuffling   // Being shuffled to new position
}

public class Block
{
    public int x;
    public int y;
    public int ColorID;
    public int GroupSize;
    public BlockIconType IconType;

    // State management
    private BlockState _state = BlockState.Idle;
    public BlockState State
    {
        get => _state;
        private set => _state = value;
    }

    // Event fired when state changes
    public event Action<Block, BlockState, BlockState> OnStateChanged;

    // Object pooling reference
    public GameObject VisualObject;

    public Block(int x, int y, int colorID)
    {
        this.x = x;
        this.y = y;
        this.ColorID = colorID;
        this.GroupSize = 1;
        this.IconType = BlockIconType.Default;
        this._state = BlockState.Spawning;

        Debug.Log($"[Block] Created at ({x},{y}) | State: {_state}");
    }

    public void Reset(int x, int y, int colorID)
    {
        this.x = x;
        this.y = y;
        this.ColorID = colorID;
        this.GroupSize = 1;
        this.IconType = BlockIconType.Default;

        // Clear old subscribers when reusing from pool
        OnStateChanged = null;

        SetState(BlockState.Spawning);
    }

    /// <summary>
    /// Change block state with debug logging and event firing.
    /// </summary>
    public void SetState(BlockState newState)
    {
        if (_state == newState) return;

        BlockState oldState = _state;
        _state = newState;

        Debug.Log($"[Block ({x},{y})] State: {oldState} → {newState}");

        // Fire event so listeners can react
        OnStateChanged?.Invoke(this, oldState, newState);
    }

    /// <summary>
    /// Can this block be clicked?
    /// </summary>
    public bool CanInteract()
    {
        bool canInteract = _state == BlockState.Idle;

        if (!canInteract)
        {
            Debug.Log($"[Block ({x},{y})] Cannot interact - State: {_state}");
        }

        return canInteract;
    }

    /// <summary>
    /// Can this block be included in group detection?
    /// </summary>
    public bool CanBeGrouped()
    {
        return _state == BlockState.Idle;
    }
}

public enum BlockIconType
{
    Default = 0,
    First = 1,
    Second = 2,
    Third = 3
}