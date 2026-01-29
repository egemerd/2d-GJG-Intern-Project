using System.Collections.Generic;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private Transform poolParent;

    private Queue<GameObject> availableBlocks = new Queue<GameObject>();
    private List<GameObject> allBlocks = new List<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBlock();
        }

        Debug.Log($"[Block Pool] Block pool initialized with {initialPoolSize} blocks");
    }

    private GameObject CreateNewBlock()
    {
        GameObject block = Instantiate(blockPrefab, poolParent);
        block.SetActive(false);
        block.name = $"PooledBlock_{allBlocks.Count}";

        availableBlocks.Enqueue(block);
        allBlocks.Add(block);

        return block;
    }

    public GameObject GetBlock()
    {
        GameObject block;

        if (availableBlocks.Count > 0)
        {
            block = availableBlocks.Dequeue();
        }
        else
        {
            // Pool exhausted, create new block
            block = CreateNewBlock();
            availableBlocks.Dequeue(); 
            Debug.LogWarning("Block pool exhausted! Creating new block. Consider increasing pool size.");
        }

        block.SetActive(true);
        return block;
    }

    public void ReturnBlock(GameObject block)
    {
        if (block == null) return;

        block.SetActive(false);
        block.transform.SetParent(poolParent);
        block.transform.localPosition = Vector3.zero;

        availableBlocks.Enqueue(block);
    }

    public void ReturnAllBlocks()
    {
        foreach (GameObject block in allBlocks)
        {
            if (block != null && block.activeSelf)
            {
                ReturnBlock(block);
            }
        }
    }

    public int AvailableCount => availableBlocks.Count;
    public int TotalCount => allBlocks.Count;
}