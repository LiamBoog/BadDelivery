using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    #region inspector Controlled Variables

    [SerializeField] private float tileSize = 0f;
    
    [SerializeField] private float movementUpdateTimeStep = 0f;
    [SerializeField] private float rehashTimestep = 0f;

    #endregion

    #region Private Members

    private SpatialHashGrid<Obstacle> obstacleHashGrid;
    
    private Vector2 hashGridSize;
    private Vector3 hashGridPosition;

    #endregion

    #region Properties

    public SpatialHashGrid<Obstacle> ObstacleHashGrid => obstacleHashGrid;

    public float MovementUpdateTimeStep => movementUpdateTimeStep;
    public float RehashTimestep => rehashTimestep;

    #endregion

    #region Public Methods

    public void CreateNewObstacleHashGrid()
    {
        hashGridSize = new Vector2(PathfindingManger.Instance.NavGrid.GridSizeX, PathfindingManger.Instance.NavGrid.GridSizeZ);
        hashGridPosition = PathfindingManger.Instance.NavGrid.GridPosition;
        
        obstacleHashGrid = new SpatialHashGrid<Obstacle>(tileSize, hashGridSize, hashGridPosition);
    }

    #endregion
}

