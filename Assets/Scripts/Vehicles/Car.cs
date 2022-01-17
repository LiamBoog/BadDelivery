using System;
using UnityEngine;
using static HelperFunctions;

public class Car : MonoBehaviour
{
    #region Inspector Controlled Variables

    [SerializeField] private Camera carCamera = null;
    [SerializeField] private Transform carCameraContainer = null;

    [SerializeField] private Rigidbody carRigidBody = null;
    [SerializeField] private GameObject frontLeftWheel = null;
    [SerializeField] private GameObject frontRightWheel = null;
    [SerializeField] private GameObject rearLeftWheel = null;
    [SerializeField] private GameObject rearRightWheel = null;
    [SerializeField] private GameObject frontLeftWheelContainer = null;
    [SerializeField] private GameObject frontRightWheelContainer = null;

    [SerializeField] private Transform driverSeatContainer = null;
    [SerializeField] private Vector3 driverExitPosition = Vector3.zero;

    #endregion
    
    #region Private Members

    private float currentSpeed = 0f;
    private float maxForwardSpeed = 15f;
    private float maxReverseSpeed = -5f;
    private float forwardAccelerateSpeed = 3f;
    private float reverseAccelerateSpeed = 2f;
    private float idleDecelerateSpeed = 4f;
    private float brakeSpeed = 8f;
    private float maxWheelTurnAngle = 45f;
    private float wheelTurnSpeed = 300f;
    private float maxCarTurnSpeed = 75f;
    
    private bool playerInControl = false;
    private float steerValue = 0f;
    private float accelerateValue = 0f;
    
    private float horizontalCameraLookSpeed = 5f;
    private float verticalCameraLookSpeed = 5f;
    
    #endregion
    
    #region Public Methods

    /// <summary>
    /// When called seats the player in the car and gives him control
    /// </summary>
    public void ChangeToPlayerControl()
    {
        Player player = PlayerManager.Instance.Player;
        Transform playerTransform = player.transform;
        playerTransform.parent = driverSeatContainer;
        playerTransform.localPosition = Vector3.zero;
        playerTransform.localRotation = Quaternion.identity;
        player.SetRigidbodyCollisions(false);
        CameraManager.Instance.SetActiveCamera(carCamera);
        playerInControl = true;
    }

    /// <summary>
    /// When called the player will be removed from the car
    /// </summary>
    public void PlayerExitCar()
    {
        Player player = PlayerManager.Instance.Player;
        player.transform.localPosition = driverExitPosition;
        player.transform.parent = null;
        player.SetRigidbodyCollisions(true);
        playerInControl = false;
    }
    
    #endregion
    
    #region Private Methods

    /// <summary>
    /// Called every frame to turn the wheels according to player input vector
    /// </summary>
    private void Steer()
    {
        if (playerInControl)
        {
            steerValue = InputManager.Instance.MovementVector.x;
        }
        else
        {
            steerValue = 0f;
        }

        float wheelRotateValue = 0f;
        float currentWheelOrientation = frontLeftWheelContainer.transform.localRotation.eulerAngles.y;
        bool wheelsFacingLeft = currentWheelOrientation > 180f && currentWheelOrientation <= 360f;
        bool wheelsFacingRight = currentWheelOrientation > 0f && currentWheelOrientation <= 180f;

        if (steerValue == 0f) //Center wheels if doing nothing and they aren't centered
        {
            if (Math.Abs(currentWheelOrientation) <= 0.1f) //Close to enough to just snap them into place
            {
                wheelRotateValue = -currentWheelOrientation;
            }
            else if (wheelsFacingLeft) //Turn them clockwise
            {
                wheelRotateValue = wheelTurnSpeed * Time.deltaTime;
            }
            else if (wheelsFacingRight) //Turn them counter clockwise
            {
                wheelRotateValue = -wheelTurnSpeed * Time.deltaTime;
            }
        }
        else //Need to turn them according to player input
        {
            wheelRotateValue = steerValue * wheelTurnSpeed * Time.deltaTime;
        }
        
        //Car is moving and player wants to turn, turn the car
        if (Mathf.Abs(currentSpeed) > 0f && currentWheelOrientation != 0f)
        {
            float turnSpeed = maxWheelTurnAngle * currentSpeed * 0.3f * Time.deltaTime;
            if (turnSpeed > maxCarTurnSpeed * Time.deltaTime)
            {
                turnSpeed = maxCarTurnSpeed * Time.deltaTime;
            }

            if (wheelsFacingLeft) //Turn car counterclockwise
            {
                transform.Rotate(0f, -turnSpeed, 0f);
            }
            else if (wheelsFacingRight) //Turn car clockwise
            {
                transform.Rotate(0f, turnSpeed, 0f);
            }
        }

        //Don't turn the wheels beyond max point
        if (currentWheelOrientation + wheelRotateValue < 360f - maxWheelTurnAngle && currentWheelOrientation + wheelRotateValue > maxWheelTurnAngle)
        {
            wheelRotateValue = 0f;
        }
        
        //Rotate the wheel containers
        frontLeftWheelContainer.transform.Rotate(0f, wheelRotateValue, 0f);
        frontRightWheelContainer.transform.Rotate(0f, wheelRotateValue, 0f);
        
        //Spin the wheels if moving
        float wheelSpinValue = currentSpeed * Time.deltaTime * 50f;
        if (Math.Abs(wheelSpinValue) > 0f)
        {
            frontLeftWheel.transform.Rotate(wheelSpinValue, 0f, 0f);
            frontRightWheel.transform.Rotate(wheelSpinValue, 0f, 0f);
            rearLeftWheel.transform.Rotate(wheelSpinValue, 0f, 0f);
            rearRightWheel.transform.Rotate(wheelSpinValue, 0f, 0f);   
        }
    }

    /// <summary>
    /// Handles acceleration and braking
    /// </summary>
    private void Accelerate()
    {
        if (playerInControl)
        {
            accelerateValue = InputManager.Instance.MovementVector.y;
        }
        else
        {
            accelerateValue = 0f;
        }

        if (accelerateValue == 0f && currentSpeed > 0) //Going forward and idling
        {
            currentSpeed -= idleDecelerateSpeed * Time.deltaTime;
            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;
            }
        }
        else if (accelerateValue == 0f && currentSpeed < 0) //Going backward and idling
        {
            currentSpeed += idleDecelerateSpeed * Time.deltaTime;
            if (currentSpeed > 0f)
            {
                currentSpeed = 0f;
            }
        }
        else if (accelerateValue > 0f && currentSpeed >= 0f) //Going forward and accelerating
        {
            currentSpeed += forwardAccelerateSpeed * Time.deltaTime;
            if (currentSpeed > maxForwardSpeed)
            {
                currentSpeed = maxForwardSpeed;
            }
        }
        else if (accelerateValue < 0f && currentSpeed <= 0f) //Going backward and accelerating
        {
            currentSpeed -= reverseAccelerateSpeed * Time.deltaTime;
            if (currentSpeed < maxReverseSpeed)
            {
                currentSpeed = maxReverseSpeed;
            }
        }
        else if (accelerateValue < 0f && currentSpeed > 0f) //Going forward and braking
        {
            currentSpeed -= brakeSpeed * Time.deltaTime;
        }
        else if (accelerateValue > 0f && currentSpeed < 0f) //Going backward and braking
        {
            currentSpeed += brakeSpeed * Time.deltaTime;
        }

        //Move the car
        Vector3 movVec = new Vector3(0,0, currentSpeed);
        movVec = transform.TransformDirection(movVec);
        carRigidBody.MovePosition(Time.deltaTime * movVec + carRigidBody.position);
    }

    /// <summary>
    /// Rotates the car camera and car camera target
    /// </summary>
    private void MoveCamera()
    {
        if (!GameManager.Instance.PlayerHasControl|| !GameManager.Instance.PlayerHasCameraControl || !playerInControl)
        {
            return;
        }

        Vector2 lookVector = InputManager.Instance.LookVector;

        //Rotate the player around y axis
        Vector3 currentCameraRotation = carCamera.transform.eulerAngles;
        float horizontalRotateValue = horizontalCameraLookSpeed * Time.deltaTime * lookVector.x;
        float verticalRotateValue = verticalCameraLookSpeed * Time.deltaTime * -lookVector.y;
        
        //Clamp the x axis rotation on the camera
        if (currentCameraRotation.x + verticalRotateValue >= 10f && currentCameraRotation.x + verticalRotateValue <= 350f) //Clamp the x axis camera rotation
        {
            verticalRotateValue = 0f;
        }
        
        carCameraContainer.Rotate(0f, horizontalRotateValue, 0f);
        carCamera.transform.Rotate(verticalRotateValue, 0f, 0f);
    }
    
    #endregion

    #region Unity Methods

    public void Update()
    {
        Steer();
        MoveCamera();
    }

    public void FixedUpdate()
    {
        Accelerate(); //Rigidbodies are used so this goes in fixed update
    }

    public void OnCollisionEnter(Collision other)
    {
        switch (other.collider.tag)
        {
            case Constants.TAG_PLAYER:
                break;
            
            case Constants.TAG_ENEMY_LIMB:
                GameObject enemyObject = FindParentWithTag(other.gameObject, Constants.TAG_ENEMY);
                enemyObject.GetComponent<Enemy>().TakeDamage((uint)Math.Abs(currentSpeed));
                break;
        }
    }

    #endregion
}
