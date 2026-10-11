using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class MovementController : MonoBehaviour
{
    // Game Object references
    private Rigidbody rigidBody;
    private PlayerController playerManager;

    // Movement settings
    [SerializeField]
    private float moveEaseingDuration = 0.5f;

    [SerializeField]
    private float maxVelocityChange = 10f;

    [SerializeField]
    [Range(0f, 10f)]
    private float walkSpeed = 5f;

    [SerializeField]
    [Range(0f, 10f)]
    private float runSpeed = 10f;

    [SerializeField]
    [Range(0f, 10f)]
    private float crouchSpeed = 2.5f;

    // Events
    //public event System.Action<OnMovementStateChangedEventArgs> OnMovementStateChanged;
    //public event System.Action<AirborneState> OnAirborneStateChanged;

    // Prrivate variables
    private float moveProgress = 0f;
    private Vector3 lastInputVelocity = Vector3.zero;

    private void OnEnable()
    {
        rigidBody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerController>();
        rigidBody.freezeRotation = true;
        rigidBody.useGravity = true;
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Move();
    }

    // Move function that is called every frame to move the player based on input
    private void Move()
    {
        // Calculate how fast we should be moving
        Vector3 inputVelocity = new Vector3(InputManager.PlayerMove.ReadValue<Vector2>().x, 0, InputManager.PlayerMove.ReadValue<Vector2>().y);
        float speedFactor = 1f;
        // Checks if player is walking and isGrounded
        if (inputVelocity.x != 0 || inputVelocity.z != 0)
        {
            lastInputVelocity = inputVelocity;
            moveProgress = Mathf.Clamp01(moveProgress + Time.deltaTime / moveEaseingDuration);
            speedFactor = Easing.OutCubic(moveProgress);
        }
        else
        {
            inputVelocity = lastInputVelocity;
            moveProgress = Mathf.Clamp01(moveProgress - Time.deltaTime / moveEaseingDuration);
            speedFactor = Easing.InCubic(moveProgress);
        }
        inputVelocity = transform.TransformDirection(inputVelocity) * speedFactor;
        switch (playerManager.CurrentMovementState)
        {
            case MovementState.Crouching:
                inputVelocity *= crouchSpeed;
                break;
            case MovementState.Walking:
                inputVelocity *= walkSpeed;
                break;
            case MovementState.Running:
                inputVelocity *= runSpeed;
                break;
        }
        // Apply a force that attempts to reach our target velocity
        Vector3 velocity = rigidBody.linearVelocity;
        Vector3 velocityChange = (inputVelocity - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;

        rigidBody.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void AirborneStatusVerifier()
    {

    }
}