using System;
using System.Collections.Generic;
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

    Vector3[] crawlerPositions;
    Vector3[] crawlerPositionTargets;
    float[] crawlerSpeeds;
    Quaternion[] crawlerRotations;
    Vector3[] crawlerScales;
    Matrix4x4[] crawlerMatrices;
    RenderParams renderParams;

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

        crawlerPositions = new Vector3[Settings.crawlerCount];
        crawlerPositionTargets = new Vector3[Settings.crawlerCount];
        crawlerSpeeds = new float[Settings.crawlerCount];
        crawlerRotations = new Quaternion[Settings.crawlerCount];
        crawlerScales = new Vector3[Settings.crawlerCount];
        crawlerMatrices = new Matrix4x4[Settings.crawlerCount];
        crawlerSpawned = new bool[Settings.crawlerCount];
        crawlerTimeSinceLastFleeing = new float[Settings.crawlerCount];

        Vector3 actorPosition = playerController.transform.position;
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            InitializeCrawler(i, actorPosition);
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

        Vector3[] crawlerExitPositionsArray = new Vector3[crawlerExitPositions.Count];
        for (int i = 0; i < crawlerExitPositions.Count; i++)
        {
            crawlerExitPositionsArray[i] = crawlerExitPositions[i];
        }
        exitKDTree = new KDTree(crawlerExitPositionsArray, crawlerExitPositions.Count);
        exitKDTree.Rebuild();
    }

    static readonly ProfilerMarker ProfilerMarker_QueryExitKDTree = new ProfilerMarker("QueryExitKDTree");
    private void QueryExitKDTreeQuery(Vector3 Origin)
    {
        using var ProfilerMarker = ProfilerMarker_QueryExitKDTree.Auto();

        exitKDTreeResults.Clear();
        exitKDTreeQuery.Radius(exitKDTree, Origin, Settings.MaxDistanceToExit, exitKDTreeResults);
    }

    private void HandleDrawCalls()
    {
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

    private void FillMatrixFromIndex(int crawlerIndex)
    {
        crawlerMatrices[crawlerIndex] = Matrix4x4.TRS(crawlerPositions[crawlerIndex], crawlerRotations[crawlerIndex], crawlerScales[crawlerIndex]);
    }

    void InitializeCrawler(int crawlerIndex, Vector3 origin)
    {
        Vector2 RandomPositionInRadius = Random.insideUnitCircle * Settings.spawnRadius;
        Vector3 randomPosition = origin + new Vector3(RandomPositionInRadius.x, 0.0f, RandomPositionInRadius.y);
        if (!NavMeshUtil.GetClosestPointInNavMesh_Big(randomPosition, out crawlerPositions[crawlerIndex]))
        {
            Debug.LogWarning("Failed to find crawler initializing position");
        }
        crawlerPositionTargets[crawlerIndex] = crawlerPositions[crawlerIndex];
        SetCrawlerWalkSpeed(crawlerIndex);
        crawlerRotations[crawlerIndex] = Quaternion.identity;
        crawlerScales[crawlerIndex] = Vector3.one * Settings.scaleFactor;
        crawlerTimeSinceLastFleeing[crawlerIndex] = Settings.fleeingCooldown;
    }

    private void RebuildKDTree()
    {
        for(int i = 0; i < crawlerKDTree.Count; i++) 
        {
            crawlerKDTree.Points[i] = crawlerPositions[i];
        }
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
        crawlerKDTreeResults.Clear();
        crawlerKDTreeQuery.Radius(crawlerKDTree, actorPosition, Settings.despawnRadius, crawlerKDTreeResults);
        for (int i = 0; i < crawlerKDTreeResults.Count; i++)
        {
            int crawlerIndex = crawlerKDTreeResults[i];
            crawlerSpawned[crawlerIndex] = true;
        }
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            if (crawlerSpawned[i] == false)
            {
                InitializeCrawler(i, actorPosition);
            }
        }
    }

    static readonly ProfilerMarker ProfilerMarker_QueryKdTree_Radius = new ProfilerMarker("QueryKdTree_Radius");
    private void QueryKdTree_Radius(Vector3 Origin)
    {
        using var ProfilerMarker = ProfilerMarker_QueryKdTree_Radius.Auto();

        crawlerKDTreeResults.Clear();
        crawlerKDTreeQuery.Radius(crawlerKDTree, Origin, fleeingRadius, crawlerKDTreeResults);
    }

    private bool TryFindCrawlerExitPosition(out Vector3 result)
    {
        int exitInRangeCount = exitKDTreeResults.Count;
        if (exitInRangeCount == 0)
        {
            result = Vector3.zero;
            return false;
        }

        int RandomExitResultIndex = Random.Range(0,  exitInRangeCount);
        int crawlerExitIndex = exitKDTreeResults[RandomExitResultIndex];
        result = crawlerExitPositions[crawlerExitIndex];
        return true;
    }

    static readonly ProfilerMarker ProfilerMarker_HandleFleeingCrawlers = new ProfilerMarker("HandleFleeingCrawlers");
    private void HandleFleeingCrawlers()
    {
        using var ProfilerMarker = ProfilerMarker_HandleFleeingCrawlers.Auto();

        Vector3 actorPosition = playerController.transform.position;
        Vector3 actorForward = playerController.transform.forward;
        float sqrFleeingRadius = fleeingRadius * fleeingRadius;

        float currentTime = Time.unscaledTime;

        QueryExitKDTreeQuery(actorPosition);
        QueryKdTree_Radius(actorPosition);

        foreach (int crawlerIndex in crawlerKDTreeResults)
        {
            float timeSinceLastFleeingCrawler = currentTime - crawlerTimeSinceLastFleeing[crawlerIndex];
            if (!(timeSinceLastFleeingCrawler >= Settings.fleeingCooldown)) 
                continue;

            ref Vector3 crawlerPosition = ref crawlerPositions[crawlerIndex];
            Vector3 actorToCrawler = crawlerPosition - actorPosition;
            float sqrDistance = actorToCrawler.sqrMagnitude;
            float randomPercentage = Random.Range(0.2f, 1.0f);
            if (sqrDistance > sqrFleeingRadius * randomPercentage)
                continue;

            Vector3 flat = actorToCrawler; flat.y = 0f;
            Vector3 actorToCrawlerNormalized = flat.normalized;
            actorToCrawlerNormalized.y = 0.0f;
            actorForward.y = 0.0f;
            if (Vector3.Dot(actorForward.normalized, actorToCrawlerNormalized) < fleeingViewDot)
                continue;

            bool hasFoundTarget = true;
            Vector3 awayVector = actorToCrawlerNormalized;
            Vector3 targetCandidate = crawlerPosition + awayVector * Settings.fleeingDistance;
            Vector3 NewTarget;
            if (!NavMeshUtil.GetClosestPointInNavMesh_Small(targetCandidate, out NewTarget))
            { 
                if (!TryFindCrawlerExitPosition(out NewTarget))
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
            Vector3 toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];

            if (toTarget.sqrMagnitude <= sqrThreshold)
            {
                SetCrawlerWalkSpeed(crawlerIndex);
                if (!NavMeshUtil.PickNewTarget_RandomInCircle(crawlerPositions[crawlerIndex], Settings.wanderRadius, out crawlerPositionTargets[crawlerIndex]))
                {
                    Debug.LogError("Failed to find random new crawler target position");
                }
                toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];
            }

            crawlerPositions[crawlerIndex] = Vector3.MoveTowards(crawlerPositions[crawlerIndex], crawlerPositionTargets[crawlerIndex], crawlerSpeeds[crawlerIndex] * dt);

            Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatDirection);
                crawlerRotations[crawlerIndex] = Quaternion.RotateTowards(crawlerRotations[crawlerIndex], desired, Settings.turnSpeed * dt);
            }

            FillMatrixFromIndex(crawlerIndex);
        }
    }
}