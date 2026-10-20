using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class MovementController : MonoBehaviour
{
    // Game Object references
    private Rigidbody rigidBody;
    private PlayerController playerManager;

    // Tuning values, read from the movement settings of GameSettings
    private MovementSettings Settings => GameSettings.Movement;

    // Speed the player is moving at along the ground (m/s), smoothed by the acceleration curves
    public float CurrentSpeed
    {
        get;
        private set;
    }

    // CurrentSpeed relative to the max speed of the current movement state, from 0 to 1
    public float NormalizedSpeed
    {
        get;
        private set;
    }

    private void OnEnable()
    {
        rigidBody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerController>();
        rigidBody.freezeRotation = true;
        // Gravity is applied manually so the player does not slide on slopes when standing still
        rigidBody.useGravity = false;
    }

    // Position in the cycle of two steps (left then right), from 0 to 1, a step lands at 0.25 and 0.75
    public float StepPhase
    {
        get;
        private set;
    }

    // Raised each time a foot lands on the ground
    public UnityEvent OnStep;

    // The step rhythm is updated every frame so the head bob that follows it stays smooth
    void Update()
    {
        UpdateSteps();
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

        // The wanted speed is smooth, the real one drops when the player is blocked by a wall: keep the lowest
        CurrentSpeed = Mathf.Min(newPlanar.magnitude, planar.magnitude + Settings.acceleration * Time.fixedDeltaTime);
        NormalizedSpeed = maxSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / maxSpeed) : 0f;
    }

    private float GetCurrentMaxSpeed()
    {
        switch (playerManager.CurrentMovementState)
        {
            case MovementState.Crouching:
                return Settings.crouchSpeed;
            case MovementState.Running:
                return Settings.runSpeed;
            default:
                return Settings.walkSpeed;
        }
    }

    // Handles gravity and slopes, returns the normal of the plane the player moves on
    private Vector3 ApplyGroundPhysics(ref Vector3 target, ref Vector3 velocity)
    {
        Vector3 groundNormal = playerManager.GroundNormal;
        bool onWalkableGround = playerManager.IsGrounded && playerManager.GroundAngle <= Settings.maxSlopeAngle;

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
            rigidBody.AddForce(-groundNormal * Settings.groundStickForce, ForceMode.Acceleration);
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
            ? Settings.acceleration * Settings.accelerationBySpeed.Evaluate(speedRatio)
            : Settings.deceleration * Settings.decelerationBySpeed.Evaluate(speedRatio);

        float newAlong = Mathf.MoveTowards(along, targetSpeed, rate * Time.fixedDeltaTime);
        Vector3 newLateral = Vector3.MoveTowards(lateral, Vector3.zero, Settings.lateralGrip * Time.fixedDeltaTime);

        return direction * newAlong + newLateral;
    }

    // The phase advances with the distance walked, a step is raised each time it crosses a landing point
    private void UpdateSteps()
    {
        float previousPhase = StepPhase;
        StepPhase = Mathf.Repeat(StepPhase + CurrentSpeed * Time.deltaTime / (2f * Settings.stepLength), 1f);

        bool isStepping = playerManager.IsGrounded && CurrentSpeed >= Settings.minStepSpeed;
        if (isStepping && (HasCrossed(previousPhase, StepPhase, 0.25f) || HasCrossed(previousPhase, StepPhase, 0.75f)))
        {
            OnStep.Invoke();
        }
    }

    // True when the phase went past the mark, the phase wraps from 1 back to 0
    private static bool HasCrossed(float from, float to, float mark)
    {
        if (from <= to)
        {
            return from < mark && to >= mark;
        }
        return from < mark || to >= mark;
    }
}
