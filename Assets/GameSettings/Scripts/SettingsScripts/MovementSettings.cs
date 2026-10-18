using UnityEngine;

// Tuning of the player movement, used by MovementController
[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game Settings/Movement Settings")]
public class MovementSettings : ScriptableObject
{
    [Header("Acceleration")]
    public float acceleration = 20f;  // m/s² at multiplier 1

    public float deceleration = 25f;

    // Acceleration and deceleration multipliers by speed ratio (current speed / max speed)
    public AnimationCurve accelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -2f),
        new Keyframe(1f, 0.15f, 0f, 0f));

    public AnimationCurve decelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 0.3f, 0f, 0f),
        new Keyframe(1f, 1f, 1.4f, 0f));

    public float lateralGrip = 40f;  // m/s², how fast sideways drift is removed when changing direction

    [Header("Movement Speeds")]
    [Range(0f, 10f)]
    public float walkSpeed = 5f;

    [Range(0f, 10f)]
    public float runSpeed = 10f;

    [Range(0f, 10f)]
    public float crouchSpeed = 2.5f;

    [Header("Slopes")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 45f;

    public float groundStickForce = 5f;
}
