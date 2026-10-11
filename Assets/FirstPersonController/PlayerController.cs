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

public class PlayerController : MonoBehaviour
{

    // References
    public Camera PlayerCamera
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
}
