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
}
