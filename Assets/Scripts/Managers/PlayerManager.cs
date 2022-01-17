using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    #region Properties

    public Player Player => player;

    #endregion
    
    #region Inspector Controlled Variables
    
    [SerializeField] private Player player = null;

    #endregion

    #region Private Members



    #endregion
}
