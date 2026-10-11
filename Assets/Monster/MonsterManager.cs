using System;
using UnityEngine;
using UnityEngine.Rendering;

enum MonsterBehaviourState
{
    NoState,
    Wandering,
    Chasing,
    Fleeing,

    COUNT
}

public class MonsterManager : MonoBehaviour
{
    [SerializeField] MonsterBehaviourState DefaultBehaviour = MonsterBehaviourState.Wandering;

    [Header("Wandering")]
    [SerializeField] float wanderingRange = 10.0f;
    [SerializeField] float radiusTargetReached = 1.0f;
    [SerializeField] float radiusDetectionPlayer = 6.0f;
    
    [Header("Chasing")]
    [SerializeField] float radiusLoosingPlayer = 10.0f;
    
    [Header("Debug")]
    [SerializeField] bool showDebug = true;
    [SerializeField] bool showDebugRadius = true;

    private MonsterBehaviourState CurrentMonsterBehaviourState;
    
    private MonsterBehaviourState RequestedMonsterBehaviourState;

    // Backboard
    Vector3 targetPosition;

    // References
    private PlayerController playerController;
    private MonsterController monsterController;
    private UnityEngine.AI.NavMeshAgent monsterNavMeshAgent;
    
    private void ChangeBehaviourState(MonsterBehaviourState monsterBehaviourState)
    {
        RequestedMonsterBehaviourState = monsterBehaviourState;
    }

    private void Start()
    {
        playerController = Common.FindTypeInScene<PlayerController>();
        monsterController = Common.FindTypeInScene<MonsterController>();
        monsterNavMeshAgent = Common.FindTypeInScene<UnityEngine.AI.NavMeshAgent>();

        ChangeBehaviourState(DefaultBehaviour);

        Debug.Assert(radiusDetectionPlayer <= radiusLoosingPlayer);
    }

    private void UpdateTargetPosition_Wandering()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        targetPosition = NavMeshUtil.PickNewTarget_Random(PlayerPosition, wanderingRange);
        monsterNavMeshAgent.SetDestination(targetPosition);
    }

    private void UpdateTargetPosition_Chasing()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        targetPosition = PlayerPosition;
        monsterNavMeshAgent.SetDestination(targetPosition);
    }

    private void StartMonsterBehaviourState_Wandering()
    {
        UpdateTargetPosition_Wandering();
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
                Debug.Assert((int)MonsterBehaviourState.COUNT == 4);
            } break;;
        }
    }

    private void StopMonsterBehaviourState_Wandering()
    {
        // TODO Julien Rogel (11-10-2026): Find a new random position
    }

    private void StopMonsterBehaviourState_Chasing()
    {
        
    }

    private void StopMonsterBehaviourState_Fleeing()
    {
        
    }

    private void DispatchMonsterBehaviourState_Stop(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.NoState:
            {
            } break;
            case MonsterBehaviourState.Wandering:
            {
                StopMonsterBehaviourState_Wandering();
            } break;
            case MonsterBehaviourState.Chasing:
            {
                StopMonsterBehaviourState_Chasing();
            } break;
            case MonsterBehaviourState.Fleeing:
            {
                StopMonsterBehaviourState_Fleeing();
            } break;
            default:
            {
                Debug.Assert((int)MonsterBehaviourState.COUNT == 4);
            } break;;
        }
    }

    private void UpdateEndMonsterBehaviourState_Wandering()
    {
        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        float distanceMonsterToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distanceMonsterToTarget = Vector3.Distance(monsterPosition, targetPosition);
        if (distanceMonsterToTarget <= radiusTargetReached)
        {
            UpdateTargetPosition_Wandering();
        }
        if (distanceMonsterToPlayer <= radiusDetectionPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.Chasing);
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
                Debug.Assert((int)MonsterBehaviourState.COUNT == 4);
            } break;;
        }
    }

    private void Update()
    {
        DispatchMonsterBehaviourState_Update(CurrentMonsterBehaviourState);

        if (CurrentMonsterBehaviourState != RequestedMonsterBehaviourState)
        {
            DispatchMonsterBehaviourState_Stop(RequestedMonsterBehaviourState);
            DispatchMonsterBehaviourState_Start(RequestedMonsterBehaviourState);
            CurrentMonsterBehaviourState = RequestedMonsterBehaviourState;
        }
    }

    private static Color GetDebugStateColor(MonsterBehaviourState state)
    {
#if UNITY_EDITOR
        switch (state)
        {
            case MonsterBehaviourState.Wandering: return Color.green;
            case MonsterBehaviourState.Chasing:   return Color.red;
            default:                              return Color.gray;
        }
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (!showDebug)
            return;
        
        Camera mainCamera = Camera.main;

        DebugDraw.InitializeDebugs_IFN();

        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        Color stateColor = GetDebugStateColor(CurrentMonsterBehaviourState);

        DebugDraw.BeginLines();

        if (monsterNavMeshAgent.hasPath)
        {
            Vector3[] corners = monsterNavMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                DebugDraw.DebugLine(mainCamera, corners[i], corners[i + 1], Color.cyan);
            }
        }

        DebugDraw.DebugCross(mainCamera, monsterPosition, 0.5f, stateColor);

        if (showDebugRadius)
        {
            switch (CurrentMonsterBehaviourState)
            {
                case MonsterBehaviourState.Wandering:
                {
                    DebugDraw.DebugCircle(mainCamera, monsterPosition, radiusDetectionPlayer, Color.yellow);
                    DebugDraw.DebugCircle(mainCamera, targetPosition, radiusTargetReached, Color.cyan);
                } break;
                case MonsterBehaviourState.Chasing:
                {
                    DebugDraw.DebugCircle(mainCamera, monsterPosition, radiusLoosingPlayer, Color.red);
                } break;
            }
        }

        DebugDraw.EndLines();

        float distToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distToTarget = Vector3.Distance(monsterPosition, targetPosition);

        DebugDraw.DebugLabel(mainCamera, monsterPosition + Vector3.up * 2.2f, $"{CurrentMonsterBehaviourState}\nPos {monsterPosition.ToString("F1")}\nPlayer dist {distToPlayer:F1}", stateColor);

        DebugDraw.DebugLabel(mainCamera, targetPosition + Vector3.up * 0.8f, $"TARGET\n{targetPosition.ToString("F1")}\nDist {distToTarget:F1}", Color.magenta);
#endif
    }
}

