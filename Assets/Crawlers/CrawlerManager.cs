using System.Collections.Generic;
using DataStructures.ViliWonka.KDTree;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class CrawlerManager : MonoBehaviour
{
    public bool enabled = true;
    public int count = 6000;

    [Header("Graphic")]
    public Mesh mesh;
    public Material material;
    public float scaleFactor = 0.1f;

    [Header("Movement")]
    public float turnSpeed = 360f;
    public float wanderRadius = 5f;
    public float arriveThreshold = 0.05f;
    public float fleeingRadius = 10.0f;
    public float fleeingDistance = 10.0f;

    [Header("Movement Speeds")]
    public float minWalkSpeed = 1.5f;
    public float maxWalkSpeed = 1.8f;
    public float minFleeSpeed = 2.8f;
    public float maxFleeSpeed = 3.1f;

    [Header("References")]
    public FirstPersonController FirstPersonController;

    private const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;
    private const int MAX_INSTANCE_COUNT_BY_KD_TREE = 1023;

    private Vector3[] crawlerPositions;
    private Vector3[] crawlerPositionTargets;
    private float[] crawlerSpeeds;
    private Quaternion[] crawlerRotations;
    private Vector3[] crawlerScales;
    private Matrix4x4[] crawlerMatrices;
    private RenderParams renderParams;

    KDTree kdTree;
    KDQuery kdQuery = new KDQuery();
    List<int> kdResults = new List<int>();

    private Vector3 GetClosestPointInNavMesh(Vector3 source, float maxDistance = 100f)
    {
        if (NavMesh.SamplePosition(source, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
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

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return crawlerPosition;
    }

    private Vector3 PickNewTarget_AwayFromPosition(Vector3 crawlerPosition, Vector3 position)
    {
        Vector3 crawlerPositionToPosition = crawlerPosition - position;
        Vector3 awayVector = crawlerPositionToPosition.normalized;
        Vector3 candidate = crawlerPosition + awayVector * fleeingDistance;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position;
    }

    private void SetCrawlerWalkSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(minWalkSpeed, maxWalkSpeed);
    }

    private void SetCrawlerFleeSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(minFleeSpeed, maxFleeSpeed);
    }

    private void FillMatrixFromIndex(int crawlerIndex)
    {
        crawlerMatrices[crawlerIndex] = Matrix4x4.TRS(crawlerPositions[crawlerIndex], crawlerRotations[crawlerIndex], crawlerScales[crawlerIndex]);
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

        crawlerPositions = new Vector3[count];
        crawlerPositionTargets = new Vector3[count];
        crawlerSpeeds = new float[count];
        crawlerRotations = new Quaternion[count];
        crawlerScales = new Vector3[count];
        crawlerMatrices = new Matrix4x4[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = Random.insideUnitSphere * 50f;
            crawlerPositions[i] = GetClosestPointInNavMesh(randomPosition);
            crawlerPositionTargets[i] = PickNewTarget_Random(crawlerPositions[i]);
            SetCrawlerWalkSpeed(i);
            crawlerRotations[i] = Random.rotation;
            crawlerScales[i] = Vector3.one * scaleFactor;
            FillMatrixFromIndex(i);
        }
    }

    private void RebuildKDTree()
    {
        kdTree = new KDTree(crawlerPositions, MAX_INSTANCE_COUNT_BY_KD_TREE);
        for(int i = 0; i < kdTree.Count; i++) 
        {
            kdTree.Points[i] = crawlerPositions[i];
        }
        kdTree.Rebuild();
    }

    void Update()
    {
        if (!enabled)
        {
            return;
        }

        RebuildKDTree();

        Vector3 actorPosition = FirstPersonController.transform.position;

        kdResults.Clear();
        kdQuery.Radius(kdTree, actorPosition, fleeingRadius, kdResults);
        for (int i = 0; i < kdResults.Count; i++)
        {
            int idx = kdResults[i];
            crawlerPositionTargets[idx] = PickNewTarget_AwayFromPosition(crawlerPositions[idx], actorPosition);
            SetCrawlerFleeSpeed(i);
        }

        float dt = Time.deltaTime;
        float sqrThreshold = arriveThreshold * arriveThreshold;

        for (int i = 0; i < count; i++)
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


        for (int i = 0; i < count; i += MAX_INSTANCE_COUNT_BY_GPU_CALL)
        {
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, count - i);
            int startInstanceIndex = i;
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }
}