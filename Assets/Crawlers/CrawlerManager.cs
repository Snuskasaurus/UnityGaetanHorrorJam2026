using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataStructures.ViliWonka.KDTree;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements.Experimental;
using Random = UnityEngine.Random;

public class CrawlerManager : MonoBehaviour
{
    private static CrawlerManager INSTANCE;

    struct TargetRequest
    {
        int Index;
        bool Fleeing;
    }

    private CrawlerSettings Settings;

    [Header("Graphic")]
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    // References
    PlayerController playerController;
    ZippoController zippoController;

    const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;
    const int MAX_INSTANCE_COUNT_BY_KD_TREE = 1023;

    float fleeingRadiusFrom = 0.0f;
    float fleeingRadiusTo = 0.0f;
    float radiusTransitionT = 1.0f;
    private float fleeingRadius = 0.0f;
    private float fleeingViewDot = 0.0f;

    Vector2[] crawlerPositions;
    Vector2[] crawlerPositionTargets;
    float[] crawlerSpeeds;
    Matrix4x4[] crawlerMatrices;
    RenderParams renderParams;
    
    Vector3[] drawPositions;
    Quaternion[] drawRotations;
    Vector3[] drawScales;

    bool[] crawlerSpawned;
    float[] crawlerTimeSinceLastFleeing;

    KDTree crawlerKDTree;
    KDQuery crawlerKDTreeQuery = new KDQuery();
    List<int> crawlerKDTreeResults = new List<int>();

    List<Vector3> crawlerExitPositions = new List<Vector3>();

    KDTree exitKDTree;
    KDQuery exitKDTreeQuery = new KDQuery();
    List<int> exitKDTreeResults = new List<int>();

    private int drawCallCount = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Vec2ToVec3(Vector2 v)
    {
        return new Vector3(v.x, 0f, v.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Vec3ToVec2(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PUBLIC FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    public static void RegisterCrawlerExitPosition(Vector3 position)
    {
        INSTANCE.crawlerExitPositions.Add(position);
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  UNITY FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void Awake()
    {
        INSTANCE = this;

        Settings = GameSettings.Crawler;
        fleeingViewDot = math.cos(math.radians(Settings.fleeingViewAngle * 0.5f));

        playerController = Common.FindTypeInScene<PlayerController>();
        zippoController = Common.FindTypeInScene<ZippoController>();

        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };

        crawlerPositions = new Vector2[Settings.crawlerCount];
        crawlerPositionTargets = new Vector2[Settings.crawlerCount];
        crawlerSpeeds = new float[Settings.crawlerCount];
        crawlerMatrices = new Matrix4x4[Settings.crawlerCount];
        crawlerSpawned = new bool[Settings.crawlerCount];
        crawlerTimeSinceLastFleeing = new float[Settings.crawlerCount];

        drawPositions = new Vector3[Settings.crawlerCount];
        drawScales = new Vector3[Settings.crawlerCount];
        drawRotations = new Quaternion[Settings.crawlerCount];

        Vector3 actorPosition = playerController.transform.position;
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            InitializeCrawler(i, in actorPosition);
        }

        crawlerKDTree = new KDTree(crawlerPositions, MAX_INSTANCE_COUNT_BY_KD_TREE);
    }

    void Start()
    {
        drawCallCount = (Settings.crawlerCount + MAX_INSTANCE_COUNT_BY_GPU_CALL - 1) / MAX_INSTANCE_COUNT_BY_GPU_CALL;
        Debug.Log("There will be " + drawCallCount + " draw call for crawlers each frames");
    }

    private void FixedUpdate()
    {
        CreateExitKDTreeOnce();
        ReplaceFarCrawlers();
        UpdateFleeingRadius();
        RebuildKDTree();
        HandleFleeingCrawlers();
    }

    private void Update()
    {
        UpdateCrawlerPositions();
        HandleDrawCalls();
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PRIVATE FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void CreateExitKDTreeOnce()
    {
        if (exitKDTree != null || crawlerExitPositions.Count == 0)
            return;

        Vector2[] crawlerExitPositionsArray = new Vector2[crawlerExitPositions.Count];
        for (int i = 0; i < crawlerExitPositions.Count; i++)
        {
            crawlerExitPositionsArray[i] = Vec3ToVec2(crawlerExitPositions[i]);
        }
        exitKDTree = new KDTree(crawlerExitPositionsArray, crawlerExitPositions.Count);
        exitKDTree.Rebuild();
    }

    static readonly ProfilerMarker ProfilerMarker_QueryExitKDTree = new ProfilerMarker("QueryExitKDTree");
    private void QueryExitKDTreeQuery(Vector2 Origin)
    {
        using var ProfilerMarker = ProfilerMarker_QueryExitKDTree.Auto();

        exitKDTreeResults.Clear();
        exitKDTreeQuery.Radius(exitKDTree, Origin, Settings.MaxDistanceToExit, exitKDTreeResults);
    }

    private void HandleDrawCalls()
    {
        for (int crawlerIndex = 0; crawlerIndex < Settings.crawlerCount; crawlerIndex++)
        {
            Vector3 crawlerPosition3D = Vec2ToVec3(crawlerPositions[crawlerIndex]);
            if (NavMesh.SamplePosition(crawlerPosition3D, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                drawPositions[crawlerIndex] = hit.position;
            }
            else
            {
                drawPositions[crawlerIndex].x = crawlerPosition3D.x;
                drawPositions[crawlerIndex].z = crawlerPosition3D.z;
                //drawPositions[crawlerIndex].y = drawPositions[crawlerIndex].y;
            }
        }

        for (int crawlerIndex = 0; crawlerIndex < Settings.crawlerCount; crawlerIndex++)
        {
            crawlerMatrices[crawlerIndex] = Matrix4x4.TRS(drawPositions[crawlerIndex], drawRotations[crawlerIndex], drawScales[crawlerIndex]);
        }

        for (int drawCallIndex = 0; drawCallIndex < drawCallCount; drawCallIndex++)
        {
            int startInstanceIndex = drawCallIndex * MAX_INSTANCE_COUNT_BY_GPU_CALL;
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, Settings.crawlerCount - startInstanceIndex);
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }

    private void SetCrawlerWalkSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(Settings.minWalkSpeed, Settings.maxWalkSpeed);
    }

    private void SetCrawlerFleeSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(Settings.minFleeingSpeed, Settings.maxFleeingSpeed);
    }

    void InitializeCrawler(int crawlerIndex, in Vector3 origin)
    {
        Vector2 RandomPositionInRadius = Random.insideUnitCircle * Settings.spawnRadius;
        Vector3 randomPosition = origin + new Vector3(RandomPositionInRadius.x, 0.0f, RandomPositionInRadius.y);
        if (!NavMeshUtil.GetClosestPointInNavMesh_Small(randomPosition, out Vector3 tempPosition))
        {
            Debug.LogWarning("Failed to find crawler initializing position");
        }
        crawlerPositions[crawlerIndex] = Vec3ToVec2(tempPosition);
        crawlerPositionTargets[crawlerIndex] = crawlerPositions[crawlerIndex];
        SetCrawlerWalkSpeed(crawlerIndex);
        drawRotations[crawlerIndex] = Quaternion.identity;
        drawScales[crawlerIndex] = Vector3.one * Settings.scaleFactor;
        crawlerTimeSinceLastFleeing[crawlerIndex] = 0.0f;
    }

    private void RebuildKDTree()
    {
        crawlerKDTree.Rebuild();
    }

    private void UpdateFleeingRadius()
    {
        float flamePower = zippoController.flamePower;

        float target = 0.0f;
        switch (playerController.CurrentMovementState)
        {
            case MovementState.Crouching:
            {
                target = FloatRange.Lerp(Settings.fleeingRadiusStatic, flamePower);
            } break;
            case MovementState.Walking:
            {
                target = FloatRange.Lerp(Settings.fleeingRadiusWalking, flamePower);
            } break;
            case MovementState.Running:
            {
                target = FloatRange.Lerp(Settings.fleeingRadiusRunning, flamePower);
            } break;
        }

        if (!Mathf.Approximately(target, fleeingRadiusTo))
        {
            fleeingRadiusFrom = fleeingRadius;
            fleeingRadiusTo = target;
            radiusTransitionT = 0.0f;
        }

        if (radiusTransitionT < 1.0f)
        {
            radiusTransitionT = Mathf.Min(1.0f, radiusTransitionT + Time.deltaTime / Settings.fleeingRadiusTransitionDuration);
            fleeingRadius = Mathf.Lerp(fleeingRadiusFrom, fleeingRadiusTo, Easing.OutCirc(radiusTransitionT));
        }
    }

    private void ReplaceFarCrawlers()
    {
        Array.Fill(crawlerSpawned, false);
        Vector3 actorPosition = playerController.transform.position;
        Vector2 actorPosition2D = Vec3ToVec2(actorPosition);
        crawlerKDTreeResults.Clear();
        crawlerKDTreeQuery.Radius(crawlerKDTree, actorPosition2D, Settings.despawnRadius, crawlerKDTreeResults);
        for (int i = 0; i < crawlerKDTreeResults.Count; i++)
        {
            int crawlerIndex = crawlerKDTreeResults[i];
            crawlerSpawned[crawlerIndex] = true;
        }
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            if (crawlerSpawned[i] == false)
            {
                InitializeCrawler(i, in actorPosition);
            }
        }
    }

    static readonly ProfilerMarker ProfilerMarker_QueryKdTree_Radius = new ProfilerMarker("QueryKdTree_Radius");
    private void QueryKdTree_Radius(Vector2 Origin)
    {
        using var ProfilerMarker = ProfilerMarker_QueryKdTree_Radius.Auto();

        crawlerKDTreeResults.Clear();
        crawlerKDTreeQuery.Radius(crawlerKDTree, Origin, fleeingRadius, crawlerKDTreeResults);
    }

    private bool TryFindCrawlerExitPosition(ref Vector2 result)
    {
        int exitInRangeCount = exitKDTreeResults.Count;
        if (exitInRangeCount == 0)
        {
            return false;
        }

        int RandomExitResultIndex = Random.Range(0,  exitInRangeCount);
        int crawlerExitIndex = exitKDTreeResults[RandomExitResultIndex];
        result = Vec3ToVec2(crawlerExitPositions[crawlerExitIndex]);
        return true;
    }

    static readonly ProfilerMarker ProfilerMarker_HandleFleeingCrawlers = new ProfilerMarker("HandleFleeingCrawlers");
    private void HandleFleeingCrawlers()
    {
        using var ProfilerMarker = ProfilerMarker_HandleFleeingCrawlers.Auto();

        Vector2 actorPosition2D = Vec3ToVec2(playerController.transform.position);
        Vector2 actorForward2D = Vec3ToVec2(playerController.transform.forward);
        float sqrFleeingRadius = fleeingRadius * fleeingRadius;

        float currentTime = Time.unscaledTime;

        QueryExitKDTreeQuery(actorPosition2D);
        QueryKdTree_Radius(actorPosition2D);

        foreach (int crawlerIndex in crawlerKDTreeResults)
        {
            float timeSinceLastFleeingCrawler = currentTime - crawlerTimeSinceLastFleeing[crawlerIndex];
            if (!(timeSinceLastFleeingCrawler >= Settings.fleeingCooldown)) 
                continue;

            Vector2 crawlerPosition = crawlerPositions[crawlerIndex];
            Vector2 actorToCrawler = crawlerPosition - actorPosition2D;
            float sqrDistance = actorToCrawler.sqrMagnitude;
            float randomPercentage = Random.Range(0.2f, 1.0f);
            if (sqrDistance > sqrFleeingRadius * randomPercentage)
                continue;

            Vector2 actorToCrawlerNormalized = actorToCrawler.normalized;
            if (Vector2.Dot(actorForward2D.normalized, actorToCrawlerNormalized) < fleeingViewDot)
                continue;

            bool hasFoundTarget = true;
            Vector2 awayVector = actorToCrawlerNormalized;
            Vector2 targetCandidate = crawlerPosition + awayVector * Settings.fleeingDistance;
            Vector2 NewTarget = Vector2.zero;
            if (!NavMeshUtil.GetClosestPointInNavMesh_Small(Vec2ToVec3(targetCandidate), out Vector3 NewTarget3D))
            {
                NewTarget = Vec3ToVec2(NewTarget3D);
                if (!TryFindCrawlerExitPosition(ref NewTarget))
                {
                    hasFoundTarget = false;
                }
            }
            if (hasFoundTarget)
            {
                SetCrawlerFleeSpeed(crawlerIndex);
                crawlerTimeSinceLastFleeing[crawlerIndex] = currentTime;
                crawlerPositionTargets[crawlerIndex] = NewTarget;
            }
        }
    }

    static readonly ProfilerMarker ProfilerMarker_UpdateCrawlerPositions = new ProfilerMarker("UpdateCrawlerPositions");
    void UpdateCrawlerPositions()
    {
        using var ProfilerMarker = ProfilerMarker_UpdateCrawlerPositions.Auto();

        float dt = Time.deltaTime;
        float sqrThreshold = Settings.arriveThreshold * Settings.arriveThreshold;

        for (int crawlerIndex = 0; crawlerIndex < Settings.crawlerCount; crawlerIndex++)
        {
            Vector2 toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];

            if (toTarget.sqrMagnitude <= sqrThreshold)
            {
                SetCrawlerWalkSpeed(crawlerIndex);
                Vector3 CrawlerPosition = Vec2ToVec3(crawlerPositions[crawlerIndex]);
                if (!NavMeshUtil.PickNewTarget_RandomInCircle(CrawlerPosition, Settings.wanderRadius, out Vector3 newCrawlerPosition))
                {
                    Debug.LogError("Failed to find random new crawler target position");
                }
                crawlerPositionTargets[crawlerIndex] = Vec3ToVec2(newCrawlerPosition);
                toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];
            }

            crawlerPositions[crawlerIndex] = Vector2.MoveTowards(crawlerPositions[crawlerIndex], crawlerPositionTargets[crawlerIndex], crawlerSpeeds[crawlerIndex] * dt);

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(Vec2ToVec3(toTarget));
                drawRotations[crawlerIndex] = Quaternion.RotateTowards(drawRotations[crawlerIndex], desired, Settings.turnSpeed * dt);
            }
        }
    }
}