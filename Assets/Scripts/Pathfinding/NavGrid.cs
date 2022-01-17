using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    #region Inspector Controlled Variables

    [SerializeField] private bool drawGridCubes = false;
    [SerializeField] private bool drawGridOutline = false;
    
    [SerializeField] private Vector3 gridWorldSize;
    [SerializeField] private float nodeSize = 0.5f;
    
    #endregion
    
    #region Private Members
    
    private NavNode[,,] grid;
    
    private Vector3 gridPosition;
    private int gridSizeX, gridSizeY, gridSizeZ;

    private float maxResistance;
    private float minResistance = 1f;
    
    #endregion
    
    #region Properties

    public Vector3 GridPosition => gridPosition;
    public int GridSizeX => gridSizeX;
    public int GridSizeY => gridSizeY;
    public int GridSizeZ => gridSizeZ;

    #endregion
    
    #region Public Methods

    public void CreateNewGrid()
    {
        gridPosition = transform.position;
        gridSizeX = (int) (gridWorldSize.x / nodeSize);
        gridSizeY = (int) (gridWorldSize.y / nodeSize);
        gridSizeZ = (int) (gridWorldSize.z / nodeSize);
        grid = new NavNode[gridSizeX, gridSizeY, gridSizeZ];

        Vector3 gridBottomLeft = gridPosition - nodeSize * (Vector3.right * gridSizeX / 2f + Vector3.forward * gridSizeZ/ 2f);
        
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 worldPoint = gridBottomLeft + 
                                         Vector3.right * (x * nodeSize + nodeSize / 2f) +
                                         Vector3.up * (y * nodeSize + nodeSize / 2f) +
                                         Vector3.forward * (z * nodeSize + nodeSize / 2f);

                    bool walkable = Physics.Raycast(worldPoint + 0.5f * nodeSize * Vector3.up, Vector3.down, nodeSize,
                        ~(LayerMask.GetMask(Constants.LAYER_NAME_UNWALKABLE)
                          + LayerMask.GetMask(Constants.LAYER_NAME_PLAYER)
                          + LayerMask.GetMask(Constants.LAYER_NAME_ENEMY)
                          + LayerMask.GetMask(Constants.LAYER_NAME_WATER)));

                    bool unwalkable = false;
                    if (walkable)
                    {
                        Collider[] colliders = Physics.OverlapBox(worldPoint + (0.5f * nodeSize + 1f) * Vector3.up, 1f * Vector3.up);
                        foreach (Collider col in colliders)
                        {
                            if (!col.isTrigger)
                            {
                                unwalkable = true;
                                break;
                            }
                        }
                    }
                    
                    grid[x, y, z] = new NavNode(worldPoint, walkable && !unwalkable, x, y, z);
                    grid[x, y, z].resistance = walkable && !unwalkable ? 1f : 10f;

                }
            }
        }
        
        BlurResistanceMap(2);
    }
    
    public NavNode NodeFromWorldPoint(Vector3 worldPoint)
    {
        
        Vector3 gridBottomLeft = gridPosition - Vector3.right * gridWorldSize.x / 2f - Vector3.forward * gridWorldSize.z / 2f;
        Vector3 gridPoint = worldPoint - gridBottomLeft;

        int x = (int) (gridPoint.x / nodeSize);
        int y = (int) (gridPoint.y / nodeSize);
        int z = (int) (gridPoint.z / nodeSize);

        return grid[x, y, z];
    }

    public List<NavNode> GetNeighbours(NavNode node)
    {
        int currentY = node.GridPositionY;
        List<NavNode> neighbours = new List<NavNode>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                int currentX = node.GridPositionX + x;
                int currentZ = node.GridPositionZ + z;

                if (currentX < gridSizeX && currentX >= 0 && currentZ < gridSizeZ && currentZ >= 0)
                {
                    if (currentY + 1 < gridSizeY)
                    {
                        neighbours.Add(grid[currentX, currentY + 1, currentZ]);
                    }

                    if (currentY - 1 > 0)
                    {
                        neighbours.Add(grid[currentX, currentY - 1, currentZ]);
                    }
                    
                    NavNode currentNode = grid[currentX, currentY, currentZ];
                    if (currentNode != node)
                    {
                        neighbours.Add(currentNode);
                    }
                }
            }
        }
        
        return neighbours;
    }
    
    /// <summary>
    /// Find the nearest walkable node
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public NavNode NearestWalkableNode(NavNode node)
    {
        for (int i = 1; i < Mathf.Max(gridSizeX / 2, gridSizeY / 2, gridSizeZ / 2); i++)
        {
            for (int x = -i; x <= i; x++)
            {
                for (int y = -i; y <= i; y++)
                {
                    for (int z = -i; z <= i; z++)
                    {
                        int currentX = node.GridPositionX + x < gridSizeX ? node.GridPositionX + x : node.GridPositionX;
                        int currentY = node.GridPositionY + y < gridSizeY ? node.GridPositionY + y : node.GridPositionY;
                        int currentZ = node.GridPositionZ + z < gridSizeZ ? node.GridPositionZ + z : node.GridPositionZ;
                        currentX = Mathf.Clamp(currentX, 0, gridSizeX - 1);
                        currentY = Mathf.Clamp(currentY, 0, gridSizeX - 1);
                        currentZ = Mathf.Clamp(currentZ, 0, gridSizeX - 1);

                        NavNode currentNode = grid[currentX, currentY, currentZ];
                        
                        if (currentNode.Walkable)
                        {
                            return currentNode;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    #endregion
    
    #region Private Methods

    //TODO - Make this faster
    private void BlurResistanceMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        float[,,] resistanceMap = new float[gridSizeX, gridSizeY, gridSizeZ];

        //Loop over all nodes in grid
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    //Sample neighbouring nodes in the same X-Z plane
                    for (int kernelX = -kernelExtents; kernelX <= kernelExtents; kernelX++)
                    {
                        int sampleX = Mathf.Clamp(x + kernelX, 0, gridSizeX - 1);
                        for (int kernelZ = -kernelExtents; kernelZ <= kernelExtents; kernelZ++)
                        {
                            int sampleZ = Mathf.Clamp(z + kernelZ, 0, gridSizeZ - 1);
                            
                            resistanceMap[x, y, z] += grid[sampleX, y, sampleZ].resistance;
                        }
                    }

                    resistanceMap[x, y, z] /= 27f;

                    if (resistanceMap[x, y, z] > maxResistance)
                    {
                        maxResistance = resistanceMap[x, y, z];
                    }

                    if (resistanceMap[x, y, z] < minResistance)
                    {
                        minResistance = resistanceMap[x, y, z];
                    }
                }
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    grid[x, y, z].resistance = resistanceMap[x, y, z];
                }
            }
        }
    }

    #endregion

    #region Unity Methods

    private void Update()
    {
        gridPosition = transform.position;
    }

    private void OnDrawGizmos()
    {
        gridPosition = transform.position;
        
        if (drawGridOutline)
        {
            Gizmos.DrawWireCube(gridPosition + 0.5f * gridWorldSize.y * Vector3.up, new Vector3(gridWorldSize.x, gridWorldSize.y, gridWorldSize.z));
        }

        if (drawGridCubes)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        if (grid[x, y, z].Walkable)
                        {
                            Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.InverseLerp(minResistance, maxResistance, grid[x, y, z].resistance));
                            Gizmos.DrawWireCube(grid[x, y, z].worldPosition, Vector3.one * nodeSize);
                        }
                    }
                }
            }
        }
    }
    
    #endregion
}
