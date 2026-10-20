using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CameraController : MonoBehaviour
{
    // User settings
    [SerializeField]
    private bool invertCamera = false;

    [SerializeField]
    [Range(0.1f, 5f)]
    private float mouseSensitivity = 0.5f;

    // Some players get motion sick with these, they should be options of the menu
    [SerializeField]
    private bool enableCameraShake = true;

    [SerializeField]
    private bool enableHeadBob = true;

    // Tuning values, read from the camera settings of GameSettings
    private CameraSettings Settings => GameSettings.Camera;

    //[SerializeField]
    //private float crouchCameraHeight = 0.5f;

    //[SerializeField]
    //private float crouchEasingDuration = 0.5f;

    // Internal variables
    private float yaw = 0f;
    private float pitch = 0f;
    private float trauma = 0f;
    private float headBobWeight = 0f;
    private Vector3 cameraBaseLocalPosition;
    //private float postureTransitionProgress = 1f;

    private PlayerController playerManager;
    private Rigidbody rigidBody;
    private MovementController movement;

    // Events
    //public event System.Action<OnPostureChangedEventArgs> OnPostureChanged;

    private void OnEnable()
    {
        playerManager = GetComponent<PlayerController>();
        rigidBody = GetComponent<Rigidbody>();
        movement = GetComponent<MovementController>();
    }

    // Use this for initialization
    void Start()
    {
        //Disable cursor and lock it to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        yaw = transform.eulerAngles.y;
        cameraBaseLocalPosition = playerManager.PlayerCamera.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        CameraRotation();
        trauma = Mathf.MoveTowards(trauma, 0f, Settings.traumaDecay * Time.deltaTime);
    }

    // The camera is oriented every frame so the mouse look stays instant
    // The shake and the head bob are added on top of the look, the pitch and the yaw are never modified
    void LateUpdate()
    {
        Transform cameraTransform = playerManager.PlayerCamera.transform;

        Vector3 shake = enableCameraShake ? ComputeShakeAngles() : Vector3.zero;
        cameraTransform.rotation = Quaternion.Euler(pitch + shake.x, yaw + shake.y, shake.z);

        Vector3 headBob = enableHeadBob ? ComputeHeadBobOffset() : Vector3.zero;
        cameraTransform.localPosition = cameraBaseLocalPosition + headBob;
    }

    // The body is rotated through the physics so it works with the Rigidbody interpolation
    void FixedUpdate()
    {
        rigidBody.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    private void CameraRotation()
    {
        yaw += InputManager.PlayerLook.ReadValue<Vector2>().x * mouseSensitivity;

        if (!invertCamera)
        {
            pitch -= mouseSensitivity * InputManager.PlayerLook.ReadValue<Vector2>().y;
        }
        else
        {
            // Inverted Y
            pitch += mouseSensitivity * InputManager.PlayerLook.ReadValue<Vector2>().y;
        }

        // Clamp pitch between lookAngle
        pitch = Mathf.Clamp(pitch, -Settings.maxTopVerticalAngle, Settings.maxBottomVerticalAngle);
        // Keep yaw in a small range to avoid float precision loss over time
        yaw = Mathf.Repeat(yaw, 360f);
    }

    // Adds a shake, from 0 to 1 (0.2 small hit, 0.6 big scare). One parameter so it can be wired on UnityEvents.
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    // The duration comes from the trauma decay of the settings, the magnitude is the trauma added
    public void ShakeCamera(float duration, float magnitude)
    {
        AddTrauma(magnitude);
    }

    // Smooth noise on each axis, scaled by trauma to the power of the exponent
    private Vector3 ComputeShakeAngles()
    {
        if (trauma <= 0f)
        {
            return Vector3.zero;
        }

        float shake = Mathf.Pow(trauma, Settings.traumaExponent);
        float time = Time.time * Settings.shakeFrequency;

        // A different seed per axis, otherwise pitch, yaw and roll would move together
        return new Vector3(
            (Mathf.PerlinNoise(0f, time) * 2f - 1f) * Settings.maxShakeAngles.x,
            (Mathf.PerlinNoise(10f, time) * 2f - 1f) * Settings.maxShakeAngles.y,
            (Mathf.PerlinNoise(20f, time) * 2f - 1f) * Settings.maxShakeAngles.z) * shake;
    }

    // Bobs the camera with the steps of the MovementController, the amplitude follows the speed.
    // The speed comes from the MovementController, it is smooth unlike the raw Rigidbody velocity.
    private Vector3 ComputeHeadBobOffset()
    {
        if (movement == null)
        {
            return Vector3.zero;
        }

        float targetWeight = playerManager.IsGrounded ? movement.NormalizedSpeed : 0f;
        if (playerManager.CurrentMovementState == MovementState.Running)
        {
            targetWeight *= Settings.headBobSprintMultiplier;
        }

        // Fades in and out so the camera never snaps when starting or stopping
        headBobWeight = Mathf.Lerp(headBobWeight, targetWeight, 1f - Mathf.Exp(-Settings.headBobSmoothing * Time.deltaTime));
        if (headBobWeight < 0.001f)
        {
            return Vector3.zero;
        }

        // The rhythm comes from the MovementController so the lowest point of the bob matches each footstep:
        // one side sway and two smooth vertical dips per cycle, at the landing points 0.25 and 0.75
        float angle = movement.StepPhase * Mathf.PI * 2f;
        return new Vector3(
            Mathf.Sin(angle) * Settings.headBobHorizontalAmplitude,
            -(1f - Mathf.Cos(angle * 2f)) * 0.5f * Settings.headBobVerticalAmplitude,
            0f) * headBobWeight;
    }
}