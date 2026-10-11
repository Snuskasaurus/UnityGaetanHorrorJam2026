using UnityEngine;

enum MonsterBehaviourState
{
    None,
    Wandering,
    Chasing,
    Fleeing,

    COUNT
}

public class MonsterManager : MonoBehaviour
{
    [SerializeField] MonsterBehaviourState DefaultBehaviour = MonsterBehaviourState.Wandering;

    [Header("Wandering")]
    [SerializeField] float wanderingRange = 10f;
    [SerializeField] float radiusTargetReached = 10f;
    [SerializeField] float radiusDetectionPlayer = 10f;

    private MonsterBehaviourState CurrentMonsterBehaviourState;
    
    private MonsterBehaviourState RequestedMonsterBehaviourState;

    // Backboard
    Vector3 targetPosition;

    // References
    private PlayerController playerController;
    private MonsterController monsterController;
    private UnityEngine.AI.NavMeshAgent monsterNavMeshAgent;

    private void Start()
    {
        playerController = Common.FindTypeInScene<PlayerController>();
        monsterController = Common.FindTypeInScene<MonsterController>();
        monsterNavMeshAgent = Common.FindTypeInScene<UnityEngine.AI.NavMeshAgent>();
    }

    private void ChooseWanderingTarget()
    {
        Vector3 PlayerPosition = playerController.transform.position;
        targetPosition = NavMeshUtil.PickNewTarget_Random(PlayerPosition, wanderingRange);

        monsterNavMeshAgent.isStopped = false;
        monsterNavMeshAgent.SetDestination(targetPosition);
    }

    private void ChangeBehaviourState(MonsterBehaviourState monsterBehaviourState)
    {
        RequestedMonsterBehaviourState = monsterBehaviourState;
    }

    private void StartMonsterBehaviourState_Wandering()
    {
        ChooseWanderingTarget();
    }

    private void StartMonsterBehaviourState_Chasing()
    {
        
    }

    private void StartMonsterBehaviourState_Fleeing()
    {
        
    }

    private void DispatchMonsterBehaviourState_Start(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.None:
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
            case MonsterBehaviourState.None:
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
        float distanceToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distanceToTarget = Vector3.Distance(monsterPosition, targetPosition);
        if (distanceToTarget <= radiusTargetReached) // TODO Julien Rogel (11-10-2026): Analyse if close to target position
        {
            ChooseWanderingTarget();
        }
        if (distanceToPlayer <= radiusDetectionPlayer)
        {
            ChangeBehaviourState(MonsterBehaviourState.Chasing);
        }
    }

    private void UpdateEndMonsterBehaviourState_Chasing()
    {
        // TODO Julien Rogel (11-10-2026): Analyse if close enough to player
    }

    private void UpdateEndMonsterBehaviourState_Fleeing()
    {
        // TODO Julien Rogel (11-10-2026): Analyse if close to target position, if yes, go to BehaviourState Wandering
    }

    private void DispatchMonsterBehaviourState_Update(MonsterBehaviourState monsterBehaviourState)
    {
        switch (monsterBehaviourState)
        {
            case MonsterBehaviourState.None:
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
}

