using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletShell : MonoBehaviour
{
    #region Inspector Controlled Variables

    [SerializeField] private Rigidbody shellRigidBody = null;

    #endregion
    
    #region Private Members

    private float secondsBeforeDestroy = 5f;
    private float ejectionForce = 3f;
    
    #endregion

    #region Private Methods

    /// <summary>
    /// Applies a force to the shell to eject it from the gun
    /// </summary>
    private void ApplyEjectionForce()
    {
        shellRigidBody.AddRelativeForce(Vector3.right * ejectionForce, ForceMode.Impulse);
        transform.parent = null;
        StartCoroutine(DestroyObject());
    }

    /// <summary>
    /// Destroys the shell after a fixed amount of time
    /// </summary>
    /// <returns></returns>
    private IEnumerator DestroyObject()
    {
        yield return new WaitForSeconds(secondsBeforeDestroy);
        Destroy(gameObject);
    }
    
    #endregion
    
    #region Unity Methods
    
    public void Start()
    {
        ApplyEjectionForce();
    }
    
    #endregion
}
