using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    #region Inspector Controlled Variables

    [SerializeField] private PlayerUI playerUI = null;

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to call PlayerUI to show the weapon wheel based on toggle parameter
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleWeaponWheel(bool toggle)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        Cursor.lockState = toggle ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = toggle;
        GameManager.Instance.SetPlayerCameraControl(!toggle);
        playerUI.ToggleWeaponWheel(toggle);
    }

    /// <summary>
    /// Accesses and returns the player crosshair position held within PlayerUI
    /// </summary>
    /// <returns>Player crosshair position vector 3</returns>
    public Vector3 GetPlayerCrosshairPosition()
    {
        return playerUI.PlayerCrosshairPosition;
    }

    /// <summary>
    /// Launches the DisplayHitmarkerCoroutine
    /// </summary>
    /// <param name="hitLocations"></param>
    public void DisplayHitmarker(List<Vector3> hitLocations)
    {
        StartCoroutine(DisplayHitmarkerCoroutine(hitLocations));
    }

    
    public void UpdateWeaponUI(uint roundsLoaded, uint magazineRoundLimit, Weapon activeWeapon)
    {
        playerUI.UpdateAmmoText(roundsLoaded, magazineRoundLimit, activeWeapon);
        playerUI.UpdateWeaponText(activeWeapon);
    }

    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Displays hitmarker(s) for all hits in hitLocations
    /// </summary>
    /// <param name="hitLocations"></param>
    /// <returns></returns>
    private IEnumerator DisplayHitmarkerCoroutine(List<Vector3> hitLocations)
    {
        for (int i = 0; i < hitLocations.Count; i++)
        {
            if (hitLocations[i] != Vector3.zero)//Unsuccessfull raycasts will appear as Vector3.zero
            {
                StartCoroutine(playerUI.DisplayHitmarkerCoroutine(hitLocations[i]));
                
                if (i % 2 == 0)//Only play sound for every other hitmarker
                {
                    playerUI.PlayHitmarkerSound();
                    yield return new WaitForSeconds(Random.Range(0.025f, 0.045f));
                }
            }
        }
    }
    
    #endregion
}
