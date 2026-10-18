using UnityEngine;

// Tuning of the ground and ceiling detection, used by PlayerController
[CreateAssetMenu(fileName = "PlayerChecksSettings", menuName = "Game Settings/Player Checks Settings")]
public class PlayerChecksSettings : ScriptableObject
{
    [Header("Environment Checks")]
    public LayerMask environmentMask = ~0;

    [Range(0.01f, 1f)]
    public float groundCheckDistance = 0.1f;

    [Range(0.01f, 2f)]
    public float ceilingCheckDistance = 0.5f;

    [Range(0.5f, 1f)]
    public float checkRadiusFactor = 0.95f;

    public float risingVelocityThreshold = 0.1f;
}

// Tuning of the first person camera, used by CameraController.
