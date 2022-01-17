using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    #region Accessors

    public bool PlayerHasControl => playerHasControl;
    public bool PlayerHasMovementControl => playerHasMovementControl;
    public bool PlayerHasCameraControl => playerHasCameraControl;

    #endregion

    #region Private Members

    private bool playerHasControl = false;
    private bool playerHasMovementControl = false;
    private bool playerHasCameraControl = false;

    #endregion
    
    #region Public Methods

    public void SetPlayerCameraControl(bool toggle)
    {
        playerHasCameraControl = toggle;
    }

    public void SetPlayerMovementControl(bool toggle)
    {
        playerHasMovementControl = toggle;
    }
    
    #endregion

    #region Unity Methods

    public void Awake()
    {
        playerHasControl = true;
        playerHasMovementControl = true;
        playerHasCameraControl = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        PathfindingManger.Instance.NavGrid.CreateNewGrid();
        EnemyManager.Instance.CreateNewObstacleHashGrid();
    }

    #endregion
}
