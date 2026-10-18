using UnityEngine;

// Tuning of the first person arms pulled back near walls, used by ArmWallRetraction
[CreateAssetMenu(fileName = "ArmSettings", menuName = "Game Settings/Arm Settings")]
public class ArmSettings : ScriptableObject
{
    [Header("Detection")]
    public LayerMask wallMask = ~0;

    [Range(0.01f, 0.3f)]
    public float probeRadius = 0.08f;

    [Header("Retraction")]
    [Range(0f, 1f)]
    public float maxRetract = 0.35f;

    [Range(0f, 0.5f)]
    public float maxLower = 0.1f;

    public Vector3 maxTuckAngles = new Vector3(15f, -10f, 0f);

    public float retractSharpness = 20f;

    public float extendSharpness = 8f;
}
