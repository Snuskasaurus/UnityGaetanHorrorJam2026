using System;
using System.Collections.Generic;
using DataStructures.ViliWonka.KDTree;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements.Experimental;
using Random = UnityEngine.Random;

public class CrawlerManager : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] public int crawlerCount = 20000;
    [SerializeField] float spawnRadius = 36.0f;
    [SerializeField] float despawnRadius = 200.0f;

    [Header("Wandering")]
    [SerializeField] float turnSpeed = 360f;
    [SerializeField] float wanderRadius = 5f;
    [SerializeField] float arriveThreshold = 0.05f;
    [SerializeField] float minWalkSpeed = 1.5f;
    [SerializeField] float maxWalkSpeed = 1.8f;
    
    [Header("Fleeing")]
    [SerializeField] float fleeingRadiusStatic = 1.0f;
    [SerializeField] float fleeingRadiusWalking = 4.0f;
    [SerializeField] float fleeingRadiusSprinting = 10.0f;
    [SerializeField] float fleeingDistance = 10.0f;
    [SerializeField] float minFleeingSpeed = 2.8f;
    [SerializeField] float maxFleeingSpeed = 3.1f;
    [SerializeField] float fleeingRadiusTransitionDuration = 0.4f;
    [SerializeField] float fleeingCooldown = 1.0f;

    [Header("Graphic")]
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField] float scaleFactor = 0.1f;

    [Header("References")]
    [SerializeField] FirstPersonController FirstPersonController;

    const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;
    const int MAX_INSTANCE_COUNT_BY_KD_TREE = 1023;
    const float MAX_DISTANCE_NAV_MESH_QUERY = 200.0f;
    const int MAX_NAV_MESH_QUERY_EACH_FRAMES = 200;

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

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh = new ProfilerMarker("GetClosestPointInNavMesh");
    private Vector3 GetClosestPointInNavMesh(Vector3 source)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return source;
    }

    private Vector3 PickNewTarget_Random(Vector3 crawlerPosition)
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = crawlerPosition + new Vector3(offset.x, 0f, offset.y);

        return GetClosestPointInNavMesh(candidate);
    }

    private Vector3 PickNewTarget_AwayFromPosition(Vector3 crawlerPosition, Vector3 position)
    {
        Vector3 crawlerPositionToPosition = crawlerPosition - position;
        Vector3 awayVector = crawlerPositionToPosition.normalized;
        Vector3 candidate = crawlerPosition + awayVector * fleeingDistance;

        return GetClosestPointInNavMesh(candidate);
    }

    private void SetCrawlerWalkSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(minWalkSpeed, maxWalkSpeed);
    }

    private void SetCrawlerFleeSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(minFleeingSpeed, maxFleeingSpeed);
    }

    private void FillMatrixFromIndex(int crawlerIndex)
    {
        crawlerMatrices[crawlerIndex] = Matrix4x4.TRS(crawlerPositions[crawlerIndex], crawlerRotations[crawlerIndex], crawlerScales[crawlerIndex]);
    }

    void InitializeCrawler(int crawlerIndex, Vector3 origin)
    {
        Vector2 RandomPositionInRadius = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPosition = origin + new Vector3(RandomPositionInRadius.x, 0.0f, RandomPositionInRadius.y);
        crawlerPositions[crawlerIndex] = GetClosestPointInNavMesh(randomPosition);
        crawlerPositionTargets[crawlerIndex] = crawlerPositions[crawlerIndex];
        SetCrawlerWalkSpeed(crawlerIndex);
        crawlerRotations[crawlerIndex] = Quaternion.identity;
        crawlerScales[crawlerIndex] = Vector3.one * scaleFactor;
    }

    void Start()
    {
        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };

        crawlerPositions = new Vector3[crawlerCount];
        crawlerPositionTargets = new Vector3[crawlerCount];
        crawlerSpeeds = new float[crawlerCount];
        crawlerRotations = new Quaternion[crawlerCount];
        crawlerScales = new Vector3[crawlerCount];
        crawlerMatrices = new Matrix4x4[crawlerCount];
        crawlerBoolCache = new bool[crawlerCount];
        crawlerTimeSinceLastFleeing = new float[crawlerCount];

        Vector3 actorPosition = FirstPersonController.transform.position;
        for (int i = 0; i < crawlerCount; i++)
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
        float target;
        if (FirstPersonController.IsSprinting())
        {
            target = fleeingRadiusSprinting;
        }
        else if (FirstPersonController.IsWalking())
        {
            target = fleeingRadiusWalking;
        }
        else
        {
            target = fleeingRadiusStatic;
        }

        if (!Mathf.Approximately(target, fleeingRadiusTo))
        {
            fleeingRadiusFrom = fleeingRadius;
            fleeingRadiusTo = target;
            radiusTransitionT = 0f;
        }

        if (radiusTransitionT < 1f)
        {
            radiusTransitionT = Mathf.Min(1f, radiusTransitionT + Time.deltaTime / fleeingRadiusTransitionDuration);
            fleeingRadius = Mathf.Lerp(fleeingRadiusFrom, fleeingRadiusTo, Easing.OutCirc(radiusTransitionT));
        }
    }

    private void ReplaceFarCrawlers()
    {
        Array.Fill(crawlerBoolCache, false);
        Vector3 actorPosition = FirstPersonController.transform.position;
        kdTreeQuery.Radius(kdTree, actorPosition, despawnRadius, kdTreeResults);
        for (int i = 0; i < kdTreeResults.Count; i++)
        {
            int crawlerIndex = kdTreeResults[i];
            crawlerBoolCache[crawlerIndex] = true;
        }
        for (int i = 0; i < crawlerCount; i++)
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

        Vector3 actorPosition = FirstPersonController.transform.position;
        float sqrFleeingRadius = fleeingRadius * fleeingRadius;

        float currentTime = Time.unscaledTime;

        QueryKdTree_Radius(actorPosition);

        for (int i = 0; i < kdTreeResults.Count; i++)
        {
            int crawlerIndex = kdTreeResults[i];
            ref Vector3 crawlerPosition = ref crawlerPositions[crawlerIndex];

            float sqrDistance = (crawlerPosition - actorPosition).sqrMagnitude;
            float randomPercentage = Random.Range(0.2f, 1.0f);
            if (sqrDistance > sqrFleeingRadius * randomPercentage)
                continue;

            float timeSinceLastFleeingCrawler = currentTime - crawlerTimeSinceLastFleeing[crawlerIndex];
            if (timeSinceLastFleeingCrawler >= fleeingCooldown)
            {
                SetCrawlerFleeSpeed(crawlerIndex);
                crawlerTimeSinceLastFleeing[crawlerIndex] = currentTime;
                crawlerPositionTargets[crawlerIndex] = PickNewTarget_AwayFromPosition(crawlerPosition, actorPosition);
            }
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
        float sqrThreshold = arriveThreshold * arriveThreshold;

        for (int crawlerIndex = 0; crawlerIndex < crawlerCount; crawlerIndex++)
        {
            Vector3 toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];

            if (toTarget.sqrMagnitude <= sqrThreshold)
            {
                SetCrawlerWalkSpeed(crawlerIndex);
                toTarget = crawlerPositionTargets[crawlerIndex] - crawlerPositions[crawlerIndex];
                crawlerPositionTargets[crawlerIndex] = PickNewTarget_Random(crawlerPositions[crawlerIndex]);
            }

            crawlerPositions[crawlerIndex] = Vector3.MoveTowards(crawlerPositions[crawlerIndex], crawlerPositionTargets[crawlerIndex], crawlerSpeeds[crawlerIndex] * dt);

            Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatDirection);
                crawlerRotations[crawlerIndex] = Quaternion.RotateTowards(crawlerRotations[crawlerIndex], desired, turnSpeed * dt);
            }

            FillMatrixFromIndex(crawlerIndex);
        }
    }

    private void Update()
    {
        UpdateCrawlerPositions();

        for (int i = 0; i < crawlerCount; i += MAX_INSTANCE_COUNT_BY_GPU_CALL)
        {
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, crawlerCount - i);
            int startInstanceIndex = i;
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }
}