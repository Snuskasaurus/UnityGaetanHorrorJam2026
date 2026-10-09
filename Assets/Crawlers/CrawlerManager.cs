using System;
using System.Collections.Generic;
using DataStructures.ViliWonka.KDTree;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
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
    [SerializeField] float fleringRadiusTransitionDuration = 0.4f;

    [Header("Graphic")]
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField] float scaleFactor = 0.1f;

    [Header("References")]
    [SerializeField] FirstPersonController FirstPersonController;

    const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;
    const int MAX_INSTANCE_COUNT_BY_KD_TREE = 1023;
    const float MAX_DISTANCE_NAV_MESH_QUERY = 200.0f;

    float fleeingRadiusFrom = 0.0f;
    float fleeingRadiusTo = 0.0f;
    float radiusTransitionT = 1.0f;

    Vector3[] crawlerPositions;
    Vector3[] crawlerPositionTargets;
    float[] crawlerSpeeds;
    Quaternion[] crawlerRotations;
    Vector3[] crawlerScales;
    Matrix4x4[] crawlerMatrices;
    RenderParams renderParams;

    bool[] crawlerBoolCache;

    private float fleeingRadius = 0;

    KDTree kdTree;
    KDQuery kdTreeQuery = new KDQuery();
    List<int> kdTreeResults = new List<int>();

    private Vector3 GetClosestPointInNavMesh(Vector3 source)
    {
        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY, NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogWarning("Can't find point in nav mesh");
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
        crawlerPositionTargets[crawlerIndex] = PickNewTarget_Random(crawlerPositions[crawlerIndex]);
        SetCrawlerWalkSpeed(crawlerIndex);
        crawlerRotations[crawlerIndex] = Quaternion.identity;
        crawlerScales[crawlerIndex] = Vector3.one * scaleFactor;
    }

    void Start()
    {
        if (!enabled)
        {
            return;
        }

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
            radiusTransitionT = Mathf.Min(1f, radiusTransitionT + Time.deltaTime / fleringRadiusTransitionDuration);
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

    private void FixedUpdate()
    {
        if (!enabled)
        {
            return;
        }

        ReplaceFarCrawlers();

        UpdateFleeingRadius();

        RebuildKDTree();

        Vector3 actorPosition = FirstPersonController.transform.position;

        float sqrFleeingRadius = fleeingRadius * fleeingRadius;

        kdTreeResults.Clear();
        kdTreeQuery.Radius(kdTree, actorPosition, fleeingRadius, kdTreeResults);
        for (int i = 0; i < kdTreeResults.Count; i++)
        {
            int crawlerIndex = kdTreeResults[i];
            ref Vector3 crawlerPosition = ref crawlerPositions[crawlerIndex];

            // float sqrDistance = (crawlerPosition - actorPosition).sqrMagnitude;
            // float randomPercentage = Random.Range(0.2f, 1.0f);
            // if (sqrDistance > sqrFleeingRadius * randomPercentage)
            //     continue;

            crawlerPositionTargets[crawlerIndex] = PickNewTarget_AwayFromPosition(crawlerPosition, actorPosition);
            SetCrawlerFleeSpeed(crawlerIndex);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float sqrThreshold = arriveThreshold * arriveThreshold;

        for (int i = 0; i < crawlerCount; i++)
        {
            Vector3 toTarget = crawlerPositionTargets[i] - crawlerPositions[i];

            if (toTarget.sqrMagnitude <= sqrThreshold)
            {
                SetCrawlerWalkSpeed(i);
                crawlerPositionTargets[i] = PickNewTarget_Random(crawlerPositions[i]);
                toTarget = crawlerPositionTargets[i] - crawlerPositions[i];
            }

            crawlerPositions[i] = Vector3.MoveTowards(crawlerPositions[i], crawlerPositionTargets[i], crawlerSpeeds[i] * dt);

            Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatDirection);
                crawlerRotations[i] = Quaternion.RotateTowards(crawlerRotations[i], desired, turnSpeed * dt);
            }

            FillMatrixFromIndex(i);
        }

        for (int i = 0; i < crawlerCount; i += MAX_INSTANCE_COUNT_BY_GPU_CALL)
        {
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, crawlerCount - i);
            int startInstanceIndex = i;
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }
}