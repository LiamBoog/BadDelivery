using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavNode
{
    public Vector3 worldPosition;
    private bool walkable;
    private int gridPositionX, gridPositionY, gridPositionZ;

    public int hCost;
    public int gCost;
    public NavNode parent;
    
    public float resistance;
    
    public int heapIndex;

    #region Properties
    
    public int FCost => gCost + hCost;
    public bool Walkable => walkable;
    public int GridPositionX => gridPositionX;
    public int GridPositionY => gridPositionY;
    public int GridPositionZ => gridPositionZ;

    #endregion

    #region Public Methods
    public NavNode(Vector3 worldPosition, bool walkable, int gridPositionX, int gridPositionY, int gridPositionZ)
    {
        this.worldPosition = worldPosition;
        this.walkable = walkable;
        this.gridPositionX = gridPositionX;
        this.gridPositionY = gridPositionY;
        this.gridPositionZ = gridPositionZ;
    }

    /// <summary>
    /// Returns 1 if this node has higher heap priority than node, 0 if they have same priority, -1 if node is higher
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public int CompareTo(NavNode node)
    {
        if (node.FCost < FCost || node.FCost == FCost && node.hCost < hCost)
        {
            return -1;
        }
        if (node.FCost == FCost && node.hCost == hCost)
        {
            return 0;
        }

        return 1;
    }

    #endregion
}
