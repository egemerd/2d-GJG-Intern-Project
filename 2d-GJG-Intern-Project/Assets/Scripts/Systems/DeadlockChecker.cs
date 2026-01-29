using UnityEngine;

public class DeadlockChecker
{
    private readonly GridData gridData;
    private readonly GroupDetector groupDetector;
    private readonly int minGroupSize;

    public DeadlockChecker(GridData gridData, GroupDetector groupDetector, int minGroupSize)
    {
        this.gridData = gridData;
        this.groupDetector = groupDetector;
        this.minGroupSize = minGroupSize;
    }

    
    public bool IsDeadlock()
    {
        for (int y = 0; y < gridData.Rows; y++)
        {
            for (int x = 0; x < gridData.Columns; x++)
            {
                Block block = gridData.GetBlock(x, y);
                if (block != null && block.CanBeGrouped())
                {
                    var group = groupDetector.FindConnectedGroup(x, y);
                    if (group != null && group.Count >= minGroupSize)
                    {
                        return false;
                    }
                }
            }
        }

        Debug.Log("[DeadlockChecker] Deadlock detected - no valid groups");
        return true;
    }
}