using UnityEngine;

// The player options (sensitivity, invert) are not here, they belong to the options menu.
[CreateAssetMenu(fileName = "CameraSettings", menuName = "Game Settings/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Look Angles")]
    [Range(0f, 90f)]
    public float maxBottomVerticalAngle = 45f;

    [Range(0f, 90f)]
    public float maxTopVerticalAngle = 85f;

    [Header("Shake")]
    public Vector3 maxShakeAngles = new Vector3(3f, 3f, 5f);  // pitch, yaw, roll in degrees at full trauma

    public float defaultShakeDuration = 0.75f;  // seconds, used by AddTrauma

    [Range(1f, 3f)]
    public float traumaExponent = 2f;  // 2 = squared, 3 = cubed

    public float shakeFrequency = 18f;  // low (2-5) = handheld camera, high (20-30) = impact

    [Header("Head Bob")]
    public float headBobVerticalAmplitude = 0.03f;  // meters

    public float headBobHorizontalAmplitude = 0.015f;  // meters

    public float headBobSmoothing = 8f;  // how fast the bob fades in and out when starting or stopping

    [Range(1f, 5f)]
    public float headBobSprintMultiplier = 1.5f;
}
