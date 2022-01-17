using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class InputManager : Singleton<InputManager>
{
    #region Accessors

    public float LeftMouseButton => leftMouseButton;
    public float RightMouseButton => rightMouseButton;
    public bool SpaceButton => spaceButton;
    public bool LeftControlButton => leftControlButton;
    public Vector2 LookVector => lookVector;
    public Vector2 MovementVector => movementVector;

    #endregion

    #region Private Members

    private float leftMouseButton;
    private float rightMouseButton;
    private bool spaceButton;
    private bool leftControlButton;
    private Vector2 lookVector;
    private Vector2 movementVector;

    #endregion

    #region Unity Input Events

    /// <summary>
    /// Unity event callback for action: Look
    /// </summary>
    /// <param name="context">Vector2 ReadValue</param>
    public void InputEventLook(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        lookVector = context.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// Unity event callback for action: Move
    /// </summary>
    /// <param name="context">This is gonna have a vector2 in ReadValue</param>
    public void InputEventMove(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }
        
        movementVector = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Unity event callback for action: Jump
    /// </summary>
    /// <param name="context"></param>
    public void InputEventJump(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        spaceButton = (context.ReadValue<float>() >= 1.0f);

        if (context.phase == InputActionPhase.Performed)
        {
            Player player = PlayerManager.Instance.Player;
            player.Jump();   
        }
    }

    /// <summary>
    /// Unity event callback for action: Sprint
    /// </summary>
    /// <param name="context"></param>
    public void InputEventSprint(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        bool buttonStatus = (context.ReadValue<float>() >= 1.0f);

        Player player = PlayerManager.Instance.Player;
        player.Sprint(buttonStatus);
    }
    
    /// <summary>
    /// Unity event callback for action: Weapon wheel
    /// </summary>
    /// <param name="context"></param>
    public void InputEventToggleWeaponWheel(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        bool buttonStatus = (context.ReadValue<float>() >= 1.0f);
        UIManager.Instance.ToggleWeaponWheel(buttonStatus);
    }

    /// <summary>
    /// Unity event callback for action: Aim
    /// </summary>
    /// <param name="context"></param>
    public void InputEventAim(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        rightMouseButton = context.ReadValue<float>();
    }

    /// <summary>
    /// Unity event callback for action: Fire
    /// </summary>
    /// <param name="context"></param>
    public void InputEventFire(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }
        
        leftMouseButton = context.ReadValue<float>();
    }

    /// <summary>
    /// Unity event callback for action: Reload
    /// </summary>
    /// <param name="context"></param>
    public void InputEventReload(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl || context.phase != InputActionPhase.Performed)
        {
            return;
        }

        Player player = PlayerManager.Instance.Player;
        player.Reload();
    }

    /// <summary>
    /// Unity event callback for action: Interact
    /// </summary>
    /// <param name="context"></param>
    public void InputEventInteract(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl || context.phase != InputActionPhase.Performed)
        {
            return;
        }

        Player player = PlayerManager.Instance.Player;
        player.Interact();
    }

    public void InputEventDescend(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }

        leftControlButton = (context.ReadValue<float>() >= 1.0f);
    }

    /// <summary>
    /// Unity event callback for action: TogglePrimaryWeapon
    /// </summary>
    /// <param name="context"></param>
    public void InputEventTogglePrimaryWeapon(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl || context.phase != InputActionPhase.Performed)
        {
            return;
        }

        Player player = PlayerManager.Instance.Player;
        player.EquipWeaponByTier(Weapon.WeaponTierEnum.Primary);
    }
    
    /// <summary>
    /// Unity event callback for action: TogglePrimaryWeapon
    /// </summary>
    /// <param name="context"></param>
    public void InputEventToggleSecondaryWeapon(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl || context.phase != InputActionPhase.Performed)
        {
            return;
        }

        Player player = PlayerManager.Instance.Player;
        player.EquipWeaponByTier(Weapon.WeaponTierEnum.Secondary);
    }
    
    /// <summary>
    /// Unity event callback for action: TogglePrimaryWeapon
    /// </summary>
    /// <param name="context"></param>
    public void InputEventToggleTertiaryWeapon(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.PlayerHasControl || context.phase != InputActionPhase.Performed)
        {
            return;
        }

        Player player = PlayerManager.Instance.Player;
        player.EquipWeaponByTier(Weapon.WeaponTierEnum.Tertiary);
    }

    #endregion
}
