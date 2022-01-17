using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Enemy : MonoBehaviour
{
    #region Enumerated Types

    enum PathType
    {
        Complete,
        Fast,
        Null
    }
    
    #endregion
    
    #region Inspector Controlled Variables

    [SerializeField] private List<Collider> characterColliders = null;
    
    [SerializeField] private float normalMoveSpeed = 0f;
    [SerializeField] private bool attack = false;
    
    [SerializeField] private float avoidancePower = 0.75f;
    [SerializeField] private float avoidRadius = 2f;
    [SerializeField] private Obstacle thisObstacle = null;

    #endregion

    #region Private Members

    private int health = 100;
    private float despawnTime = 10f;
    
    private int currentPathIndex = 0;

    #endregion

    #region Public Methods

    /// <summary>
    /// Deal a certain amount of damage to this enemy
    /// </summary>
    /// <param name="damage">Amount of damage</param>
    public void TakeDamage(uint damage)
    {
        health -= (int)damage;
        if (health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Enables ragdoll effect
    /// </summary>
    public void ActivateRagdoll()
    {
        foreach (Collider collider in characterColliders)
        {
            collider.isTrigger = false;
            collider.attachedRigidbody.isKinematic = false;
        }
    }

    /// <summary>
    /// Disables ragdoll effect
    /// </summary>
    public void DeactivateRagdoll()
    {
        foreach (Collider collider in characterColliders)
        {
            collider.isTrigger = true;
            collider.attachedRigidbody.isKinematic = true;
        }
    }

    #endregion

    #region Private Methods

    private IEnumerator AttackCoroutine()
    {
        List<NavNode> currentPath = null;
        IEnumerator previousFollowPathCoroutine = null;
        
        bool requestedPath = false;
        Vector3 latestPathTarget = Vector3.zero;
        Vector3 latestCompletePathTarget = Vector3.zero;

        float deltaTime = 0f;
        float timeStep = EnemyManager.Instance.MovementUpdateTimeStep;

        void OnPathFound(List<NavNode> newPath)
        {
            currentPath = newPath;
            latestPathTarget = PlayerManager.Instance.Player.transform.position;

            //Follow new path
            FollowNewPath(Pathfinding.SimplifyPath(currentPath), previousFollowPathCoroutine, out previousFollowPathCoroutine);

            HelperFunctions.DrawPath(currentPath, Color.black, 0.05f);
            HelperFunctions.DrawPath(Pathfinding.SimplifyPath(currentPath), Color.blue, 0.05f);

            requestedPath = false;
        }

        while (true)
        {
            while (attack)
            {
                deltaTime += Time.deltaTime;
                if (deltaTime > timeStep)
                {
                    deltaTime = 0f;

                    if (!requestedPath)
                    {
                        switch (PathTypeNeeded(latestPathTarget, latestCompletePathTarget))
                        {
                            case PathType.Complete:
                                PathfindingManger.Instance.RequestPath(new PathfindingManger.PathRequest(transform, PlayerManager.Instance.Player.transform, 
                                    newPath =>
                                    {
                                        currentPathIndex = 0;
                                        OnPathFound(newPath);
                                        latestCompletePathTarget = latestPathTarget;
                                    }));
                                requestedPath = true;
                                break;
                            
                            case PathType.Fast:
                                PathfindingManger.Instance.RequestPathFast(currentPath, new PathfindingManger.PathRequest(transform, PlayerManager.Instance.Player.transform, 
                                    newPath =>
                                    {
                                        OnPathFound(newPath);
                                    }));
                                requestedPath = true;
                                break;
                        }
                    }
                }
                
                yield return null;
            }

            yield return null;
        }
    }
    
    private void FollowNewPath(List<NavNode> newPath, IEnumerator previousCoroutine, out IEnumerator currentCoroutine)
    {
        //Stop following previous path
        if (previousCoroutine != null)
        {
            StopCoroutine(previousCoroutine);
        }

        //Start following new path
        currentCoroutine = FollowPathCoroutine(newPath, 1f, 0.67f, 133.33f);
        StartCoroutine(currentCoroutine);
    }

    /// <summary>
    /// Smoothly follows path currentPath according to smoothing parameters
    /// </summary>
    /// <param name="currentPath">The path to follow</param>
    /// <param name="d">Increasing d decreases path follow accuracy but increases maximum smoothness of the path</param>
    /// <param name="angleAccelRate">Increasing angleAccelRate increases the 'snappiness' of the turning</param>
    /// <param name="maxTurnRate">Decreasing maxTurnRate increases turn radii and overall path smoothness</param>
    /// <returns></returns>
    private IEnumerator FollowPathCoroutine(List<NavNode> currentPath, float d, float angleAccelRate, float maxTurnRate)
    {
        //Find nearest node that hasn't already been passed
        int startIndex = currentPath.Count - 1;
        if (currentPath.Count > 2)
        {
            startIndex = 0;
            bool passedNextNode;
            do
            {
                startIndex++;
                
                Vector2 positionRelativeToNode = HelperFunctions.XZVector(transform.position - currentPath[startIndex].worldPosition);
                Vector2 nextPathSegment = HelperFunctions.XZVector(currentPath[startIndex - 1].worldPosition - currentPath[startIndex].worldPosition);
                passedNextNode = Vector2.Angle(positionRelativeToNode, nextPathSegment) >= 90f;
            } while (startIndex + 1 < currentPath.Count - 1 && (passedNextNode || startIndex < currentPathIndex - 1));
        }

        for (int i = startIndex; i < currentPath.Count; i++)
        {
            //Keep track of where he is in the path
            currentPathIndex = i;

            //Initialize loop variables
            float turnSpeed = 0f;
            bool crossedThreshold = false;
            float initialTheta = 0f;
            float angleAcceleration = angleAccelRate * normalMoveSpeed;
            float maxTurnSpeed = maxTurnRate * normalMoveSpeed;

            float deltaTime = 0f;
            float timeStep = EnemyManager.Instance.MovementUpdateTimeStep;
            
            while (!crossedThreshold)
            {
                deltaTime += Time.deltaTime;
                if (deltaTime > timeStep)
                {
                    Vector2 forward = HelperFunctions.XZVector(transform.forward);
                    Vector2 desiredDirection = HelperFunctions.XZVector(currentPath[i].worldPosition - transform.position);
                    float theta = Vector2.SignedAngle(forward, desiredDirection);
                    
                    if (Mathf.Abs(theta) > maxTurnSpeed * deltaTime)//Check if enemy is facing next node
                    {
                        //Update the angle the enemy must turn to face next node
                        if (initialTheta == 0f || Mathf.Abs(theta) > initialTheta)
                        {
                            initialTheta = Mathf.Abs(theta);
                        }

                        //Smoothly increase and decrease turnSpeed between 0 and maxTurnSpeed
                        if (Mathf.Abs(theta) > 0.5f * initialTheta && Mathf.Abs(turnSpeed) < maxTurnSpeed)
                        {
                            turnSpeed += -Mathf.Sign(theta) * angleAcceleration * maxTurnSpeed * deltaTime;
                        }
                        else if (Mathf.Abs(theta) < 0.5f * initialTheta && Mathf.Abs(turnSpeed) > angleAcceleration * maxTurnSpeed * deltaTime / 2f)//Check if we can decrease turnSpeed without making it negative
                        {
                            turnSpeed -= -Mathf.Sign(theta) * angleAcceleration * maxTurnSpeed * deltaTime;
                        }
                        else
                        {
                            initialTheta = 0f;
                        }
                    }
                    else
                    {
                        //Stop turning if facing next node
                        turnSpeed = 0f;
                    }

                    //Move enemy
                    Vector3 direction = (Quaternion.Euler(0f, deltaTime * turnSpeed, 0f) * transform.forward + AvoidObstacles()).normalized;
                    transform.position += deltaTime * normalMoveSpeed * direction;
                    Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + deltaTime * normalMoveSpeed * direction, Color.magenta, 35f);
                    
                    //Rotate Enemy
                    transform.Rotate(new Vector3(0f, deltaTime * turnSpeed, 0f));

                    //Math for checking when to start turning toward next node
                    Vector2 hypotenuse = HelperFunctions.XZVector(transform.position - currentPath[i].worldPosition);
                    Vector2 pathSegment = HelperFunctions.XZVector(currentPath[i - 1].worldPosition - currentPath[i].worldPosition);
                    float phi = Mathf.Deg2Rad * Vector2.Angle(hypotenuse, pathSegment);
                    
                    crossedThreshold = !(hypotenuse.magnitude * Mathf.Cos(phi) > d);
                    
                    //Reset delta time
                    deltaTime = 0f;
                }

                yield return null;
            }
        }
    }

    private PathType PathTypeNeeded(Vector3 latestNewPathTarget, Vector3 latestCompletePathTarget)
    {
        Vector3 playerPosition = PlayerManager.Instance.Player.transform.position;
        if (latestNewPathTarget == Vector3.zero)
        {
            return PathType.Complete;
        }
        if ((playerPosition - latestNewPathTarget).magnitude > 0.5f * Mathf.Sqrt(2f))
        {
            if ((playerPosition - latestCompletePathTarget).magnitude > 10f)
            {
                return PathType.Complete;
            }
            
            return PathType.Fast;
        }

        return PathType.Null;
    }

    private Vector3 AvoidObstacles()
    {
        Vector3 position = transform.position;
        Vector2 forward = HelperFunctions.XZVector(transform.forward);
        Vector3 output = Vector3.zero;
        
        List<List<Obstacle>> nearbyObstacles = EnemyManager.Instance.ObstacleHashGrid.GetINearbyItems(position, 2);
        Obstacle currentObstacle;
        
        for (int i = 0; i < nearbyObstacles.Count; i++)
        {
            for (int j = 0; j < nearbyObstacles[i].Count; j++)
            {
                currentObstacle = nearbyObstacles[i][j];
                
                Vector2 dir = HelperFunctions.XZVector(currentObstacle.Collider.ClosestPoint(position) - position);
                float dist = dir.magnitude;
                if (dist < avoidRadius && !currentObstacle.Collider.Equals(thisObstacle.Collider))
                {
                    Vector3 outputDirection = -new Vector3(dir.x, 0f, dir.y).normalized;
                    float angle = Mathf.Rad2Deg * Mathf.Acos((forward.x * dir.x + forward.y * dir.y) / dist);
                    float turnForce = 1f - angle / 90f;
                    float pushForce = 1f - dist / avoidRadius;

                    output += (1f + turnForce) * pushForce * avoidancePower * outputDirection;
                }
            }
        }
        return output;
    }
    
    /// <summary>
    /// Calls the Die coroutine
    /// </summary>
    private void Die()
    {
        StartCoroutine(DieCoroutine());
    }

    /// <summary>
    /// Deals with death of this enemy
    /// </summary>
    private IEnumerator DieCoroutine()
    {
        ActivateRagdoll();
        yield return new WaitForSeconds(despawnTime);
        Destroy(gameObject);
    }

    #endregion

    #region Unity Methods
    
    public void Start()
    {
        DeactivateRagdoll();
        StartCoroutine(AttackCoroutine());
    }

    [SerializeField] private float rehashTimeStep = 0f;
    private float timer = 0f;
    public void Update()
    {
        timer += Time.deltaTime;
        if (timer >= EnemyManager.Instance.RehashTimestep)
        {
            //TODO - Add rehash scheduling
            timer = 0f;
            EnemyManager.Instance.ObstacleHashGrid.Rehash(thisObstacle, thisObstacle.Collider.transform.position);
        }
    }

    #endregion
}
