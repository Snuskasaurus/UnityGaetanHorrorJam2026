using System.Collections;
using UnityEngine;

public enum Posture
{
    Standing,
    Crouching,
    Prone
}

public struct OnPostureChangedEventArgs
{
    public Posture PreviousPostureState;
    public Posture CurrentPostureState;
}
public enum MovementState
{
    Crouching,
    Walking,
    Running
}

public struct OnMovementStateChangedEventArgs
{
    public MovementState PreviousMovementState;
    public MovementState CurrentMovementState;
}

public enum AirborneState
{
    Grounded,
    Jumping,
    Falling
}

public struct OnAirborneStateChangedEventArgs
{
    public AirborneState PreviousAirborneState;
    public AirborneState CurrentAirborneState;
}

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    // Environment check settings
    [Header("Environment Checks")]
    [SerializeField]
    private LayerMask environmentMask = ~0;

    [SerializeField]
    [Range(0.01f, 1f)]
    private float groundCheckDistance = 0.1f;

    [SerializeField]
    [Range(0.01f, 2f)]
    private float ceilingCheckDistance = 0.5f;

    [SerializeField]
    [Range(0.5f, 1f)]
    private float checkRadiusFactor = 0.95f;

    [SerializeField]
    private float risingVelocityThreshold = 0.1f;

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
    public event System.Action<OnPostureChangedEventArgs> OnPostureChanged;
    public event System.Action<OnMovementStateChangedEventArgs> OnMovementStateChanged;
    public event System.Action<OnAirborneStateChangedEventArgs> OnAirborneStateChanged;

    // Use this for initialization
    void OnEnable()
    {
        PlayerCamera = GetComponentInChildren<Camera>();
        rigidBody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
    }

    private void FixedUpdate()
    {
        CheckEnvironment();
        UpdateAirborneState();
    }

    private void Start()
    {
        // Initialize posture
        CurrentPosture = Posture.Standing;
        InputManager.PlayerCrouch.performed += ctx => ChangePostureState(Posture.Crouching);
        InputManager.PlayerCrouch.canceled += ctx => ChangePostureState(Posture.Standing);
        InputManager.PlayerSprint.performed += ctx => ChangePostureState(Posture.Standing);

        // Initialize movement state
        CurrentMovementState = MovementState.Walking;
        InputManager.PlayerSprint.performed += ctx => ChangeMovementState(MovementState.Running);
        InputManager.PlayerSprint.canceled += ctx => ChangeMovementState(MovementState.Walking);
        InputManager.PlayerCrouch.performed += ctx => ChangeMovementState(MovementState.Crouching);
        InputManager.PlayerCrouch.canceled += ctx => ChangeMovementState(MovementState.Walking);
    }

    private void ChangePostureState(Posture newPosture)
    {
        if (CurrentPosture != newPosture)
        {
            // Raise the event
            OnPostureChanged?.Invoke(new OnPostureChangedEventArgs
            {
                PreviousPostureState = CurrentPosture,
                CurrentPostureState = newPosture
            });
            CurrentPosture = newPosture;
        }
    }

    private void ChangeMovementState(MovementState newMovementState)
    {
        if (CurrentMovementState != newMovementState)
        {
            OnMovementStateChanged?.Invoke(new OnMovementStateChangedEventArgs
            {
                PreviousMovementState = CurrentMovementState,
                CurrentMovementState = newMovementState
            });
            CurrentMovementState = newMovementState;
        }
    }

    private void ChangeAirborneState(AirborneState newAirborneState)
    {
        if (AirborneState != newAirborneState)
        {
            OnAirborneStateChanged?.Invoke(new OnAirborneStateChangedEventArgs
            {
                PreviousAirborneState = AirborneState,
                CurrentAirborneState = newAirborneState
            });
            AirborneState = newAirborneState;
        }
    }

    // Casts a sphere from the bottom and the top of the capsule to detect what is below and above the player
    private void CheckEnvironment()
    {
        GetCapsuleSpheres(out Vector3 bottomSphere, out Vector3 topSphere, out float radius);
        float castRadius = radius * checkRadiusFactor;
        // Start the cast slightly inside the capsule so contacts at distance 0 are not missed
        float skin = radius - castRadius;

        IsGrounded = SphereCast(bottomSphere, castRadius, Vector3.down, groundCheckDistance + skin, out RaycastHit groundHit);
        GroundHit = groundHit;

        HasCeiling = SphereCast(topSphere, castRadius, Vector3.up, ceilingCheckDistance + skin, out RaycastHit ceilingHit);
        CeilingHit = ceilingHit;
    }

    private void UpdateAirborneState()
    {
        if (IsGrounded)
        {
            ChangeAirborneState(AirborneState.Grounded);
        }
        else if (rigidBody.linearVelocity.y > risingVelocityThreshold)
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
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, castHits, distance, environmentMask, QueryTriggerInteraction.Ignore);

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
