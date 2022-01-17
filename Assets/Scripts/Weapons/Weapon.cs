using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum WeaponIdEnum
    {
        Null,
        PistolTactical,
        RifleTactical,
        ShotgunPumpAction,
    }

    public enum WeaponTierEnum
    {
        Primary,
        Secondary,
        Tertiary
    }

    #region Properties

    public WeaponIdEnum WeaponId => weaponId;
    public WeaponTierEnum WeaponTier => weaponTier;

    public Gun GunScript => gunScript;

    #endregion

    #region Inspector Controlled Variables

    [SerializeField] private WeaponIdEnum weaponId = WeaponIdEnum.Null;
    [SerializeField] private WeaponTierEnum weaponTier = WeaponTierEnum.Primary;
    [SerializeField] private Gun gunScript = null;

    #endregion

    #region Public Methods

    /// <summary>
    /// Uses the weapon for BLOOD
    /// </summary>
    public void Fire()
    {
        if (gunScript != null)
        {
            gunScript.Shoot();
        }
    }

    /// <summary>
    /// When called allows semi auto weapons to be used again
    /// </summary>
    public void ResetFire()
    {
        if (gunScript != null)
        {
            gunScript.ResetTrigger();
        }
    }

    /// <summary>
    /// Reload the weapon if applicable
    /// </summary>
    public void Reload()
    {
        if (gunScript != null)
        {
            gunScript.Reload();
        }
    }

    #endregion
}
