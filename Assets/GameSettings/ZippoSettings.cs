using UnityEngine;

// Tuning of the zippo mechanics, used by ZippoController
[CreateAssetMenu(fileName = "ZippoSettings", menuName = "Game Settings/Zippo Settings")]
public class ZippoSettings : ScriptableObject
{
    [Header("Light Attempt")]
    [Range(0f, 1f)]
    public float lightAttemptSuccessChance = 0.35f;

    [Header("Flame Power")]
    [Range(0.05f, 1f)]
    public float minFlamePower = 0.2f;

    [Range(0.05f, 1f)]
    public float startFlamePower = 0.6f;

    [Range(0.05f, 1f)]
    public float fleeingPower = 0.8f;

    [Range(0.01f, 0.5f)]
    public float scrollStep = 0.1f;

    [Header("Ressource Management")]
    public float consumptionPerSecond = 1f;

    public float maxRessource = 100f;
}
