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

    [SerializeField]
    [Range(0f, 90f)]
    private float maxBottomVerticalAngle = 45f;

    [SerializeField]
    [Range(0f, 90f)]
    private float maxTopVerticalAngle = 85f;

    [SerializeField]
    private float crouchCameraHeight = 0.5f;

    [SerializeField]
    private float crouchEasingDuration = 0.5f;


    // Internal variables
    private float yaw = 0f;
    private float pitch = 0f;
    private float postureTransitionProgress = 1f;

    private PlayerController playerManager;




    // Events
    public event System.Action<OnPostureChangedEventArgs> OnPostureChanged;

    private void OnEnable()
    {
        playerManager = GetComponent<PlayerController>();
    }

    // Use this for initialization
    void Start()
    {
        //Disable cursor and lock it to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;

    }

    // Update is called once per frame
    void Update()
    {
        CameraRotation();
    }

    private void PosturePosition()
    {
    }

    private void CameraRotation()
    {
        yaw = transform.localEulerAngles.y + InputManager.PlayerLook.ReadValue<Vector2>().x * mouseSensitivity;

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
        pitch = Mathf.Clamp(pitch, -maxTopVerticalAngle, maxBottomVerticalAngle);

        transform.localEulerAngles = new Vector3(0, yaw, 0);
        playerManager.PlayerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }
}