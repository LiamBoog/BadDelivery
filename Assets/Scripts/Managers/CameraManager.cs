using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    #region Properties

    public Camera ActiveCamera => activeCamera;
    
    #endregion
    
    #region Private Members

    private Camera activeCamera = null;

    #endregion
    
    #region Public Methods

    /// <summary>
    /// Sets the active camera to be the one passed in
    /// </summary>
    /// <param name="camera">The camera to use</param>
    public void SetActiveCamera(Camera camera)
    {
        if (activeCamera != null)
        {
            activeCamera.enabled = false;   
            activeCamera.gameObject.SetActive(false);
        }
        
        activeCamera = camera;
        activeCamera.gameObject.SetActive(true);
        activeCamera.enabled = true;
    }
    
    #endregion
}
