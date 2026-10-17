using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class MovementController : MonoBehaviour
{
    // Game Object references
    private Rigidbody rigidBody;
    private PlayerController playerManager;

    // Movement settings
    [Header("Acceleration")]
    [SerializeField]
    private float acceleration = 20f;  // m/s² at multiplier 1

    [SerializeField]
    private float deceleration = 25f;

    [SerializeField]
    private AnimationCurve accelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -2f),
        new Keyframe(1f, 0.15f, 0f, 0f));

    [SerializeField]
    private AnimationCurve decelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 0.3f, 0f, 0f),
        new Keyframe(1f, 1f, 1.4f, 0f));

    [SerializeField]
    private float lateralGrip = 40f;  // m/s², how fast sideways drift is removed when changing direction

    [Header("Movement Speeds")]
    [SerializeField]
    [Range(0f, 10f)]
    private float walkSpeed = 5f;

    [SerializeField]
    [Range(0f, 10f)]
    private float runSpeed = 10f;

    [SerializeField]
    [Range(0f, 10f)]
    private float crouchSpeed = 2.5f;

    [Header("Slopes")]
    [SerializeField]
    [Range(0f, 90f)]
    private float maxSlopeAngle = 45f;

    [SerializeField]
    private float groundStickForce = 5f;

    private void OnEnable()
    {
        rigidBody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerController>();
        rigidBody.freezeRotation = true;
        // Gravity is applied manually so the player does not slide on slopes when standing still
        rigidBody.useGravity = false;
    }

    void FixedUpdate()
    {
        Move();
    }

    // Move function that is called every physics step to move the player based on input
    private void Move()
    {
        float maxSpeed = GetCurrentMaxSpeed();

        Vector2 input = Vector2.ClampMagnitude(InputManager.PlayerMove.ReadValue<Vector2>(), 1f);
        // Use the physics rotation, not the interpolated transform
        Vector3 target = rigidBody.rotation * new Vector3(input.x, 0f, input.y) * maxSpeed;

        Vector3 velocity = rigidBody.linearVelocity;
        Vector3 planeNormal = ApplyGroundPhysics(ref target, ref velocity);

        // Only the velocity along the movement plane is controlled, the rest is left to physics
        Vector3 planar = Vector3.ProjectOnPlane(velocity, planeNormal);
        Vector3 newPlanar = ComputePlanarVelocity(planar, target, maxSpeed);
        rigidBody.AddForce(newPlanar - planar, ForceMode.VelocityChange);
    }

    private float GetCurrentMaxSpeed()
    {
        switch (playerManager.CurrentMovementState)
        {
            case MovementState.Crouching:
                return crouchSpeed;
            case MovementState.Running:
                return runSpeed;
            default:
                return walkSpeed;
        }
    }

    // Handles gravity and slopes, returns the normal of the plane the player moves on
    private Vector3 ApplyGroundPhysics(ref Vector3 target, ref Vector3 velocity)
    {
        Vector3 groundNormal = playerManager.GroundNormal;
        bool onWalkableGround = playerManager.IsGrounded && playerManager.GroundAngle <= maxSlopeAngle;

        if (onWalkableGround)
        {
            // Follow the slope surface instead of moving horizontally into it
            target = Vector3.ProjectOnPlane(target, groundNormal).normalized * target.magnitude;

            // Remove any velocity going away from the ground so we don't fly off slope crests
            float awayFromGround = Vector3.Dot(velocity, groundNormal);
            if (awayFromGround > 0f)
            {
                rigidBody.AddForce(-groundNormal * awayFromGround, ForceMode.VelocityChange);
                velocity -= groundNormal * awayFromGround;
            }
            rigidBody.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);
            return groundNormal;
        }

        if (playerManager.IsGrounded)
        {
            // Too steep: prevent the player from pushing up the slope
            Vector3 slopeOutward = new Vector3(groundNormal.x, 0f, groundNormal.z).normalized;
            float intoSlope = Vector3.Dot(target, slopeOutward);
            if (intoSlope < 0f)
            {
                target -= slopeOutward * intoSlope;
            }
        }
        rigidBody.AddForce(Physics.gravity, ForceMode.Acceleration);
        return Vector3.up;
    }

    // Eases the speed along the wanted direction with the curves, and quickly removes sideways drift
    private Vector3 ComputePlanarVelocity(Vector3 planar, Vector3 target, float maxSpeed)
    {
        float targetSpeed = target.magnitude;
        Vector3 direction;
        if (targetSpeed > 0.01f)
        {
            direction = target / targetSpeed;
        }
        else if (planar.sqrMagnitude > 0.0001f)
        {
            direction = planar.normalized;
        }
        else
        {
            return Vector3.zero;
        }

        float along = Vector3.Dot(planar, direction);
        Vector3 lateral = planar - direction * along;

        float speedRatio = maxSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(along) / maxSpeed) : 1f;
        bool accelerating = along >= 0f && targetSpeed > along;
        float rate = accelerating
            ? acceleration * accelerationBySpeed.Evaluate(speedRatio)
            : deceleration * decelerationBySpeed.Evaluate(speedRatio);

        float newAlong = Mathf.MoveTowards(along, targetSpeed, rate * Time.fixedDeltaTime);
        Vector3 newLateral = Vector3.MoveTowards(lateral, Vector3.zero, lateralGrip * Time.fixedDeltaTime);

        return direction * newAlong + newLateral;
    }
}
