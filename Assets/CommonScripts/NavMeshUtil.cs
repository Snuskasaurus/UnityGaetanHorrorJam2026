using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtil : MonoBehaviour
{
    const float MAX_DISTANCE_NAV_MESH_QUERY = 200.0f;

    static readonly ProfilerMarker ProfilerMarker_GetClosestPointInNavMesh = new ProfilerMarker("GetClosestPointInNavMesh");
    public static Vector3 GetClosestPointInNavMesh(Vector3 source)
    {
        using var ProfilerMarker = ProfilerMarker_GetClosestPointInNavMesh.Auto();

        if (NavMesh.SamplePosition(source, out NavMeshHit hit, MAX_DISTANCE_NAV_MESH_QUERY, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return source;
    }

    public static Vector3 PickNewTarget_Random(Vector3 origin, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);

        return GetClosestPointInNavMesh(candidate);
    }

    public static Vector3 PickNewTarget_AwayFromPosition(Vector3 origin, Vector3 position, float maxDistance)
    {
        Vector3 crawlerPositionToPosition = origin - position;
        Vector3 awayVector = crawlerPositionToPosition.normalized;
        Vector3 candidate = origin + awayVector * maxDistance;

        return GetClosestPointInNavMesh(candidate);
    }
}
