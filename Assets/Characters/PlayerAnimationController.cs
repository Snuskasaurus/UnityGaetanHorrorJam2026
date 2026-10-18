using UnityEngine;

// Drives the first person arms Animator from the player state and the zippo,
// and relays the zippo animation events to the ZippoController.
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    // Animator parameters
    private static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");
    private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");
    private static readonly int IsCrouchParam = Animator.StringToHash("IsCrouch");
    private static readonly int ZippoOutParam = Animator.StringToHash("ZippoOut");
    private static readonly int LightAttemptParam = Animator.StringToHash("LightAttempt");

    // References
    private Animator animator;
    private PlayerController player;
    private Rigidbody playerBody;
    private ZippoController zippo;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerController>();
        playerBody = GetComponentInParent<Rigidbody>();
        zippo = GetComponentInParent<ZippoController>();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnPostureChanged.AddListener(OnPostureChanged);
            player.OnMovementStateChanged.AddListener(OnMovementStateChanged);
        }
        if (zippo != null)
        {
            zippo.OnLightAttemptStarted.AddListener(OnLightAttemptStarted);
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnPostureChanged.RemoveListener(OnPostureChanged);
            player.OnMovementStateChanged.RemoveListener(OnMovementStateChanged);
        }
        if (zippo != null)
        {
            zippo.OnLightAttemptStarted.RemoveListener(OnLightAttemptStarted);
        }
    }

    // The events only fire on the first change, so the initial state is applied once here
    private void Start()
    {
        animator.SetBool(IsCrouchParam, player.CurrentPosture == Posture.Crouching);
        animator.SetBool(IsRunningParam, player.CurrentMovementState == MovementState.Running);
    }

    // The speed changes continuously and has no event, it is the only parameter updated every frame
    private void Update()
    {
        Vector3 velocity = playerBody.linearVelocity;
        velocity.y = 0f;

        animator.SetFloat(WalkSpeedParam, velocity.magnitude);
        animator.SetBool(ZippoOutParam, zippo != null && zippo.IsZippoOut);
    }

    private void OnPostureChanged(StateChange<Posture> change)
    {
        animator.SetBool(IsCrouchParam, change.Current == Posture.Crouching);
    }

    private void OnMovementStateChanged(StateChange<MovementState> change)
    {
        animator.SetBool(IsRunningParam, change.Current == MovementState.Running);
    }

    private void OnLightAttemptStarted()
    {
        animator.SetTrigger(LightAttemptParam);
    }

    // Animation event, applies the light attempt result when the thumb strikes the wheel
    public void ResolveLightAttempt()
    {
        if (zippo != null)
        {
            zippo.ResolveLightAttempt();
        }
    }

    // Animation event
    public void EndLightAttempt()
    {
        if (zippo != null)
        {
            zippo.EndLightAttempt();
        }
    }

    // Animation event
    public void NotifyZippoEquipped()
    {
        if (zippo != null)
        {
            zippo.NotifyZippoEquipped();
        }
    }
}
