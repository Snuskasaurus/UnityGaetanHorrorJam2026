using UnityEngine;

// Tuning of the zippo flame visual, used by ZippoFlameVisual
[CreateAssetMenu(fileName = "ZippoVisualSettings", menuName = "Game Settings/Zippo Visual Settings")]
public class ZippoVisualSettings : ScriptableObject
{
    // Light range and flame size by flame power (0 to 1), relative to their values in the prefab
    [Header("Power")]
    public AnimationCurve rangeByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    public AnimationCurve flameScaleByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    public float powerSmoothing = 10f;  // how fast the flame visually reaches a new power
}
