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
    // Tuning values, read from the CrawlerSettings asset of GameSettings
    private CrawlerSettings Settings => GameSettings.Crawler;

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
    private float fleeingRadius = 0;

    Vector3[] crawlerPositions;
    Vector3[] crawlerPositionTargets;
    float[] crawlerSpeeds;
    Quaternion[] crawlerRotations;
    Vector3[] crawlerScales;
    Matrix4x4[] crawlerMatrices;
    RenderParams renderParams;

    bool[] crawlerBoolCache;
    float[] crawlerTimeSinceLastFleeing;

    KDTree kdTree;
    KDQuery kdTreeQuery = new KDQuery();
    List<int> kdTreeResults = new List<int>();

    struct TargetRequest
    {
        int Index;
        bool Fleeing;
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
        crawlerPositions[crawlerIndex] = NavMeshUtil.GetClosestPointInNavMesh_Big(randomPosition);
        crawlerPositionTargets[crawlerIndex] = crawlerPositions[crawlerIndex];
        SetCrawlerWalkSpeed(crawlerIndex);
        crawlerRotations[crawlerIndex] = Quaternion.identity;
        crawlerScales[crawlerIndex] = Vector3.one * Settings.scaleFactor;
    }

    void Start()
    {
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
        crawlerBoolCache = new bool[Settings.crawlerCount];
        crawlerTimeSinceLastFleeing = new float[Settings.crawlerCount];

        Vector3 actorPosition = playerController.transform.position;
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            InitializeCrawler(i, actorPosition);
        }

        kdTree = new KDTree(crawlerPositions, MAX_INSTANCE_COUNT_BY_KD_TREE);
    }

    private void RebuildKDTree()
    {
        for(int i = 0; i < kdTree.Count; i++) 
        {
            kdTree.Points[i] = crawlerPositions[i];
        }
        kdTree.Rebuild();
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
            radiusTransitionT = 0f;
        }

        if (radiusTransitionT < 1f)
        {
            radiusTransitionT = Mathf.Min(1f, radiusTransitionT + Time.deltaTime / Settings.fleeingRadiusTransitionDuration);
            fleeingRadius = Mathf.Lerp(fleeingRadiusFrom, fleeingRadiusTo, Easing.OutCirc(radiusTransitionT));
        }
    }

    private void ReplaceFarCrawlers()
    {
        Array.Fill(crawlerBoolCache, false);
        Vector3 actorPosition = playerController.transform.position;
        kdTreeQuery.Radius(kdTree, actorPosition, Settings.despawnRadius, kdTreeResults);
        for (int i = 0; i < kdTreeResults.Count; i++)
        {
            int crawlerIndex = kdTreeResults[i];
            crawlerBoolCache[crawlerIndex] = true;
        }
        for (int i = 0; i < Settings.crawlerCount; i++)
        {
            if (crawlerBoolCache[i] == false)
            {
                InitializeCrawler(i, actorPosition);
            }
        }
    }

    static readonly ProfilerMarker ProfilerMarker_QueryKdTree_Radius = new ProfilerMarker("QueryKdTree_Radius");
    private void QueryKdTree_Radius(Vector3 Origin)
    {
        using var ProfilerMarker = ProfilerMarker_QueryKdTree_Radius.Auto();

        kdTreeResults.Clear();
        kdTreeQuery.Radius(kdTree, Origin, fleeingRadius, kdTreeResults);
    }

    static readonly ProfilerMarker ProfilerMarker_HandleFleeingCrawlers = new ProfilerMarker("HandleFleeingCrawlers");
    private void HandleFleeingCrawlers()
    {
        using var ProfilerMarker = ProfilerMarker_HandleFleeingCrawlers.Auto();

        Vector3 actorPosition = playerController.transform.position;
        float sqrFleeingRadius = fleeingRadius * fleeingRadius;

        float currentTime = Time.unscaledTime;

        QueryKdTree_Radius(actorPosition);

        foreach (int crawlerIndex in kdTreeResults)
        {
            float timeSinceLastFleeingCrawler = currentTime - crawlerTimeSinceLastFleeing[crawlerIndex];
            if (!(timeSinceLastFleeingCrawler >= Settings.fleeingCooldown)) 
                continue;

            ref Vector3 crawlerPosition = ref crawlerPositions[crawlerIndex];
            float sqrDistance = (crawlerPosition - actorPosition).sqrMagnitude;
            float randomPercentage = Random.Range(0.2f, 1.0f);
            if (sqrDistance > sqrFleeingRadius * randomPercentage)
                continue;

            SetCrawlerFleeSpeed(crawlerIndex);
            crawlerTimeSinceLastFleeing[crawlerIndex] = currentTime;
            crawlerPositionTargets[crawlerIndex] = NavMeshUtil.PickNewTarget_AwayFromPosition(crawlerPosition, actorPosition, Settings.fleeingDistance);
        }
    }

    private void FixedUpdate()
    {
        ReplaceFarCrawlers();
        UpdateFleeingRadius();
        RebuildKDTree();
        HandleFleeingCrawlers();
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
                toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];
                crawlerPositionTargets[crawlerIndex] = NavMeshUtil.PickNewTarget_RandomInCircle(crawlerPositions[crawlerIndex], Settings.wanderRadius);
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

    private void Update()
    {
        UpdateCrawlerPositions();

        for (int i = 0; i < Settings.crawlerCount; i += MAX_INSTANCE_COUNT_BY_GPU_CALL)
        {
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, Settings.crawlerCount - i);
            int startInstanceIndex = i;
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }
}