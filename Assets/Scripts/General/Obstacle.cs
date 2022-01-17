using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    //The collider to be used as an obstacle
    [SerializeField] private new Collider collider = null;

    public Collider Collider => collider;

    public void Start()
    {
        EnemyManager.Instance.ObstacleHashGrid.Add(this, collider.transform.position);
    }

    public void OnEnable()
    {
        if (EnemyManager.Instance.ObstacleHashGrid != null)
        {
            EnemyManager.Instance.ObstacleHashGrid.Add(this, collider.transform.position);
        }
    }

    public void OnDisable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ObstacleHashGrid.Remove(this);
        }
    }
}
