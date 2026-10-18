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

    // Tuning values, read from the camera section of GameSettings
    private CameraSettings Settings => GameSettings.Instance.camera;

    //[SerializeField]
    //private float crouchCameraHeight = 0.5f;

    //[SerializeField]
    //private float crouchEasingDuration = 0.5f;

    // Internal variables
    private float yaw = 0f;
    private float pitch = 0f;
    //private float postureTransitionProgress = 1f;

    private PlayerController playerManager;
    private Rigidbody rigidBody;

    // Events
    //public event System.Action<OnPostureChangedEventArgs> OnPostureChanged;

    private void OnEnable()
    {
        playerManager = GetComponent<PlayerController>();
        rigidBody = GetComponent<Rigidbody>();
    }

    // Use this for initialization
    void Start()
    {
        //Disable cursor and lock it to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        yaw = transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        CameraRotation();
    }

    // The camera is oriented every frame so the mouse look stays instant
    void LateUpdate()
    {
        playerManager.PlayerCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // The body is rotated through the physics so it works with the Rigidbody interpolation
    void FixedUpdate()
    {
        rigidBody.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    private void PosturePosition()
    {
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
}