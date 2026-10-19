using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtil : MonoBehaviour
{
    const float MAX_DISTANCE_NAV_MESH_QUERY_BIG = 200.0f;
    const float MAX_DISTANCE_NAV_MESH_QUERY_SMALL = 10.0f;

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh_Big = new ProfilerMarker("GetClosestPointInNavMesh_Big");
    public static bool GetClosestPointInNavMesh_Big(Vector3 source, out Vector3 result)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh_Big.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY_BIG, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        else
        {
            result = source;
            return false;
        }
    }

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh_Small = new ProfilerMarker("GetClosestPointInNavMesh_Small");
    public static bool GetClosestPointInNavMesh_Small(Vector3 source, out Vector3 result)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh_Small.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY_SMALL, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        else
        {
            result = source;
            return false;
        }
    }

    public static bool PickNewTarget_RandomInBox(Vector3 origin, float halfX, float halfZ, float height, out Vector3 result)
    {
        float positionX = Random.Range(-halfX, halfX);
        float positionZ = Random.Range(-halfZ, halfZ);
        Vector3 candidate = origin + new Vector3(positionX, height, positionZ);
        bool success = GetClosestPointInNavMesh_Small(candidate, out result);
        return success;
    }

    public static bool PickNewTarget_RandomInCircle(Vector3 origin, float radius, out Vector3 result)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);
        bool success = GetClosestPointInNavMesh_Small(candidate, out result);
        return success;
    }
}
