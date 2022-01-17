using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarInteractCollider : MonoBehaviour
{
    #region Inspector Controlled Variables

    [SerializeField] private Car car = null;

    #endregion
    
    #region Properties

    public Car Car => car;

    #endregion
}
