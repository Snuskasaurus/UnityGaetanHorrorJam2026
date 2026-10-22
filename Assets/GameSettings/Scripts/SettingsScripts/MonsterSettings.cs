using UnityEngine;

// Tuning of the monster behaviour, used by MonsterManager
[CreateAssetMenu(fileName = "MonsterSettings", menuName = "Game Settings/Monster Settings")]
public class MonsterSettings : ScriptableObject
{
    public MonsterBehaviourState DefaultBehaviour = MonsterBehaviourState.Wandering;

    [Header("Common")]
    public float radiusTargetReached = 3.0f;

    [Header("Movement")]
    public float MovementSpeedDefault = 10.0f;
    public float MovementSpeedChasing = 10.0f;
    public float MovementSpeedFleeing = 10.0f;
    public float MovementSpeedRunningToDoor = 10.0f;

    [Header("State | Wandering")]
    [Range(0, 100)]
    public int chanceToWaitAfterTargetReached = 100;

    [Header("State | Chasing")]
    public float radiusLoosingPlayer = 10.0f;

    [Header("State | InvestigatingSound")]
    public float minRadiusToSound = 1.0f;
    public float maxRadiusToSound = 8.0f;

    [Header("State | Waiting")]
    public float minTimeToWait = 0.5f;
    public float maxTimeToWait = 1.0f;

    [Header("State | Fleeing")]
    public float FleeingRadius = 10.0f;

    [Header("State | OffMap")]
    public float MinTimeSpentOffMap = 1.0f;
    public float MaxTimeSpentOffMap = 3.0f;

    [Header("Vision System")]
    public bool visionSystemEnabled = true;
    public float radiusVisibility = 6.0f;

    [Header("Hearing System")]
    public bool hearingSystemEnabled = true;
    public float hearingThreshold = 0.5f;
    public float soundSnapDistance = 5.0f;
    public float perfectPrecisionLoudnessRatio = 4.0f;

    [Header("Scare System")]
    public bool fearSystemEnabled = true;

    [Header("Debugs")]
    public bool showDebug = true;
    public bool showDebugRadius = true;
    public bool showDebugHearing = true;
    public float timeShowingAudioEvent = 0.5f;
}
