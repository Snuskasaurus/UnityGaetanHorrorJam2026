using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Previous and current value of a state, sent by the state changed events
public struct StateChange<T>
{
    public T Previous;
    public T Current;
}

public enum Posture
{
    Standing,
    Crouching,
    Prone
}

public enum MovementState
{
    Crouching,
    Walking,
    Running
}

public enum AirborneState
{
    Grounded,
    Jumping,
    Falling
}

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    // Tuning values, read from the playerChecks settings of GameSettings
    private PlayerChecksSettings Settings => GameSettings.PlayerChecks;

    // References
    public Camera PlayerCamera
    {
        get;
        private set;
    }

    private Rigidbody rigidBody;
    private CapsuleCollider capsule;
    private readonly RaycastHit[] castHits = new RaycastHit[8];

    // Environment state
    public bool IsGrounded
    {
        get;
        private set;
    }

    public RaycastHit GroundHit
    {
        get;
        private set;
    }

    public Vector3 GroundNormal => IsGrounded ? GroundHit.normal : Vector3.up;

    public float GroundAngle => IsGrounded ? Vector3.Angle(GroundHit.normal, Vector3.up) : 0f;

    public bool HasCeiling
    {
        get;
        private set;
    }

    public RaycastHit CeilingHit
    {
        get;
        private set;
    }

    // State variables
    public Posture CurrentPosture
    {
        get;
        private set;
    }
    public AirborneState AirborneState
    { 
        get; 
        private set; 
    }

    public MovementState CurrentMovementState
    {
        get;
        private set;
    }

    // Events
    [Header("Events")]
    public UnityEvent<StateChange<Posture>> OnPostureChanged;
    public UnityEvent<StateChange<MovementState>> OnMovementStateChanged;
    public UnityEvent<StateChange<AirborneState>> OnAirborneStateChanged;

    // When crouch and sprint are both held, the last one pressed wins
    private bool isSprintPressedLast = false;

    // Use this for initialization
    void OnEnable()
    {
        PlayerCamera = GetComponentInChildren<Camera>();
        rigidBody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        InputManager.PlayerCrouch.performed += OnCrouchPerformed;
        InputManager.PlayerCrouch.canceled += OnStanceInputCanceled;
        InputManager.PlayerSprint.performed += OnSprintPerformed;
        InputManager.PlayerSprint.canceled += OnStanceInputCanceled;
    }

    private void OnDisable()
    {
        InputManager.PlayerCrouch.performed -= OnCrouchPerformed;
        InputManager.PlayerCrouch.canceled -= OnStanceInputCanceled;
        InputManager.PlayerSprint.performed -= OnSprintPerformed;
        InputManager.PlayerSprint.canceled -= OnStanceInputCanceled;
    }

    private void FixedUpdate()
    {
        CheckEnvironment();
        UpdateAirborneState();
    }

    private void Start()
    {
        CurrentPosture = Posture.Standing;
        CurrentMovementState = MovementState.Walking;
    }

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        isSprintPressedLast = false;
        RefreshStances();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprintPressedLast = true;
        RefreshStances();
    }

    private void OnStanceInputCanceled(InputAction.CallbackContext context)
    {
        RefreshStances();
    }

    // Deduces the posture and the movement state from the crouch and sprint keys currently held
    private void RefreshStances()
    {
        bool isCrouchHeld = InputManager.PlayerCrouch.IsPressed();
        bool isSprintHeld = InputManager.PlayerSprint.IsPressed();

        if (isSprintHeld && (!isCrouchHeld || isSprintPressedLast))
        {
            ChangePostureState(Posture.Standing);
            ChangeMovementState(MovementState.Running);
        }
        else if (isCrouchHeld)
        {
            ChangePostureState(Posture.Crouching);
            ChangeMovementState(MovementState.Crouching);
        }
        else
        {
            ChangePostureState(Posture.Standing);
            ChangeMovementState(MovementState.Walking);
        }
    }

    private void ChangePostureState(Posture newPosture)
    {
        if (CurrentPosture != newPosture)
        {
            // The state is updated before raising the event, so listeners can read the new value
            Posture previousPosture = CurrentPosture;
            CurrentPosture = newPosture;
            OnPostureChanged?.Invoke(new StateChange<Posture>
            {
                Previous = previousPosture,
                Current = newPosture
            });
        }
    }

    private void ChangeMovementState(MovementState newMovementState)
    {
        if (CurrentMovementState != newMovementState)
        {
            // The state is updated before raising the event, so listeners can read the new value
            MovementState previousMovementState = CurrentMovementState;
            CurrentMovementState = newMovementState;
            OnMovementStateChanged?.Invoke(new StateChange<MovementState>
            {
                Previous = previousMovementState,
                Current = newMovementState
            });
        }
    }

    private void ChangeAirborneState(AirborneState newAirborneState)
    {
        if (AirborneState != newAirborneState)
        {
            // The state is updated before raising the event, so listeners can read the new value
            AirborneState previousAirborneState = AirborneState;
            AirborneState = newAirborneState;
            OnAirborneStateChanged?.Invoke(new StateChange<AirborneState>
            {
                Previous = previousAirborneState,
                Current = newAirborneState
            });
        }
    }

    // Casts a sphere from the bottom and the top of the capsule to detect what is below and above the player
    private void CheckEnvironment()
    {
        GetCapsuleSpheres(out Vector3 bottomSphere, out Vector3 topSphere, out float radius);
        float castRadius = radius * Settings.checkRadiusFactor;
        // Start the cast slightly inside the capsule so contacts at distance 0 are not missed
        float skin = radius - castRadius;

        IsGrounded = SphereCast(bottomSphere, castRadius, Vector3.down, Settings.groundCheckDistance + skin, out RaycastHit groundHit);
        GroundHit = groundHit;

        HasCeiling = SphereCast(topSphere, castRadius, Vector3.up, Settings.ceilingCheckDistance + skin, out RaycastHit ceilingHit);
        CeilingHit = ceilingHit;
    }

    private void UpdateAirborneState()
    {
        if (IsGrounded)
        {
            ChangeAirborneState(AirborneState.Grounded);
        }
        else if (rigidBody.linearVelocity.y > Settings.risingVelocityThreshold)
        {
            ChangeAirborneState(AirborneState.Jumping);
        }
        else
        {
            ChangeAirborneState(AirborneState.Falling);
        }
    }

    // Returns the closest hit that does not belong to the player itself
    private bool SphereCast(Vector3 origin, float radius, Vector3 direction, float distance, out RaycastHit closestHit)
    {
        closestHit = default;
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, castHits, distance, Settings.environmentMask, QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = castHits[i];
            // Ignore our own colliders and initial overlaps (distance 0 with no valid point)
            if (hit.collider.transform.IsChildOf(transform) || (hit.distance <= 0f && hit.point == Vector3.zero))
            {
                continue;
            }
            if (!found || hit.distance < closestHit.distance)
            {
                closestHit = hit;
                found = true;
            }
        }
        return found;
    }

    // Computes the world positions of the bottom and top sphere centers of the capsule
    private void GetCapsuleSpheres(out Vector3 bottomSphere, out Vector3 topSphere, out float radius)
    {
        Vector3 scale = transform.lossyScale;
        radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);

        Vector3 center = transform.TransformPoint(capsule.center);
        Vector3 offset = transform.up * (halfHeight - radius);
        bottomSphere = center - offset;
        topSphere = center + offset;
    }
}
