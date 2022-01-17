using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Player : MonoBehaviour
{
    #region Enumerated Types

    enum MovementState
    {
        Foot,
        SwimmingSurface,
        SwimmingDiving,
        Car,
    }

    enum AimState
    {
        AimedIn,
        Normal
    }

    #endregion

    #region Properties

    public Weapon ActiveWeapon => activeWeapon;

    #endregion

    #region Inspector Controlled Variables

    [SerializeField] private float normalMoveSpeed = 0;
    [SerializeField] private float sprintSpeedModifier = 0;
    [SerializeField] private float verticalLookSpeed = 0;
    [SerializeField] private float horizontalLookSpeed = 0;

    [SerializeField] private int maxHealth = 0;
    [SerializeField] private float maxUnderwaterSeconds = 0;
    [SerializeField] private float groundJumpForce = 0;
    [SerializeField] private float waterJumpForce = 0;
    [SerializeField] private uint waterDrownDamage = 0;
    
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private Vector3 playerCameraPositionNormal = Vector3.zero;
    [SerializeField] private Vector3 playerCameraPositionAim = Vector3.zero;
    
    [SerializeField] private GameObject playerBody = null;
    [SerializeField] private BoxCollider playerFrictionCollider = null;
    [SerializeField] private BoxCollider playerFrictionlessCollider = null;
    [SerializeField] private Rigidbody playerRigidBody = null;

    [SerializeField] private List<Weapon.WeaponIdEnum> playerWeaponObjectsId = null;
    [SerializeField] private List<Weapon> playerWeaponObjects = null;

    #endregion

    #region Private Members

    private int currentHealth = 0;
    private float remainingUnderwaterSeconds = 0;
    private float currentMoveSpeed = 0f;
    
    private MovementState movementState = MovementState.Foot;
    private AimState aimState = AimState.Normal;

    private Car carToEnter = null;
    private Car carInsideOf = null;

    private Weapon weaponToPickup = null;
    private Weapon activeWeapon = null;

    private Collider swimWaterSurfaceCollider = null;
    private bool outOfBreathCoroutineRunning = false;

    private Dictionary<Weapon.WeaponTierEnum, List<Weapon.WeaponIdEnum>> inventoryWeapons =
        new Dictionary<Weapon.WeaponTierEnum, List<Weapon.WeaponIdEnum>>()
        {
            {Weapon.WeaponTierEnum.Primary, new List<Weapon.WeaponIdEnum>()},
            {Weapon.WeaponTierEnum.Secondary, new List<Weapon.WeaponIdEnum>()},
            {Weapon.WeaponTierEnum.Tertiary, new List<Weapon.WeaponIdEnum>()},
        };
    private Dictionary<Weapon.WeaponTierEnum, int> inventoryWeaponsSelection =
        new Dictionary<Weapon.WeaponTierEnum, int>()
        {
            {Weapon.WeaponTierEnum.Primary, 0},
            {Weapon.WeaponTierEnum.Secondary, 0},
            {Weapon.WeaponTierEnum.Tertiary, 0},
        };

    private Dictionary<Gun.AmmoType, uint> inventoryAmmo = new Dictionary<Gun.AmmoType, uint>
    {
        {Gun.AmmoType.Pistol, 100},
        {Gun.AmmoType.AssaultRifle, 0},
        {Gun.AmmoType.Shotgun, 100},
        {Gun.AmmoType.SniperRifle, 0},
    };

    private const float SWIM_SURFACE_TO_DIVE_DISTANCE = 2.0f;
    private const float SWIM_DIVE_TO_SURFACE_DISTANCE = 1.0f;
    private const float SWIM_SURFACE_DIVE_SPEED = 3.0f;

    #endregion

    #region Public Methods

    /// <summary>
    /// Deals damage to the player
    /// </summary>
    /// <param name="damage">Amount of damage to deal</param>
    public void TakeDamage(uint damage)
    {
        Debug.Log($"Dealing {damage} damage to player");
        currentHealth -= (int) damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// When called the player will jump if possible
    /// </summary>
    public void Jump()
    {
        if (!IsGrounded() || movementState != MovementState.Foot)
        {
            return;
        }
        
        playerRigidBody.AddForce(Vector3.up * groundJumpForce, ForceMode.Impulse);
    }

    /// <summary>
    /// When called the player will try to toggle sprint according to toggle param and IsGrounded/AimState
    /// </summary>
    /// <param name="toggle">True if turning sprint on false if turning it off</param>
    public void Sprint(bool toggle)
    {
        if (movementState != MovementState.Foot)
        {
            return;
        }
        
        if (toggle && IsGrounded() && aimState != AimState.AimedIn) //Only allow player to sprint when on ground and not aiming
        {
            currentMoveSpeed = normalMoveSpeed * sprintSpeedModifier;
        }
        else
        {
            currentMoveSpeed = normalMoveSpeed;
        }
    }

    /// <summary>
    /// When called the player will interact according to colliders he's touching
    /// </summary>
    public void Interact()
    {
        if (!GameManager.Instance.PlayerHasControl)
        {
            return;
        }
        
        //Get all colliders
        Collider[] hitColliders = Physics.OverlapBox(
            transform.position + new Vector3(0f, playerFrictionCollider.size.y/2, 0f), new Vector3(0.5f, 1.0f, 0.5f), Quaternion.identity);

        
        foreach (Collider col in hitColliders)
        {
            switch (col.tag)
            {
                case Constants.TAG_CAR_INTERACT_COLLIDER:
                    if (carToEnter == null && movementState == MovementState.Foot)
                    {
                        carToEnter = col.gameObject.GetComponent<CarInteractCollider>().Car;   
                    }
                    break;

                case Constants.TAG_WEAPON_INTERACT_COLLIDER:
                    if (weaponToPickup == null && movementState == MovementState.Foot)
                    {
                        weaponToPickup = col.gameObject.GetComponent<WeaponInteractCollider>().Weapon;
                    }
                    break;
            }
        }
        
        if (carToEnter != null)
        {
            ChangeMovementState(MovementState.Car);
        }
        else if (carInsideOf != null)
        {
            ChangeMovementState(MovementState.Foot);
        }
        else if (weaponToPickup != null)
        {
            PickUpWeapon(weaponToPickup);
        }
       
    }

    /// <summary>
    /// When called the player will reload the equipped weapon
    /// </summary>
    public void Reload()
    {
        if (activeWeapon == null)
        {
            return;
        }

        activeWeapon.Reload();
    }

    /// <summary>
    /// Changes the active weapon to the 1st weapon in the given tier
    /// If already on that tier, cycles through them
    /// </summary>
    /// <param name="newWeaponTier">Weapon tier we're switching to or cycling within</param>
    public void EquipWeaponByTier(Weapon.WeaponTierEnum newWeaponTier)
    {
        //TODO - cancel reload here

        if (activeWeapon != null && newWeaponTier == activeWeapon.WeaponTier) //Cycle through the same tier
        {
            inventoryWeaponsSelection[newWeaponTier] = (inventoryWeaponsSelection[newWeaponTier] + 1) % inventoryWeapons[newWeaponTier].Count;
        }

        if (inventoryWeapons[newWeaponTier].Count > 0) //If there's something to select in that tier
        {
            Weapon.WeaponIdEnum newWeaponId = inventoryWeapons[newWeaponTier][inventoryWeaponsSelection[newWeaponTier]];
            int indexOfWeaponObject = playerWeaponObjectsId.IndexOf(newWeaponId);

            if (activeWeapon != null)
            {
                activeWeapon.gameObject.SetActive(false);
            }

            activeWeapon = playerWeaponObjects[indexOfWeaponObject];
            activeWeapon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Tries to the active weapon to the one specified by newWeaponId
    /// </summary>
    /// <param name="newWeaponId"></param>
    public void EquipWeaponById(Weapon.WeaponIdEnum newWeaponId)
    {
        //TODO - cancel reload here

        if (newWeaponId == Weapon.WeaponIdEnum.Null)
        {
            //TODO - sheathe weapon or something lol
            return;
        }

        int indexOfWeaponObject = playerWeaponObjectsId.IndexOf(newWeaponId);
        Weapon.WeaponTierEnum newWeaponTier = playerWeaponObjects[indexOfWeaponObject].WeaponTier;
        int indexOfWeaponInventory = inventoryWeapons[newWeaponTier].IndexOf(newWeaponId);

        if (indexOfWeaponInventory == -1)
        {
            return;
        }

        if (activeWeapon != null)
        {
            activeWeapon.gameObject.SetActive(false);
        }

        activeWeapon = playerWeaponObjects[indexOfWeaponObject];
        activeWeapon.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called to see how much ammo of a given type the player has
    /// </summary>
    /// <param name="type">Ammo type for peek</param>
    /// <returns>How much ammo there is for that type</returns>
    public uint PeekAmmo(Gun.AmmoType type)
    {
        return inventoryAmmo[type];
    }

    /// <summary>
    /// Called from gun to request ammo for reload
    /// </summary>
    /// <param name="type">Ammo type for request</param>
    /// <param name="amount">Amount for request</param>
    /// <returns>Amount of ammo given</returns>
    public uint RequestAmmo(Gun.AmmoType type, uint amount)
    {
        if (inventoryAmmo[type] >= amount)
        {
            inventoryAmmo[type] -= amount;
            return amount;
        }

        uint output = inventoryAmmo[type];
        inventoryAmmo[type] = 0;
        return output;
    }

    /// <summary>
    /// Sets whether the player rigidbody should respond to physics collisions
    /// </summary>
    /// <param name="set">What to set it to</param>
    public void SetRigidbodyCollisions(bool set)
    {
        playerRigidBody.isKinematic = !set;
        playerRigidBody.detectCollisions = set;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Called to kill the player
    /// </summary>
    private void Die()
    {
        Debug.Log("dead lol");
    }

    /// <summary>
    /// Called each frame to move the player
    /// </summary>
    private void MovePlayer()
    {
        if (!GameManager.Instance.PlayerHasControl || !GameManager.Instance.PlayerHasMovementControl)
        {
            return;
        }
        
        if (movementState == MovementState.Foot)
        {
            Vector2 movementVector = InputManager.Instance.MovementVector;

            //Translate the player
            Vector3 movVec = new Vector3(movementVector.x, 0f, movementVector.y);
            movVec = transform.TransformDirection(movVec);
            transform.position += Time.deltaTime * currentMoveSpeed * movVec;
        
            //Rotate the body to face direction of movement - TODO FINISH THIS
            playerBody.transform.LookAt(movementVector);
        }
        else if (movementState == MovementState.SwimmingSurface)
        {
            Vector2 movementVector = InputManager.Instance.MovementVector;
            
            //Translate the player
            Vector3 movVec = new Vector3(movementVector.x, 0, movementVector.y);
            movVec = transform.TransformDirection(movVec);
            transform.position += Time.deltaTime * currentMoveSpeed * movVec;
            
            if (InputManager.Instance.LeftControlButton && SwimCanDive()) //Swimming and pressed lctrl - dive here
            {
                ChangeMovementState(MovementState.SwimmingDiving);
            }
            else if (InputManager.Instance.SpaceButton && SwimCanExit())
            {
                ChangeMovementState(MovementState.Foot);
            }
        }
        else if (movementState == MovementState.SwimmingDiving)
        {
            Vector2 movementVector = InputManager.Instance.MovementVector;
            bool ascend = InputManager.Instance.SpaceButton;
            bool descend = InputManager.Instance.LeftControlButton;
            float verticalMoveValue = 0f;

            if (ascend && !descend)
            {
                verticalMoveValue = 1.0f;
            }
            else if (descend && !ascend)
            {
                verticalMoveValue = -1.0f;
            }

            Vector3 movVec = new Vector3(movementVector.x, verticalMoveValue, movementVector.y);
            movVec = transform.TransformDirection(movVec);
            transform.position += Time.deltaTime * currentMoveSpeed * movVec;

            if (InputManager.Instance.SpaceButton && SwimCanSurface())
            {
                ChangeMovementState(MovementState.SwimmingSurface);
            }
        }
    }

    /// <summary>
    /// Called each frame to move the camera and rotate the player
    /// </summary>
    private void MoveCamera()
    {
        if (!GameManager.Instance.PlayerHasControl || !GameManager.Instance.PlayerHasCameraControl ||
            (movementState != MovementState.Foot && movementState != MovementState.SwimmingSurface && movementState != MovementState.SwimmingDiving))
        {
            return;
        }

        Vector2 lookVector = InputManager.Instance.LookVector;

        //Rotate the player around y axis
        Vector3 playerRotateVector = new Vector3(0f, 0.001f * horizontalLookSpeed * lookVector.x, 0f);
        transform.Rotate(playerRotateVector);

        //Rotate camera around x axis
        float cameraRotateValue = 0.001f * verticalLookSpeed * -lookVector.y;
        Vector3 currentCameraRotation = playerCamera.transform.eulerAngles;
        if (currentCameraRotation.x + cameraRotateValue >= 20f && currentCameraRotation.x + cameraRotateValue <= 340f) //Clamp the x axis camera rotation
        {
            cameraRotateValue = 0f;
        }
        playerCamera.transform.Rotate(new Vector3(cameraRotateValue, 0f, 0f));
    }

    /// <summary>
    /// Called each frame to zoom or unzoom the camera
    /// </summary>
    private void TranslateCameraForAim()
    {
        if (movementState != MovementState.Foot)
        {
            return;
        }

        int aimValue = (int)InputManager.Instance.RightMouseButton;
        Vector3 currentCameraPosition = playerCamera.transform.localPosition;
        Vector3 targetCameraPosition = (aimValue == 1) ? playerCameraPositionAim : playerCameraPositionNormal;
        float distance = Vector3.Distance(currentCameraPosition, targetCameraPosition);

        if (distance == 0f)
        {
            return; //Already at the target, just leave
        }

        //Set the aim state
        if (aimValue == 1)
        {
            aimState = AimState.AimedIn;
            Sprint(false); //When aiming in stop sprinting
        }
        else
        {
            aimState = AimState.Normal;
        }

        if (distance < 0.001f)
        {
            playerCamera.transform.localPosition = targetCameraPosition; //Close enough to tp
            return;
        }

        //If we made it here then we translate the camera towards the target
        Vector3 translateDirection = targetCameraPosition - currentCameraPosition;
        Vector3 translateValue = 10f * Time.deltaTime * translateDirection;
        playerCamera.transform.Translate(translateValue);
    }

    /// <summary>
    /// Polls the lmb value and uses the active weapon if held
    /// Also resets fire for semi autos when let go
    /// </summary>
    private void FireWeapon()
    {
        if (activeWeapon == null)
        {
            return;
        }

        int lmbAction = (int)InputManager.Instance.LeftMouseButton;

        if (lmbAction == 1)
        {
            Sprint(false); //Stop sprinting when a shot was fired
            activeWeapon.Fire();
        }
        else
        {
            activeWeapon.ResetFire();
        }
    }

    /// <summary>
    /// Checks collision between player and ground
    /// </summary>
    /// <returns>Whether the player is grounded</returns>
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, playerFrictionCollider.size.x + 0.05f, 
            layerMask: ~LayerMask.GetMask(Constants.LAYER_NAME_PLAYER));
    }

    /// <summary>
    /// Picks up a weapon and adds it to the player inventory
    /// </summary>
    /// <param name="weapon">The weapon to pick up</param>
    private void PickUpWeapon(Weapon weapon)
    {
        weaponToPickup = null;
        
        if (weapon == null)
        {
            return;
        }

        if (!inventoryWeapons[weapon.WeaponTier].Contains(weapon.WeaponId)) //Don't have this weapon, add it to inventory
        {
            inventoryWeapons[weapon.WeaponTier].Add(weapon.WeaponId);
            Destroy(weapon.gameObject);
        }
        else //Already have it in inventory, increase ammo count
        {
            //TODO - increment ammo
        }
    }

    /// <summary>
    /// Changes the movement state to the specified argument and makes any other
    /// changes associated with that movement state (ex. locking y axis) based on movementState
    /// </summary>
    /// <param name="state"></param>
    private void ChangeMovementState(MovementState state)
    {
        switch (movementState)
        {
            case MovementState.Foot:
                switch (state)
                {
                    case MovementState.Car:
                        carInsideOf = carToEnter;
                        carToEnter = null;
                        carInsideOf.ChangeToPlayerControl();
                        movementState = state;
                        break;
                    
                    case MovementState.SwimmingSurface:
                        GameManager.Instance.SetPlayerMovementControl(false);
                        StartCoroutine(SwimFootToSurfaceAnimation(delegate
                        {
                            playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                            playerRigidBody.useGravity = false;
                            movementState = state;
                            remainingUnderwaterSeconds = maxUnderwaterSeconds;
                            GameManager.Instance.SetPlayerMovementControl(true);
                        }));
                        break;
                }
                break;
            
            case MovementState.Car:
                switch (state)
                {
                    case MovementState.Foot:
                        carInsideOf.PlayerExitCar();
                        carInsideOf = null;
                        CameraManager.Instance.SetActiveCamera(playerCamera);
                        movementState = state;
                        break;
                }
                break;
            
            case MovementState.SwimmingSurface:
                switch (state)
                {
                    case MovementState.SwimmingDiving:
                        GameManager.Instance.SetPlayerMovementControl(false);
                        playerRigidBody.useGravity = false;
                        playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                        StartCoroutine(SwimSurfaceToUnderwaterAnimation(delegate
                        {
                            GameManager.Instance.SetPlayerMovementControl(true);
                            movementState = state;
                        }));
                        break;
                    
                    case MovementState.Foot:
                        playerRigidBody.useGravity = true;
                        swimWaterSurfaceCollider = null;
                        playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                        playerRigidBody.AddForce(waterJumpForce * Vector3.up, ForceMode.Impulse);
                        movementState = state;
                        break;
                }
                break;
            
            case MovementState.SwimmingDiving:
                switch (state)
                {
                    case MovementState.SwimmingSurface:
                        GameManager.Instance.SetPlayerMovementControl(false);
                        StartCoroutine(SwimUnderwaterToSurfaceAnimation(delegate
                        {
                            GameManager.Instance.SetPlayerMovementControl(true);
                            movementState = state;
                            remainingUnderwaterSeconds = maxUnderwaterSeconds;
                        }));
                        break;
                }
                break;
        }
    }
    
    /// <summary>
    /// Returns whether a player swimming on the surface can dive underwater
    /// </summary>
    /// <returns></returns>
    private bool SwimCanDive()
    {
        if (GameManager.Instance.PlayerHasControl && GameManager.Instance.PlayerHasMovementControl && movementState == MovementState.SwimmingSurface)
        {
            return !Physics.Raycast(transform.position, Vector3.down, playerFrictionCollider.size.y + SWIM_SURFACE_TO_DIVE_DISTANCE, 
                ~LayerMask.GetMask(Constants.LAYER_NAME_PLAYER));
        }

        return false;
    }

    /// <summary>
    /// Returns whether a player swimming underwater can surface
    /// </summary>
    /// <returns></returns>
    private bool SwimCanSurface()
    {
        if (GameManager.Instance.PlayerHasControl && GameManager.Instance.PlayerHasMovementControl && movementState == MovementState.SwimmingDiving)
        {
            Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit,playerFrictionCollider.size.y + SWIM_DIVE_TO_SURFACE_DISTANCE, 
                ~LayerMask.GetMask(Constants.LAYER_NAME_PLAYER));
            if (hit.transform != null && hit.transform.CompareTag(Constants.TAG_WATER_SURFACE_COLLIDER))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether close enough to ground while swimming on surface
    /// </summary>
    /// <returns></returns>
    private bool SwimCanExit()
    {
        //TODO - make it stop swimming if it gets too shallow
        
        if (GameManager.Instance.PlayerHasControl && GameManager.Instance.PlayerHasMovementControl && movementState == MovementState.SwimmingSurface)
        {
            Collider[] hitColliders = Physics.OverlapBox(
                transform.position + new Vector3(0f, playerFrictionCollider.size.y/2, 0f), new Vector3(0.5f, 1.0f, 0.5f),
                Quaternion.identity, LayerMask.GetMask(Constants.LAYER_NAME_GROUND));

            if (hitColliders.Length > 0)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Called every frame to count remaining time underwater
    /// Start OutOfBreathCoroutine when out of breath to drown
    /// </summary>
    private void UnderwaterBreath()
    {
        if (movementState != MovementState.SwimmingDiving || outOfBreathCoroutineRunning)
        {
            return;
        }

        remainingUnderwaterSeconds -= Time.deltaTime;
        if (remainingUnderwaterSeconds < 0f)
        {
            outOfBreathCoroutineRunning = true;
            StartCoroutine(OutOfBreathCoroutine());
        }
    }

    /// <summary>
    /// When called will deal waterDrownDamage damage every 2.0 seconds
    /// until player is dead or surfaces
    /// </summary>
    /// <returns></returns>
    private IEnumerator OutOfBreathCoroutine()
    {
        float timer = 0f;
        while (movementState == MovementState.SwimmingDiving)
        {
            timer += Time.deltaTime;
            if (timer >= 2.0f)
            {
                TakeDamage(waterDrownDamage);
                timer -= 2.0f;
            }
            yield return null;
        }

        outOfBreathCoroutineRunning = false;
        remainingUnderwaterSeconds = maxUnderwaterSeconds;
    }

    /// <summary>
    /// Animation which fires when jumping into the water
    /// Sets the y position to the water surface
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator SwimFootToSurfaceAnimation(Action callback)
    {
        float targetY = swimWaterSurfaceCollider.transform.position.y;
        while (Math.Abs(playerRigidBody.transform.position.y - targetY) < 1.4f)
        {
            yield return null;
        }
        
        playerRigidBody.MovePosition(new Vector3(playerRigidBody.transform.position.x, targetY, playerRigidBody.transform.position.z));
        callback.Invoke();
    }

    /// <summary>
    /// Animation which fires when going underwater from the surface
    /// Lowers the y position smoothly
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator SwimSurfaceToUnderwaterAnimation(Action callback)
    {
        float targetY = playerRigidBody.transform.position.y - SWIM_SURFACE_TO_DIVE_DISTANCE;
        while (Math.Abs(playerRigidBody.transform.position.y - targetY) > 0.1f)
        {
            playerRigidBody.MovePosition(SWIM_SURFACE_DIVE_SPEED * Time.deltaTime * Vector3.down + playerRigidBody.position);
            yield return null;
        }
        
        callback.Invoke();
    }

    /// <summary>
    /// Animation which fires when surfacing from underwater
    /// Raises the y position smoothly to the water surface
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator SwimUnderwaterToSurfaceAnimation(Action callback)
    {
        float targetY = swimWaterSurfaceCollider.transform.position.y;
        while (Math.Abs(playerRigidBody.transform.position.y - targetY) > 1.4f)
        {
            playerRigidBody.MovePosition(SWIM_SURFACE_DIVE_SPEED * Time.deltaTime * Vector3.up + playerRigidBody.position);
            yield return null;
        }
        
        callback.Invoke();
    }

    #endregion

    #region Unity Methods

    public void Start()
    {
        CameraManager.Instance.SetActiveCamera(playerCamera);
        currentMoveSpeed = normalMoveSpeed;
        currentHealth = maxHealth;
    }

    public void Update()
    {
        MovePlayer();
        MoveCamera();
        TranslateCameraForAim();
        FireWeapon();
        UnderwaterBreath();
    }

    public void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case Constants.TAG_WATER_SURFACE_COLLIDER:
                //Only use this when hitting the collider from above, otherwise change movement state using the animations
                bool fromAbove = (playerFrictionCollider.transform.position.y + (playerFrictionCollider.size.y)/2 > other.transform.position.y);
                if (fromAbove)
                {
                    swimWaterSurfaceCollider = other;
                    ChangeMovementState(MovementState.SwimmingSurface);
                }
                break;
        }
    }

    #endregion
}
