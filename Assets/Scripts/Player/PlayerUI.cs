using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    #region Enumerated Types

    enum WeaponWheelSection
    {
        Section1,
        Section2,
        Section3,
        Section4,
        Section5,
        Section6
    }

    #endregion

    #region Inspector Controlled Variables

    [SerializeField] private GameObject playerCrosshair = null;
    [SerializeField] private GameObject weaponWheel = null;
    [SerializeField] private Text currentAmmoText = null;
    [SerializeField] private Text maxAmmoText = null;
    [SerializeField] private Text weaponText = null;

    [SerializeField] private GameObject hitmarker = null;
    [SerializeField] private AudioClip hitmarkerSound = null;

    #endregion

    #region Private Members

    private bool weaponWheelActive = false;
    private WeaponWheelSection weaponWheelSelection = WeaponWheelSection.Section1;

    #endregion

    #region Properties

    public Vector3 PlayerCrosshairPosition => playerCrosshair.transform.position;

    #endregion

    #region Public Methods

    /// <summary>
    /// Display/Hide the weapon wheel based on toggle parameter
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleWeaponWheel(bool toggle)
    {
        weaponWheel.SetActive(toggle);
        weaponWheelActive = toggle;

        //When closing the weapon wheel update from the last selection
        if (!toggle)
        {
            WeaponWheelSelectWeapon();
        }
    }
    
    /// <summary>
    /// Displays hitmarker on screen for 0.2 seconds
    /// </summary>
    /// <param name="hitmarkerWorldSpaceLocation"></param>
    /// <returns></returns>
    public IEnumerator DisplayHitmarkerCoroutine(Vector3 hitmarkerWorldSpaceLocation)
    {
        GameObject hitmarkerObject = Instantiate(hitmarker);
        hitmarkerObject.transform.SetParent(gameObject.transform);
        hitmarkerObject.transform.position = CameraManager.Instance.ActiveCamera.WorldToScreenPoint(hitmarkerWorldSpaceLocation);

        float lifeTimer = 0f;
        while (lifeTimer < 0.2f)
        {
            hitmarkerObject.transform.position = CameraManager.Instance.ActiveCamera.WorldToScreenPoint(hitmarkerWorldSpaceLocation);
            lifeTimer += Time.deltaTime;
            yield return null;
        }

        Destroy(hitmarkerObject);
    }

    /// <summary>
    /// Plays hitmarker sound on new audio source
    /// </summary>
    public void PlayHitmarkerSound()
    {
        AudioManager.Instance.CreateSingleUseAudioSource(hitmarkerSound);
    }
    
    public void UpdateAmmoText(uint roundLoaded, uint magazineRoundLimit, Weapon activeWeapon)
    {
        Gun gunScript = activeWeapon != null ? activeWeapon.GunScript : null;
        if (gunScript != null)
        {
            currentAmmoText.gameObject.SetActive(true);
            currentAmmoText.text = roundLoaded.ToString();
            
            maxAmmoText.gameObject.SetActive(true);
            maxAmmoText.text = $"/ {magazineRoundLimit.ToString()}";
        }
        else
        {
            currentAmmoText.gameObject.SetActive(false);
            maxAmmoText.gameObject.SetActive(false);
        }

    }

    public void UpdateWeaponText(Weapon activeWeapon)
    {
        bool isActive = activeWeapon != null;
            
        weaponText.gameObject.SetActive(isActive);
        if (isActive)
        {
            weaponText.text = activeWeapon.name;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Called every frame to update the weapon wheel selection based on where the cursor is
    /// </summary>
    private void UpdateWeaponWheelSelection()
    {
        if (!weaponWheelActive)
        {
            return;
        }

        //Grab the coordinates which are relative to bottom left and center + normalize them
        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        currentMousePosition.x -= Screen.width / 2.0f;
        currentMousePosition.y -= Screen.height / 2.0f;
        currentMousePosition = currentMousePosition.normalized;

        //Angle given is acute so we use y value to check if cursor is in top or bottom half
        float angleFromStart = Vector2.Angle(Vector2.right, currentMousePosition);
        bool topHalf = currentMousePosition.y >= 0f;

        //Wheel is split up like this, each section is 60 degrees
        /*
         *      1   2
         *    3       4
         *      5   6
         */

        if (angleFromStart <= 30f)
        {
            weaponWheelSelection = WeaponWheelSection.Section4;
        }
        else if (angleFromStart > 150f)
        {
            weaponWheelSelection = WeaponWheelSection.Section3;
        }
        else if (angleFromStart <= 90f && topHalf)
        {
            weaponWheelSelection = WeaponWheelSection.Section2;
        }
        else if (angleFromStart <= 90f && !topHalf)
        {
            weaponWheelSelection = WeaponWheelSection.Section6;
        }
        else if (angleFromStart <= 150f && angleFromStart > 90f && topHalf)
        {
            weaponWheelSelection = WeaponWheelSection.Section1;
        }
        else if (angleFromStart <= 150f && angleFromStart > 90f && !topHalf)
        {
            weaponWheelSelection = WeaponWheelSection.Section5;
        }
    }

    /// <summary>
    /// Called when the weapon wheel is closed to update the selected weapon
    /// </summary>
    private void WeaponWheelSelectWeapon()
    {
        //Pick weapon in that section
        Weapon.WeaponIdEnum weaponId = Weapon.WeaponIdEnum.Null;
        switch (weaponWheelSelection)
        {
            case WeaponWheelSection.Section1:
                weaponId = Weapon.WeaponIdEnum.PistolTactical;
                break;
            case WeaponWheelSection.Section2:
                weaponId = Weapon.WeaponIdEnum.RifleTactical;
                break;
            case WeaponWheelSection.Section3:
                weaponId = Weapon.WeaponIdEnum.ShotgunPumpAction;
                break;
            case WeaponWheelSection.Section4:
                break;
            case WeaponWheelSection.Section5:
                break;
            case WeaponWheelSection.Section6:
                break;
        }

        PlayerManager.Instance.Player.EquipWeaponById(weaponId);
    }

    #endregion

    #region Unity Methods

    public void Update()
    {
        UpdateWeaponWheelSelection();
    }

    #endregion
}
