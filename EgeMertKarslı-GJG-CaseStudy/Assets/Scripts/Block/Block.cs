using System;
using UnityEngine;

[System.Serializable]
public enum BlockState
{
    Idle,       
    Blasting,   
    Falling,   
    Spawning,   
    Shuffling   
}

public class Block
{
    public int x;
    public int y;
    public int ColorID;
    public int GroupSize;
    public BlockIconType IconType;

    
    private BlockState _state = BlockState.Idle;
    public BlockState State
    {
        get => _state;
        private set => _state = value;
    }

    
    public event Action<Block, BlockState, BlockState> OnStateChanged;

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

    
    public void SetState(BlockState newState)
    {
        if (_state == newState) return;

        BlockState oldState = _state;
        _state = newState;

        Debug.Log($"[Block ({x},{y})] State: {oldState} → {newState}");

        // Fire event so listeners can react
        OnStateChanged?.Invoke(this, oldState, newState);
    }

    
    public bool CanInteract()
    {
        bool canInteract = _state == BlockState.Idle;

        if (!canInteract)
        {
            Debug.Log($"[Block ({x},{y})] Cannot interact - State: {_state}");
        }

        return canInteract;
    }

    
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