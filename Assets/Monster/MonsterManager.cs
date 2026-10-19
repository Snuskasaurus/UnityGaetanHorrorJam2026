using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

struct MonsterBlackboard
{
    public Vector3 targetPosition;
    public Vector3 lastPlayerPosition;
    public Vector3 lastSoundHeardPosition;
    public float lastSoundHeardPrecision;
    public float EndDate;
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

    // Tuning values, read from the MonsterSettings asset of GameSettings
    private MonsterSettings Settings => GameSettings.Monster;

    private MonsterBlackboard blackboard;
    private MonsterBehaviourState CurrentMonsterBehaviourState;
    private MonsterBehaviourState RequestedMonsterBehaviourState;

    private NavMeshPath soundPath;
    private readonly List<SoundInfo> debugSoundInfosList = new List<SoundInfo>();
    private SoundInfo DebugLastSoundHeard;

    // References
    private PlayerController playerController;
    private MonsterController monsterController;
    private UnityEngine.AI.NavMeshAgent monsterNavMeshAgent;

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PUBLIC FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    public static void SendSoundToMonsterHearing(Vector3 soundPosition, float soundVolume) => INSTANCE.SendSoundToMonsterHearing_internal(soundPosition, soundVolume);

    public static void TryToFearMonster() => INSTANCE.TryToFearMonster_Internal();

    /// ----------------------------------------------------------------------------------------------------------------
    ///  UNITY FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void Awake()
    {
        INSTANCE = this;
    }

    private void Start()
    {
        playerController = Common.FindTypeInScene<PlayerController>();
        monsterController = Common.FindTypeInScene<MonsterController>();
        monsterNavMeshAgent = Common.FindTypeInScene<UnityEngine.AI.NavMeshAgent>();

        ChangeBehaviourState(Settings.DefaultBehaviour);

        Debug.Assert(Settings.radiusVisibility <= Settings.radiusLoosingPlayer);

        soundPath = new NavMeshPath();

        monsterNavMeshAgent.speed = Settings.MovementSpeedDefault;
    }

    private void Update()
    {
        DispatchStateUpdate(CurrentMonsterBehaviourState);

        if (CurrentMonsterBehaviourState != RequestedMonsterBehaviourState)
        {
            // DispatchMonsterBehaviourState_Stop(CurrentMonsterBehaviourState); // Julien Rogel (11-10-2026): Not needed ?
            DispatchBehaviourStart(RequestedMonsterBehaviourState);
            CurrentMonsterBehaviourState = RequestedMonsterBehaviourState;
        }

        if (Settings.showDebugHearing)
        {
            float currentTime = Time.fixedTime;
            debugSoundInfosList.RemoveAll(soundInfo => currentTime >= soundInfo.date + Settings.timeShowingAudioEvent);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// WANDERING STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_Wandering()
    {
        UpdateWanderingTargetPosition();

        monsterNavMeshAgent.speed = Settings.MovementSpeedDefault;
    }

    private void UpdateState_Wandering()
    {
        StartChasing_IFN();

        bool hasReachedTargetPosition = HasReachedTargetPosition();
        if (hasReachedTargetPosition)
        {
            monsterNavMeshAgent.ResetPath();
            int randomNumber = Random.Range(0, 101);
            if (randomNumber >= Settings.chanceToWaitAfterTargetReached)
            {
                UpdateWanderingTargetPosition();
            }
            else
            {
                ChangeBehaviourState(MonsterBehaviourState.Waiting);
            }
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// REMEMBERING PLAYER STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_RememberingPlayer()
    {
        UpdateRememberingPlayerTargetPosition();

        monsterNavMeshAgent.speed = Settings.MovementSpeedChasing;
    }

    private void UpdateState_RememberingPlayer()
    {
        bool hasReachedTargetPosition = HasReachedTargetPosition();
        if (hasReachedTargetPosition)
        {
            monsterNavMeshAgent.ResetPath();
            ChangeBehaviourState(MonsterBehaviourState.Waiting);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// CHASING STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_Chasing()
    {
        UpdateChasingTargetPosition();

        monsterNavMeshAgent.speed = Settings.MovementSpeedChasing;
    }

    private void UpdateState_Chasing()
    {
        Vector3 playerPosition = playerController.transform.position;
        Vector3 monsterPosition = monsterController.transform.position;
        float distanceMonsterToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        if (distanceMonsterToPlayer >= Settings.radiusLoosingPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.RememberingPlayer);
        }
        else
        {
            UpdateChasingTargetPosition();
        }

        blackboard.lastPlayerPosition = playerPosition;
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// FLEEING STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_Fleeing()
    {
        UpdateFleeingTargetPosition();

        monsterNavMeshAgent.speed = Settings.MovementSpeedFleeing;
    }

    private void UpdateState_Fleeing()
    {
        if (HasReachedTargetPosition())
        {
            ChangeBehaviourState(MonsterBehaviourState.Offmap);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// WAITING STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_Waiting()
    {
        blackboard.EndDate = Common.GetCurrentDate() + Random.Range(Settings.minTimeToWait, Settings.maxTimeToWait);

        monsterNavMeshAgent.speed = Settings.MovementSpeedDefault;
    }

    private void UpdateState_Waiting()
    {
        StartChasing_IFN();

        bool HasFinishedWaiting = Common.HasReachedDate(blackboard.EndDate);
        if (HasFinishedWaiting)
        {
            ChangeBehaviourState(MonsterBehaviourState.Wandering);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// INVESTIGATING SOUND STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_InvestigatingSound()
    {
        UpdateInvestigatingSoundTargetPosition();

        monsterNavMeshAgent.speed = Settings.MovementSpeedDefault;
    }

    private void UpdateState_InvestigatingSound()
    {
        StartChasing_IFN();

        Vector3 monsterPosition = monsterController.transform.position;
        float distanceMonsterToTarget = Vector3.Distance(monsterPosition, blackboard.targetPosition);
        if (distanceMonsterToTarget <= Settings.radiusTargetReached)
        {
            ChangeBehaviourState(MonsterBehaviourState.Wandering);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// OFFMAP STATE
    /// ----------------------------------------------------------------------------------------------------------------

    private void StartState_OffMap()
    {
        blackboard.EndDate = Common.GetCurrentDate() + Random.Range(Settings.MinTimeSpentOffMap, Settings.MaxTimeSpentOffMap);

        monsterNavMeshAgent.speed = Settings.MovementSpeedDefault;
    }

    private void UpdateState_OffMap()
    {
        bool HasFinishedWaiting = Common.HasReachedDate(blackboard.EndDate);
        if (HasFinishedWaiting)
        {
            ChangeBehaviourState(MonsterBehaviourState.Wandering);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    /// DISPATCH STATES
    /// ----------------------------------------------------------------------------------------------------------------

    private void DispatchBehaviourStart(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.NoState:
            {
            } break;
            case MonsterBehaviourState.Offmap:
            {
                StartState_OffMap();
            } break;
            case MonsterBehaviourState.Wandering:
            {
                StartState_Wandering();
            } break;
            case MonsterBehaviourState.InvestigatingSound:
            {
                StartState_InvestigatingSound();
            } break;
            case MonsterBehaviourState.Waiting:
            {
                StartState_Waiting();
            } break;
            case MonsterBehaviourState.RememberingPlayer:
            {
                StartState_RememberingPlayer();
            } break;
            case MonsterBehaviourState.Chasing:
            {
                StartState_Chasing();
            } break;
            case MonsterBehaviourState.Fleeing:
            {
                StartState_Fleeing();
            } break;
            default:
            {
                Debug.Assert((int)MonsterBehaviourState.COUNT == 8);
            } break;;
        }
    }

    private void DispatchStateUpdate(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.NoState:
            {
            } break;
            case MonsterBehaviourState.Offmap:
            {
                UpdateState_OffMap();
            } break;
            case MonsterBehaviourState.Wandering:
            {
                UpdateState_Wandering();
            } break;
            case MonsterBehaviourState.InvestigatingSound:
            {
                UpdateState_InvestigatingSound();
            } break;
            case MonsterBehaviourState.Chasing:
            {
                UpdateState_Chasing();
            } break;
            case MonsterBehaviourState.Waiting:
            {
                UpdateState_Waiting();
            } break;
            case MonsterBehaviourState.RememberingPlayer:
            {
                UpdateState_RememberingPlayer();
            } break;
            case MonsterBehaviourState.Fleeing:
            {
                UpdateState_Fleeing();
            } break;
            default:
            {
                Debug.Assert((int)MonsterBehaviourState.COUNT == 8);
            } break;;
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  FREE FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void UpdateTargetToSound_IFN(Vector3 newSoundPosition, float newSoundPrecision)
    {
        if (newSoundPrecision >= blackboard.lastSoundHeardPrecision)
        {
            blackboard.lastSoundHeardPosition = newSoundPosition;
            blackboard.lastSoundHeardPrecision = newSoundPrecision;
            UpdateInvestigatingSoundTargetPosition();
        }
    }

    private void TryToFearMonster_Internal()
    {
        if (!Settings.fearSystemEnabled)
            return;

        Vector3 playerPosition = playerController.transform.position;
        bool monsterIsCloseEnoughToPlayer = Vector3.Distance(playerPosition, blackboard.targetPosition) <= Settings.radiusTargetReached; 
        if (monsterIsCloseEnoughToPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.Fleeing);
        }
    }

    private void SendSoundToMonsterHearing_internal(Vector3 soundPosition, float soundVolume)
    {
        if (Settings.hearingSystemEnabled == false)
            return;

        if (RequestedMonsterBehaviourState == MonsterBehaviourState.Chasing)
            return;

        if (RequestedMonsterBehaviourState == MonsterBehaviourState.Fleeing)
            return;

        bool soundCanBeHeard = CheckIfSoundCanBeHeard(soundPosition, soundVolume, out float soundPrecision);

        if (Settings.showDebugHearing)
        {
            SoundInfo SoundInfo;
            SoundInfo.Position = soundPosition;
            SoundInfo.volume = soundVolume;
            SoundInfo.precision = soundPrecision;
            SoundInfo.date = Time.fixedTime;
            SoundInfo.heard = soundCanBeHeard;
            debugSoundInfosList.Add(SoundInfo);
            if (soundCanBeHeard)
            {
                DebugLastSoundHeard = SoundInfo;
            }
        }

        if (soundCanBeHeard)
        {
            if (RequestedMonsterBehaviourState == MonsterBehaviourState.InvestigatingSound)
            {
                UpdateTargetToSound_IFN(soundPosition, soundPrecision);
            }
            else
            {
                blackboard.lastSoundHeardPosition = soundPosition;
                blackboard.lastSoundHeardPrecision = soundPrecision;
                ChangeBehaviourState(MonsterBehaviourState.InvestigatingSound);
            }
        }
    }

    private bool TryGetPathDistance(Vector3 from, Vector3 to, out float pathDistance)
    {
        pathDistance = 0.0f;

        if (!NavMesh.SamplePosition(to, out NavMeshHit hit, Settings.soundSnapDistance, NavMesh.AllAreas))
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

    private static float GetHearingRadius(float soundVolume, float hearingThreshold)
    {
        if (hearingThreshold <= 0f) 
            return float.PositiveInfinity;

        return Mathf.Max(0f, soundVolume / hearingThreshold - 1f);
    }

    private bool CheckIfSoundCanBeHeard(Vector3 soundPosition, float soundVolume, out float precision)
    {
        precision = 0.0f;

        Vector3 monsterPosition = monsterController.transform.position;

        float straightDistance = Vector3.Distance(monsterPosition, soundPosition);
        if (straightDistance > GetHearingRadius(soundVolume, Settings.hearingThreshold))
            return false;

        if (!TryGetPathDistance(monsterPosition, soundPosition, out float pathDistance))
            return false;

        float perceivedLoudness = soundVolume / (1.0f + pathDistance);
        if (perceivedLoudness < Settings.hearingThreshold)
            return false;

        float loudnessRatio = perceivedLoudness / Mathf.Max(Settings.hearingThreshold, 0.0001f);
        precision = Mathf.InverseLerp(1.0f, Mathf.Max(Settings.perfectPrecisionLoudnessRatio, 1.0001f), loudnessRatio);

        return true;
    }

    private void ChangeBehaviourState(MonsterBehaviourState monsterBehaviourState)
    {
        RequestedMonsterBehaviourState = monsterBehaviourState;
    }

    private void StartChasing_IFN()
    {
        if (!Settings.visionSystemEnabled)
            return;

        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        float distanceMonsterToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        if (distanceMonsterToPlayer <= Settings.radiusVisibility)
        {
            ChangeBehaviourState(MonsterBehaviourState.Chasing);
        }
    }

    private bool HasReachedTargetPosition()
    {
        Vector3 monsterPosition = monsterController.transform.position;
        float distanceMonsterToTarget = Vector3.Distance(monsterPosition, blackboard.targetPosition);

        return distanceMonsterToTarget <= Settings.radiusTargetReached;
    }

    private void UpdateWanderingTargetPosition()
    {
        Vector3 targetPosition = Vector3.zero;
        Room room = null;
        if (RoomManager.TryGetRandomRoom(ref room))
        {
            targetPosition = room.GetRandomPositionInsideRoom();
        }
        if (!NavMeshUtil.GetClosestPointInNavMesh_Small(targetPosition, out blackboard.targetPosition))
        {
            Debug.LogError("Failed ot find new target for monster");
        }
        monsterNavMeshAgent.SetDestination(blackboard.targetPosition);
    }

    private void UpdateFleeingTargetPosition()
    {
        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 targetPosition = Vector3.zero;
        EscapeZone escapeZone = null;
        if (EscapeZoneManager.GetRandomEscapeZone(ref escapeZone, monsterPosition, Settings.FleeingRadius))
        {
            targetPosition = escapeZone.transform.position;
        }
        if (!NavMeshUtil.GetClosestPointInNavMesh_Small(targetPosition, out blackboard.targetPosition))
        {
            Debug.LogError("Failed ot find new target for monster");
        }
        monsterNavMeshAgent.SetDestination(blackboard.targetPosition);
    }

    private void UpdateInvestigatingSoundTargetPosition()
    {
        blackboard.lastSoundHeardPrecision = math.clamp(blackboard.lastSoundHeardPrecision, 0, 1);
        float radius = math.lerp(Settings.maxRadiusToSound, Settings.minRadiusToSound, blackboard.lastSoundHeardPrecision);
        Vector3 soundPosition = blackboard.lastSoundHeardPosition;
        if (!NavMeshUtil.PickNewTarget_RandomInCircle(soundPosition, radius, out blackboard.targetPosition))
        {
            Debug.LogError("Failed ot find new target for monster");
        }
        monsterNavMeshAgent.SetDestination(blackboard.targetPosition);
    }

    private void UpdateChasingTargetPosition()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        blackboard.targetPosition = PlayerPosition;
        monsterNavMeshAgent.SetDestination(blackboard.targetPosition);
    }

    private void UpdateRememberingPlayerTargetPosition()
    {
        blackboard.targetPosition = blackboard.lastPlayerPosition;
        monsterNavMeshAgent.SetDestination(blackboard.targetPosition);
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  DEBUGS
    /// ----------------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
    private static Color GetDebugStateColor(MonsterBehaviourState state)
    {
        switch (state)
        {
            case MonsterBehaviourState.Offmap:             return Color.gray;
            case MonsterBehaviourState.Wandering:          return Color.green;
            case MonsterBehaviourState.Waiting:            return Color.white;
            case MonsterBehaviourState.InvestigatingSound: return Color.yellow;
            case MonsterBehaviourState.RememberingPlayer:  return Color.orangeRed;
            case MonsterBehaviourState.Chasing:            return Color.red;
            case MonsterBehaviourState.Fleeing:            return Color.purple;
            default:                                       return Color.gray;
        }
    }
#endif

#if UNITY_EDITOR
    private void OnGUI_DebugHearing(Camera mainCamera)
    {
        DebugDraw.BeginLines();
        foreach (var soundInfo in debugSoundInfosList)
        {
            Color colorDebug = soundInfo.heard ? Color.orange : Color.white;
            float HearingRadius = GetHearingRadius(soundInfo.volume, Settings.hearingThreshold);
            DebugDraw.DebugCircle(mainCamera, soundInfo.Position, HearingRadius, colorDebug);
        }
        if (CurrentMonsterBehaviourState == MonsterBehaviourState.InvestigatingSound)
        {
            float HearingRadius = GetHearingRadius(DebugLastSoundHeard.volume, Settings.hearingThreshold);
            DebugDraw.DebugCircle(mainCamera, DebugLastSoundHeard.Position, HearingRadius, Color.orange);
        }
        DebugDraw.EndLines();

        foreach (var soundInfo in debugSoundInfosList)
        {
            Color colorDebug =  soundInfo.heard ? Color.orange : Color.white;
            Vector3 labelPosition = soundInfo.Position + Vector3.up * 0.25f;
            DebugDraw.DebugLabel(mainCamera, labelPosition, colorDebug, $"Volume {soundInfo.volume:F1}\n" +
                                                                        $"Precision {soundInfo.precision:F2}");
        }
        if (CurrentMonsterBehaviourState == MonsterBehaviourState.InvestigatingSound)
        {
            Vector3 labelPosition = DebugLastSoundHeard.Position + Vector3.up * 0.25f;
            DebugDraw.DebugLabel(mainCamera, labelPosition, Color.orange, $"Volume {DebugLastSoundHeard.volume:F1}\n" +
                                                                             $"Precision {DebugLastSoundHeard.precision:F2}");
        }
    }
#endif

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (!Settings.showDebug)
            return;

        Camera mainCamera = Camera.main;

        DebugDraw.InitializeDebugs_IFN();

        if (Settings.showDebugHearing)
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

        if (Settings.showDebugRadius)
        {
            if (CurrentMonsterBehaviourState != MonsterBehaviourState.Chasing ||
                CurrentMonsterBehaviourState != MonsterBehaviourState.Fleeing ||
                CurrentMonsterBehaviourState != MonsterBehaviourState.Offmap)
            {
                DebugDraw.DebugCircle(mainCamera, monsterPosition, Settings.radiusVisibility, Color.red);
            }

            DebugDraw.DebugCircle(mainCamera, blackboard.targetPosition, Settings.radiusTargetReached, stateColor);

            if (CurrentMonsterBehaviourState == MonsterBehaviourState.Chasing)
            {
                DebugDraw.DebugCircle(mainCamera, monsterPosition, Settings.radiusLoosingPlayer, stateColor);
            }
        }

        DebugDraw.EndLines();

        float distToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distToTarget = Vector3.Distance(monsterPosition, blackboard.targetPosition);

        DebugDraw.DebugLabel(mainCamera, monsterPosition + Vector3.up * 2.2f, stateColor, $"{CurrentMonsterBehaviourState}\n" +
                                                                                          $"Pos {monsterPosition.ToString("F1")}\n" +
                                                                                          $"Player dist {distToPlayer:F1}");

        DebugDraw.DebugLabel(mainCamera, blackboard.targetPosition + Vector3.up * 0.8f, Color.magenta, $"TARGET\n{blackboard.targetPosition.ToString("F1")}\n" + 
                                                                                                          $"Dist {distToTarget:F1}");
#endif
    }
}