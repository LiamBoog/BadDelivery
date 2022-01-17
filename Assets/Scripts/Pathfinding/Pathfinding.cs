using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Pathfinding
{
    #region Public Methods

    //TODO - Make this do something when it can't find a path
    /// <summary>
    /// Finds shortest path from startPoint to endPoint
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="callback"></param>
    public static void FindPath(NavGrid grid, Vector3 startPoint, Vector3 endPoint, Action<List<NavNode>> callback)
    {
        Stopwatch s = new Stopwatch();
        s.Start();
        NodeHeap openSet = new NodeHeap();
        List<NavNode> closedSet = new List<NavNode>();

        NavNode startNode = grid.NodeFromWorldPoint(startPoint);
        NavNode endNode = grid.NodeFromWorldPoint(endPoint);
        
        if (!endNode.Walkable)
        {
            endNode = grid.NearestWalkableNode(endNode);
        }
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            //Find new node to be checked
            NavNode currentNode = openSet.Pop();

            closedSet.Add(currentNode);

            //Break if path is found
            if (currentNode == endNode)
            {
                callback?.Invoke(RetracePath(startNode, endNode));
                return;
            }
            
            //A* algorithm
            AStarStep(currentNode, endNode, grid, closedSet, openSet);
        }
    }

    /// <summary>
    /// Finds a path from endPoint to currentPath and modifies currentPath to follow it. Not necessarily the shortest path, but faster to find.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="currentPath"></param>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="callback"></param>
    public static void FindPathFast(NavGrid grid, List<NavNode> currentPath, Vector3 startPoint, Vector3 endPoint, Action<List<NavNode>> callback)
    {
        NodeHeap openSet = new NodeHeap();
        List<NavNode> closedSet = new List<NavNode>();

        NavNode startNode = grid.NodeFromWorldPoint(endPoint);
        NavNode endNode = grid.NodeFromWorldPoint(startPoint);
        
        if (!startNode.Walkable)
        {
           startNode = grid.NearestWalkableNode(startNode);
        }
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            //Find new node to be checked
            NavNode currentNode = openSet.Pop();

            closedSet.Add(currentNode);

            //Break if path is found
            if (currentPath.Contains(currentNode))
            {
                List<NavNode> path = RetracePath(startNode, currentNode);

                //Append new path to old path
                if (path.Count > 0)
                {
                    int i = currentPath.Count - 1;
                    while (currentPath[i] != path[path.Count - 1])
                    {
                        currentPath.Remove(currentPath[i]);
                        i--;
                    }
                    currentPath.Remove(currentPath[i]);

                    for (int j = path.Count - 1; j >= 0; j--)
                    {
                        currentPath.Add(path[j]);
                    }
                }
                
                callback?.Invoke(currentPath);
                return;
            }

            //A* algorithm
            AStarStep(currentNode, endNode, grid, closedSet, openSet);
        }
    }

    public static List<NavNode> SimplifyPath(List<NavNode> path)
    {
        List<NavNode> simplifiedPath = new List<NavNode>();
        
        NavNode currentNode = path[1];
        NavNode previousNode = path[0];
        Vector2 previousDir = Vector2.zero;
        
        for (int i = 2; i < path.Count; i++)
        {
            Vector2 currentDir = new Vector2(currentNode.worldPosition.x, currentNode.worldPosition.z) - new Vector2(previousNode.worldPosition.x, previousNode.worldPosition.z);

            if (currentDir != previousDir)
            {
                simplifiedPath.Add(previousNode);
            }

            previousNode = currentNode;
            currentNode = path[i];
            previousDir = currentDir;
        }
        
        simplifiedPath.Add(path[path.Count - 1]);

        return simplifiedPath;
    }
    
    #endregion

    #region Private Methods

    private static void AStarStep(NavNode currentNode, NavNode endNode, NavGrid grid, List<NavNode> closedSet, NodeHeap openSet)
    {
        foreach (NavNode neighbour in grid.GetNeighbours(currentNode))
        {
            if (neighbour.Walkable && !closedSet.Contains(neighbour))
            {
                int newMovementCostToNeighbour = currentNode.gCost + GetCostToNeighbour(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }
    }
    
    private static List<NavNode> RetracePath(NavNode startNode, NavNode endNode)
    {
        List<NavNode> path = new List<NavNode>();

        NavNode currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }
    
    private static List<NavNode> RetraceSimplifiedPath(NavNode startNode, NavNode endNode)
    {
        List<NavNode> path = new List<NavNode>();

        NavNode currentNode = endNode.parent;
        NavNode previousNode = endNode;
        Vector2 previousDir = Vector2.zero;
        
        while (currentNode != startNode)
        {
            Vector2 currentDir = new Vector2(currentNode.worldPosition.x, currentNode.worldPosition.z) - new Vector2(previousNode.worldPosition.x, previousNode.worldPosition.z);

            if (currentDir != previousDir)
            {
                path.Add(previousNode);
            }

            previousNode = currentNode;
            currentNode = currentNode.parent;
            previousDir = currentDir;
        }

        path.Reverse();

        return path;
    }

    private static int GetDistance(NavNode nodeA, NavNode nodeB)
    {
        int x = Mathf.Abs(nodeA.GridPositionX - nodeB.GridPositionX);
        int y = Mathf.Abs(nodeA.GridPositionY - nodeB.GridPositionY);
        int z = Mathf.Abs(nodeA.GridPositionZ - nodeB.GridPositionZ);
        
        int A = 0;
        int B = 0;
        int C = 0;

        while (x > 0 || y > 0 || z > 0)
        {
            if (x > 0 && y > 0 && z > 0)
            {
                A++;
                x--;
                y--;
                z--;
            }
            else if (x > 0 && y > 0 || x > 0 && z > 0 || y > 0 && z > 0)
            {
                B++;
                x -= x > 0 ? 1 : 0;
                y -= y > 0 ? 1 : 0;
                z -= z > 0 ? 1 : 0;
            }
            else if (x > 0 || y > 0 || z > 0)
            {
                C++;
                x -= x > 0 ? 1 : 0;
                y -= y > 0 ? 1 : 0;
                z -= z > 0 ? 1 : 0;
            }
        }
        
        return A * 17 + B * 14 + C * 10;
    }
    
    /// <summary>
    /// Returns the weighted cost of moving to a neighbouring node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="neighbour"></param>
    /// <returns></returns>
    private static int GetCostToNeighbour(NavNode node, NavNode neighbour)
    {
        int dist = GetDistance(node, neighbour);

        return (int)(0.5f * dist * (node.resistance + neighbour.resistance));
    }
    
    #endregion
}
