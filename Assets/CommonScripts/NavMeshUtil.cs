using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtil : MonoBehaviour
{
    const float MAX_DISTANCE_NAV_MESH_QUERY_BIG = 200.0f;
    const float MAX_DISTANCE_NAV_MESH_QUERY_SMALL = 10.0f;

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh_Big = new ProfilerMarker("GetClosestPointInNavMesh_Big");
    public static Vector3 GetClosestPointInNavMesh_Big(Vector3 source)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh_Big.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY_BIG, NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogError("Failed to find position on navmesh");
        return source;
    }

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh_Small = new ProfilerMarker("GetClosestPointInNavMesh_Small");
    public static Vector3 GetClosestPointInNavMesh_Small(Vector3 source)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh_Small.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY_SMALL, NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogError("Failed to find position on navmesh");
        return source;
    }

    public static Vector3 PickNewTarget_RandomInBox(Vector3 origin, float halfX, float halfZ, float height)
    {
        float positionX = Random.Range(-halfX, halfX);
        float positionZ = Random.Range(-halfZ, halfZ);
        Vector3 candidate = origin + new Vector3(positionX, height, positionZ);

        return GetClosestPointInNavMesh_Big(candidate);
    }

    public static Vector3 PickNewTarget_RandomInCircle(Vector3 origin, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);

        return GetClosestPointInNavMesh_Big(candidate);
    }

    public static Vector3 PickNewTarget_AwayFromPosition(Vector3 origin, Vector3 position, float maxDistance)
    {
        Vector3 crawlerPositionToPosition = origin - position;
        Vector3 awayVector = crawlerPositionToPosition.normalized;
        Vector3 candidate = origin + awayVector * maxDistance;

        return GetClosestPointInNavMesh_Big(candidate);
    }
}
