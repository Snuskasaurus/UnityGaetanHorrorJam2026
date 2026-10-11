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

#if UNITY_EDITOR

    private Material debugLineMaterial;
    private GUIStyle debugLabelStyle;

    private void OnGUI()
    {
        if (!showDebug)
            return;

        Camera camera = Camera.main;

        Vector3 monsterPosition = monsterController.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        Color stateColor = GetDebugStateColor(CurrentMonsterBehaviourState);

        EnsureDebugResources();

        debugLineMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        if (monsterNavMeshAgent.hasPath)
        {
            Vector3[] corners = monsterNavMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                DebugLine(camera, corners[i], corners[i + 1], Color.cyan);
            }
        }

        DebugCross(camera, monsterPosition, 0.5f, stateColor);

        if (CurrentMonsterBehaviourState == MonsterBehaviourState.Wandering)
        {
            DebugCross(camera, targetPosition, 0.5f, Color.cyan);
        }

        if (showDebugRadius)
        {
            switch (CurrentMonsterBehaviourState)
            {
                case MonsterBehaviourState.Wandering:
                {
                    DebugCircle(camera, monsterPosition, radiusDetectionPlayer, Color.yellow);
                    DebugCircle(camera, targetPosition, radiusTargetReached, Color.cyan);
                } break;
                case MonsterBehaviourState.Chasing:
                {
                    DebugCircle(camera, monsterPosition, radiusLoosingPlayer, Color.red);
                } break;
            }
        }

        GL.End();

        float distToPlayer = Vector3.Distance(monsterPosition, playerPosition);
        float distToTarget = Vector3.Distance(monsterPosition, targetPosition);

        DebugLabel(camera, monsterPosition + Vector3.up * 2.2f, $"{CurrentMonsterBehaviourState}\nPos {monsterPosition.ToString("F1")}\nPlayer dist {distToPlayer:F1}", stateColor);

        DebugLabel(camera, targetPosition + Vector3.up * 0.8f, $"TARGET\n{targetPosition.ToString("F1")}\nDist {distToTarget:F1}", Color.magenta);
    }

    private static Color GetDebugStateColor(MonsterBehaviourState state)
    {
        switch (state)
        {
            case MonsterBehaviourState.Wandering: return Color.green;
            case MonsterBehaviourState.Chasing:   return Color.red;
            default:                              return Color.gray;
        }
    }

    private void EnsureDebugResources()
    {
        if (debugLineMaterial == null)
        {
            debugLineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            debugLineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            debugLineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            debugLineMaterial.SetInt("_Cull",     (int)CullMode.Off);
            debugLineMaterial.SetInt("_ZWrite", 0);
        }

        if (debugLabelStyle == null)
        {
            debugLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
    }

    private static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
    {
        Vector3 s = cam.WorldToScreenPoint(world);
        gui = new Vector2(s.x, cam.pixelHeight - s.y);
        return s.z > 0f; // false when behind the camera
    }

    private static void DebugLine(Camera cam, Vector3 a, Vector3 b, Color color)
    {
        if (!WorldToGui(cam, a, out Vector2 pa) || !WorldToGui(cam, b, out Vector2 pb)) return;
        GL.Color(color);
        GL.Vertex3(pa.x, pa.y, 0f);
        GL.Vertex3(pb.x, pb.y, 0f);
    }

    private static void DebugCross(Camera cam, Vector3 p, float size, Color color)
    {
        DebugLine(cam, p + Vector3.left * size,    p + Vector3.right * size,   color);
        DebugLine(cam, p + Vector3.down * size,    p + Vector3.up * size,      color);
        DebugLine(cam, p + Vector3.back * size,    p + Vector3.forward * size, color);
    }

    private static void DebugCircle(Camera cam, Vector3 center, float radius, Color color, int segments = 32)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            DebugLine(cam, prev, next, color);
            prev = next;
        }
    }

    private void DebugLabel(Camera cam, Vector3 worldPos, string text, Color color)
    {
        if (!WorldToGui(cam, worldPos, out Vector2 p)) return;

        debugLabelStyle.normal.textColor = Color.black; // drop shadow
        GUI.Label(new Rect(p.x - 99f, p.y - 29f, 200f, 60f), text, debugLabelStyle);

        debugLabelStyle.normal.textColor = color;
        GUI.Label(new Rect(p.x - 100f, p.y - 30f, 200f, 60f), text, debugLabelStyle);
    }
#endif
}

