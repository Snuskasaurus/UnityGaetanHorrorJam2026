using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

enum MonsterBehaviourState
{
    NoState,
    Wandering,
    InvestigatingSound,
    Chasing,
    Fleeing,

    COUNT
}

struct MonsterBackboard
{
    public Vector3 targetPosition;
    public Vector3 lastSoundPositionHeard;
    public float precisionLastSoundHeard;
}

struct SoundInfo
{
    public Vector3 Position;
    public float volume;
    public float precision;
    public float date;
    public bool heard;
}

public class MonsterManager : MonoBehaviour
{
    private static MonsterManager INSTANCE;

    [SerializeField] MonsterBehaviourState DefaultBehaviour = MonsterBehaviourState.Wandering;

    [Header("State | Wandering")]
    [SerializeField] float wanderingRange = 10.0f;
    [SerializeField] float radiusTargetReached = 1.0f;
    [SerializeField] float radiusDetectionPlayer = 6.0f;

    [Header("State | Chasing")]
    [SerializeField] float radiusLoosingPlayer = 10.0f;

    [Header("State | InvestigatingSound")]
    [SerializeField] float minRadiusToSound = 1.0f;
    [SerializeField] float maxRadiusToSound = 8.0f;

    [Header("Hearing")]
    [SerializeField] float hearingThreshold = 0.5f;
    [SerializeField] float soundSnapDistance = 5.0f;
    [SerializeField] float perfectPrecisionLoudnessRatio = 4.0f;

    [Header("Debugs")]
    [SerializeField] bool showDebug = true;
    [SerializeField] bool showDebugRadius = true;
    [SerializeField] bool showDebugHearing = true;
    [SerializeField] float timeShowingAudioEvent = 0.5f;

    private MonsterBackboard monsterBackboard;
    private MonsterBehaviourState CurrentMonsterBehaviourState;
    private MonsterBehaviourState RequestedMonsterBehaviourState;

    NavMeshPath soundPath;
    List<SoundInfo> soundInfosList = new List<SoundInfo>();
    SoundInfo LastSoundHeard;

    // References
    private PlayerController playerController;
    private MonsterController monsterController;
    private UnityEngine.AI.NavMeshAgent monsterNavMeshAgent;

    public static void SendSoundToMonsterHearing(Vector3 soundPosition, float soundVolume)
    {
        INSTANCE.SendSoundToMonsterHearing_internal(soundPosition, soundVolume);
    }

    private void SendSoundToMonsterHearing_internal(Vector3 soundPosition, float soundVolume)
    {
        if (RequestedMonsterBehaviourState == MonsterBehaviourState.Chasing)
            return;

        bool soundCanBeHeard = CheckIfSoundCanBeHeard(soundPosition, soundVolume, out float soundPrecision);

        if (showDebugHearing)
        {
            SoundInfo SoundInfo;
            {
                SoundInfo.Position = soundPosition;
                SoundInfo.volume = soundVolume;
                SoundInfo.precision = soundPrecision;
                SoundInfo.date = Time.fixedTime;
                SoundInfo.heard = soundCanBeHeard;
            }
            soundInfosList.Add(SoundInfo);
            if (soundCanBeHeard)
            {
                LastSoundHeard = SoundInfo;
            }
        }

        if (soundCanBeHeard)
        {
            monsterBackboard.lastSoundPositionHeard = soundPosition;
            monsterBackboard.precisionLastSoundHeard = soundPrecision;
            ChangeBehaviourState(MonsterBehaviourState.InvestigatingSound);
        }
    }

    private bool TryGetPathDistance(Vector3 from, Vector3 to, out float pathDistance)
    {
        pathDistance = 0.0f;

        if (!NavMesh.SamplePosition(to, out NavMeshHit hit, soundSnapDistance, NavMesh.AllAreas))
        {
            Debug.LogError("Sound emitted is not on navmesh");
            return false;
        }

        if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, soundPath) || soundPath.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("Cant find a path to the sound emitted");
            return false;
        }

        Vector3[] corners = soundPath.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            pathDistance += Vector3.Distance(corners[i], corners[i + 1]);
        }

        return true;
    }

    public static float GetHearingRadius(float soundVolume, float hearingThreshold)
    {
        if (hearingThreshold <= 0f) 
            return float.PositiveInfinity;

        return Mathf.Max(0f, soundVolume / hearingThreshold - 1f);
    }

    private bool CheckIfSoundCanBeHeard(Vector3 soundPosition, float soundVolume, out float precision)
    {
        precision = 0.0f; // Must be assigned on every return path

        Vector3 monsterPosition = monsterController.transform.position;

        float straightDistance = Vector3.Distance(monsterPosition, soundPosition);
        if (straightDistance > GetHearingRadius(soundVolume, hearingThreshold))
            return false;

        if (!TryGetPathDistance(monsterPosition, soundPosition, out float pathDistance))
            return false;

        float perceivedLoudness = soundVolume / (1.0f + pathDistance);
        if (perceivedLoudness < hearingThreshold)
            return false;

        float loudnessRatio = perceivedLoudness / Mathf.Max(hearingThreshold, 0.0001f);
        precision = Mathf.InverseLerp(1.0f, Mathf.Max(perfectPrecisionLoudnessRatio, 1.0001f), loudnessRatio);

        return true;
    }

    private void ChangeBehaviourState(MonsterBehaviourState monsterBehaviourState)
    {
        RequestedMonsterBehaviourState = monsterBehaviourState;
    }

    private void Start()
    {
        INSTANCE = this;

        playerController = Common.FindTypeInScene<PlayerController>();
        monsterController = Common.FindTypeInScene<MonsterController>();
        monsterNavMeshAgent = Common.FindTypeInScene<UnityEngine.AI.NavMeshAgent>();

        ChangeBehaviourState(DefaultBehaviour);

        Debug.Assert(radiusDetectionPlayer <= radiusLoosingPlayer);

        soundPath = new NavMeshPath();
    }

    private void UpdateTargetPosition_Wandering()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        monsterBackboard.targetPosition = NavMeshUtil.PickNewTarget_Random(PlayerPosition, wanderingRange);
        monsterNavMeshAgent.SetDestination(monsterBackboard.targetPosition);
    }

    private void UpdateTargetPosition_InvestigatingSound()
    {
        monsterBackboard.precisionLastSoundHeard = math.clamp(monsterBackboard.precisionLastSoundHeard, 0, 1);
        float radius = math.lerp(minRadiusToSound, maxRadiusToSound, monsterBackboard.precisionLastSoundHeard);
        Vector3 PlayerPosition = playerController.transform.position;
        monsterBackboard.targetPosition = NavMeshUtil.PickNewTarget_Random(PlayerPosition, radius);
        monsterNavMeshAgent.SetDestination(monsterBackboard.targetPosition);
    }

    private void UpdateTargetPosition_Chasing()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        monsterBackboard.targetPosition = PlayerPosition;
        monsterNavMeshAgent.SetDestination(monsterBackboard.targetPosition);
    }

    private void StartMonsterBehaviourState_Wandering()
    {
        UpdateTargetPosition_Wandering();
    }

    private void StartMonsterBehaviourState_InvestigatingSound()
    {
        UpdateTargetPosition_InvestigatingSound();
    }

    private void StartMonsterBehaviourState_Chasing()
    {
        UpdateTargetPosition_Chasing();
    }

    private void StartMonsterBehaviourState_Fleeing()
    {
        
    }

    private void DispatchMonsterBehaviourState_Start(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.NoState:
            {
            } break;
            case MonsterBehaviourState.Wandering:
            {
                StartMonsterBehaviourState_Wandering();
            } break;
            case MonsterBehaviourState.InvestigatingSound:
            {
                StartMonsterBehaviourState_InvestigatingSound();
            } break;
            case MonsterBehaviourState.Chasing:
            {
                StartMonsterBehaviourState_Chasing();
            } break;
            case MonsterBehaviourState.Fleeing:
            {
                StartMonsterBehaviourState_Fleeing();
            } break;
            default:
            {
                Debug.Assert((int)MonsterBehaviourState.COUNT == 5);
            } break;;
        }
    }

    private void StartChasing_IFN()
    {
        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        float distanceMonsterToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        if (distanceMonsterToPlayer <= radiusDetectionPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.Chasing);
        }
    }

    private void UpdateEndMonsterBehaviourState_Wandering()
    {
        StartChasing_IFN();

        Vector3 monsterPosition = monsterController.transform.position;
        float distanceMonsterToTarget = Vector3.Distance(monsterPosition, monsterBackboard.targetPosition);
        if (distanceMonsterToTarget <= radiusTargetReached)
        {
            UpdateTargetPosition_Wandering();
        }
    }

    private void UpdateEndMonsterBehaviourState_Chasing()
    {
        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        float distanceMonsterToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        if (distanceMonsterToPlayer >= radiusLoosingPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.Wandering);
        }
        else
        {
            UpdateTargetPosition_Chasing();
        }
    }

    private void UpdateEndMonsterBehaviourState_Fleeing()
    {
        // TODO Julien Rogel (11-10-2026): Analyse if close to target position, if yes, go to BehaviourState Wandering
    }

    private void UpdateEndMonsterBehaviourState_InvestigatingSound()
    {
        StartChasing_IFN();

        float distanceTargetToSound = Vector3.Distance(monsterBackboard.targetPosition, monsterBackboard.lastSoundPositionHeard);
        if (distanceTargetToSound <= radiusTargetReached)
        {
            UpdateTargetPosition_InvestigatingSound();
        }

        Vector3 monsterPosition = monsterController.transform.position;
        float distanceMonsterToTarget = Vector3.Distance(monsterPosition, monsterBackboard.targetPosition);
        if (distanceMonsterToTarget <= radiusTargetReached)
        {
            ChangeBehaviourState(MonsterBehaviourState.Wandering);
        }
    }

    private void DispatchMonsterBehaviourState_Update(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.NoState:
            {
            } break;
            case MonsterBehaviourState.Wandering:
            {
                UpdateEndMonsterBehaviourState_Wandering();
            } break;
            case MonsterBehaviourState.InvestigatingSound:
            {
                UpdateEndMonsterBehaviourState_InvestigatingSound();
            } break;
            case MonsterBehaviourState.Chasing:
            {
                UpdateEndMonsterBehaviourState_Chasing();
            } break;
            case MonsterBehaviourState.Fleeing:
            {
                UpdateEndMonsterBehaviourState_Fleeing();
            } break;
            default:
            {
                Debug.Assert((int)MonsterBehaviourState.COUNT == 5);
            } break;;
        }
    }

    private void Update()
    {
        DispatchMonsterBehaviourState_Update(CurrentMonsterBehaviourState);

        if (CurrentMonsterBehaviourState != RequestedMonsterBehaviourState)
        {
            // DispatchMonsterBehaviourState_Stop(CurrentMonsterBehaviourState); // Julien Rogel (11-10-2026): Not needed ?
            DispatchMonsterBehaviourState_Start(RequestedMonsterBehaviourState);
            CurrentMonsterBehaviourState = RequestedMonsterBehaviourState;
        }

        if (showDebugHearing)
        {
            float currentTime = Time.time;
            soundInfosList.RemoveAll(soundInfo => currentTime >= soundInfo.date + timeShowingAudioEvent);
        }
    }

    private static Color GetDebugStateColor(MonsterBehaviourState state)
    {
#if UNITY_EDITOR
        switch (state)
        {
            case MonsterBehaviourState.Wandering:          return Color.green;
            case MonsterBehaviourState.InvestigatingSound: return Color.orange;
            case MonsterBehaviourState.Chasing:            return Color.red;
            default:                                       return Color.gray;
        }
#endif
    }

    private void OnGUI_DebugHearing(Camera mainCamera)
    {
        DebugDraw.BeginLines();
        foreach (var soundInfo in soundInfosList)
        {
            Color colorDebug = soundInfo.heard ? Color.orange : Color.white;
            float HearingRadius = GetHearingRadius(soundInfo.volume, hearingThreshold);
            DebugDraw.DebugCircle(mainCamera, soundInfo.Position, HearingRadius, colorDebug);
        }
        if (CurrentMonsterBehaviourState == MonsterBehaviourState.InvestigatingSound)
        {
            float HearingRadius = GetHearingRadius(LastSoundHeard.volume, hearingThreshold);
            DebugDraw.DebugCircle(mainCamera, LastSoundHeard.Position, HearingRadius, Color.orange);
        }
        DebugDraw.EndLines();

        foreach (var soundInfo in soundInfosList)
        {
            Color colorDebug =  soundInfo.heard ? Color.orange : Color.white;
            Vector3 labelPosition = soundInfo.Position + Vector3.up * 2.0f;
            DebugDraw.DebugLabel(mainCamera, labelPosition, colorDebug, $"Volume {soundInfo.volume:F1}\n" +
                                                                        $"Precision {soundInfo.precision:F2}");
        }
        if (CurrentMonsterBehaviourState == MonsterBehaviourState.InvestigatingSound)
        {
            Vector3 labelPosition = LastSoundHeard.Position + Vector3.up * 2.0f;
            DebugDraw.DebugLabel(mainCamera, labelPosition, Color.orange, $"Volume {LastSoundHeard.volume:F1}\n" +
                                                                             $"Precision {LastSoundHeard.precision:F2}");
        }
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (!showDebug)
            return;

        Camera mainCamera = Camera.main;

        DebugDraw.InitializeDebugs_IFN();

        if (showDebugHearing)
        {
            OnGUI_DebugHearing(mainCamera);
        }

        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        Color stateColor = GetDebugStateColor(CurrentMonsterBehaviourState);

        DebugDraw.BeginLines();

        if (monsterNavMeshAgent.hasPath)
        {
            Vector3[] corners = monsterNavMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                DebugDraw.DebugLine(mainCamera, corners[i], corners[i + 1], stateColor);
            }
        }

        DebugDraw.DebugCross(mainCamera, monsterPosition, 0.5f, stateColor);

        if (showDebugRadius)
        {
            if (CurrentMonsterBehaviourState != MonsterBehaviourState.Chasing)
            {
                DebugDraw.DebugCircle(mainCamera, monsterPosition, radiusDetectionPlayer, Color.red);
            }
            
            switch (CurrentMonsterBehaviourState)
            {
                case MonsterBehaviourState.Wandering:
                case MonsterBehaviourState.InvestigatingSound:
                {
                    DebugDraw.DebugCircle(mainCamera, monsterBackboard.targetPosition, radiusTargetReached, stateColor);
                } break;
                case MonsterBehaviourState.Chasing:
                {
                    DebugDraw.DebugCircle(mainCamera, monsterPosition, radiusLoosingPlayer, stateColor);
                } break;
            }
        }

        DebugDraw.EndLines();

        float distToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distToTarget = Vector3.Distance(monsterPosition, monsterBackboard.targetPosition);

        DebugDraw.DebugLabel(mainCamera, monsterPosition + Vector3.up * 2.2f, stateColor, $"{CurrentMonsterBehaviourState}\n" +
                                                                                          $"Pos {monsterPosition.ToString("F1")}\n" +
                                                                                          $"Player dist {distToPlayer:F1}");

        DebugDraw.DebugLabel(mainCamera, monsterBackboard.targetPosition + Vector3.up * 0.8f, Color.magenta, $"TARGET\n{monsterBackboard.targetPosition.ToString("F1")}\n" +
                                                                                                                $"Dist {distToTarget:F1}");
#endif
    }
}

