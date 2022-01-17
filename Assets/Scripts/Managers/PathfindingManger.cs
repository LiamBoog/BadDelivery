using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Threading;

public class PathfindingManger : Singleton<PathfindingManger>
{
    public struct PathRequest
    {
        public Transform startPoint;
        public Transform endPoint;
        public Action<List<NavNode>> callback;

        public PathRequest(Transform startPoint, Transform endPoint, Action<List<NavNode>> callback)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            this.callback = callback;
        }
    }

    public struct PathResult
    {
        public List<NavNode> path;
        public Action<List<NavNode>> callback;

        public PathResult(List<NavNode> path, Action<List<NavNode>> callback)
        {
            this.path = path;
            this.callback = callback;
        }
    }

    #region Inspector Controlled Variables
    
    [SerializeField] private NavGrid navGrid = null;
    [SerializeField] private float pathfindingTimestep;

    #endregion

    #region Private Members

    private Queue<PathResult> pathResultQueue = new Queue<PathResult>();
    private Queue<Thread> pathRequestQueue = new Queue<Thread>();
    private bool isProcessingPath = false;

    private float timer;

    #region Properties

    public NavGrid NavGrid => navGrid;

    #endregion

    #endregion

    #region Public Methods
    public void RequestPath(PathRequest pathRequest)
    {
        Vector3 startPosition = pathRequest.startPoint.position;
        Vector3 endPosition = pathRequest.endPoint.position;

        Thread newThread = new Thread(new ThreadStart(
            delegate
            {
                isProcessingPath = true;

                Pathfinding.FindPath(navGrid, startPosition, endPosition,
                newPath =>
                {
                    lock (pathResultQueue)
                    {
                        pathResultQueue.Enqueue(new PathResult(newPath, pathRequest.callback));
                    }

                    isProcessingPath = false;
                });
            }));
        pathRequestQueue.Enqueue(newThread);
    }

    public void RequestPathFast(List<NavNode> path, PathRequest pathRequest)
    {
        Vector3 startPosition = pathRequest.startPoint.position;
        Vector3 endPosition = pathRequest.endPoint.position;
        
        Thread newThread = new Thread(new ThreadStart(
            delegate
            {
                isProcessingPath = true;

                Pathfinding.FindPathFast(navGrid, path, startPosition, endPosition,
                    newPath =>
                {
                    lock (pathResultQueue)
                    {
                        pathResultQueue.Enqueue(new PathResult(newPath, pathRequest.callback));
                    }
                    
                    isProcessingPath = false;
                });
            }));
        pathRequestQueue.Enqueue(newThread);
    }
    
    #endregion
    
    #region Unity Methods
    
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= pathfindingTimestep)
        {
            timer = 0f;
            
            lock (pathRequestQueue)
            {
                if (pathRequestQueue.Count > 0 && !isProcessingPath)
                {
                    pathRequestQueue.Dequeue().Start();
                }
            }
        
            lock (pathResultQueue)
            {
                if (pathResultQueue.Count > 0)
                {
                    PathResult currentResult = pathResultQueue.Dequeue();
                    currentResult.callback(currentResult.path);
                }
            }
        }
    }

    #endregion
}
