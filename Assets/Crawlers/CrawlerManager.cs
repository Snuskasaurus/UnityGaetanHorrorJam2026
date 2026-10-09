using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class CrawlerManager : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int count = 6000;
    public float scaleFactor = 0.1f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float turnSpeed = 360f;
    public float wanderRadius = 5f;
    public float arriveThreshold = 0.05f;

    private const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;

    private Vector3[] positions;
    private Vector3[] targets;
    private float[] speeds;
    private Quaternion[] rotations;
    private Vector3[] scales;
    private Matrix4x4[] matrices;
    private RenderParams renderParams;

    private Vector3 GetClosestPointInNavMesh(Vector3 source, float maxDistance = 100f)
    {
        if (NavMesh.SamplePosition(source, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogWarning("Can't find point in nav mesh");
        return source;
    }

    private Vector3 PickNewTarget(Vector3 from)
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = from + new Vector3(offset.x, 0f, offset.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return from;
    }

    private void FillMatrixFromIndex(int index)
    {
        matrices[index] = Matrix4x4.TRS(positions[index], rotations[index], scales[index]);
    }

    void Start()
    {
        positions = new Vector3[count];
        targets = new Vector3[count];
        speeds = new float[count];
        rotations = new Quaternion[count];
        scales = new Vector3[count];
        matrices = new Matrix4x4[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = Random.insideUnitSphere * 50f;
            positions[i] = GetClosestPointInNavMesh(randomPosition);
            targets[i] = PickNewTarget(positions[i]);
            speeds[i] = moveSpeed * Random.Range(0.7f, 1.3f);
            rotations[i] = Random.rotation;
            scales[i] = Vector3.one * scaleFactor;
            FillMatrixFromIndex(i);
        }

        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true
        };
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float sqrThreshold = arriveThreshold * arriveThreshold;

        for (int i = 0; i < count; i++)
        {
            Vector3 toTarget = targets[i] - positions[i];

            if (toTarget.sqrMagnitude <= sqrThreshold)
            {
                targets[i] = PickNewTarget(positions[i]);
                toTarget = targets[i] - positions[i];
            }

            positions[i] = Vector3.MoveTowards(positions[i], targets[i], speeds[i] * dt);

            Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(flatDirection);
                rotations[i] = Quaternion.RotateTowards(rotations[i], desired, turnSpeed * dt);
            }

            FillMatrixFromIndex(i);
        }

        for (int i = 0; i < count; i += MAX_INSTANCE_COUNT_BY_GPU_CALL)
        {
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, count - i);
            int startInstanceIndex = i;
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices, instanceCount, startInstanceIndex);
        }
    }
}