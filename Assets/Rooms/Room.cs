using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public static class RoomManager
{
    private static List<Room> RoomList = new List<Room>();
    private static List<int> CacheIndexes = new List<int>();

    public static bool TryGetClosestRoomAtPosition(ref Room room, Vector3 position, float radius)
    {
        #if UNITY_EDITOR
        if (!CheckHasRoomRegistered())
            return false;
        #endif

        float sqrRadius = radius * radius;

        float lastSqrDistance = float.MaxValue;
        int lastRoomIndex = -1;
        for (int i = 0; i < RoomList.Count; i++)
        {
            Room candidateRoom = RoomList[i];
            if (candidateRoom.RoomIsInSqrRadius(position, sqrRadius, out float sqrDistance))
            {
                if (sqrDistance < lastSqrDistance)
                {
                    lastRoomIndex = i;
                    lastSqrDistance = sqrDistance;
                }
            }
        }

        return true;
    }

    public static bool TryGetRandomRoom(ref Room room, Vector3 position, float avoidanceRadius)
    {
        #if UNITY_EDITOR
        if (!CheckHasRoomRegistered())
            return false;
        #endif

        for (int i = 0; i < RoomList.Count; i++)
        {
            Room candidateRoom = RoomList[i];

            bool IsInAvoidanceRadius = candidateRoom.RoomIsInSqrRadius(position, avoidanceRadius, out float sqrDistance);
            if (IsInAvoidanceRadius)
                continue;

            CacheIndexes.Add(i);
        }

        int RandomIndex = Random.Range(0, CacheIndexes.Count);
        if (CacheIndexes.Count == 0)
            return false;

        int roomIndex = CacheIndexes[RandomIndex];
        room = RoomList[roomIndex];
        CacheIndexes.Clear();

        return true;
    }

    public static bool TryGetRandomRoom(ref Room room)
    {
        #if UNITY_EDITOR
        if (!CheckHasRoomRegistered())
            return false;
        #endif

        int roomIndex = 0;
        if (RoomList.Count > 1)
        {
            roomIndex = Random.Range(0, RoomList.Count);
        }
        room = RoomList[roomIndex];
        return true;
    }

    private static bool CheckHasRoomRegistered()
    {
        if (RoomList.Count == 0)
        {
            Debug.LogError("There are no rooms registered in list");
            return false;
        }
        return true;
    }

    public static void RegisterRoom(Room room)
    {
        RoomList.Add(room);
    }

    public static void UnregisterRoom(Room room)
    {
        RoomList.Remove(room);
    }
}

public class Room : MonoBehaviour
{
    [SerializeField] private BoxCollider boxCollider;

    public Vector3 GetRandomPositionInsideRoom()
    {
        Vector3 origin = boxCollider.center;
        float halfX = boxCollider.size.x / 2.0f;
        float halfZ = boxCollider.size.y / 2.0f; // Julien Rogel (17-10-2026): Thanks
        float height = origin.y;

        if (!NavMeshUtil.PickNewTarget_RandomInBox(origin, halfX, halfZ, height, out var targetPositionLocalSpace))
        {
            Debug.LogWarning("Failed to pick random position inside room");
        }
        return boxCollider.gameObject.transform.TransformPoint(targetPositionLocalSpace);
    }

    public bool RoomIsInSqrRadius(Vector3 position, float sqrRadius, out float sqrDistance)
    {
        Transform t = boxCollider.transform;
        Vector3 localPos = t.InverseTransformPoint(position);

        Vector3 center = boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;

        Vector3 closestLocal = new Vector3(
            Mathf.Clamp(localPos.x, center.x - halfSize.x, center.x + halfSize.x),
            Mathf.Clamp(localPos.y, center.y - halfSize.y, center.y + halfSize.y),
            Mathf.Clamp(localPos.z, center.z - halfSize.z, center.z + halfSize.z));

        Vector3 closestWorld = t.TransformPoint(closestLocal);
        sqrDistance = (position - closestWorld).sqrMagnitude;

        return sqrDistance <= sqrRadius;
    }

    public bool PositionIsInsideRoom(Vector3 position)
    {
        Vector3 positionToLocalSpace = boxCollider.gameObject.transform.InverseTransformPoint(position);

        Vector3 offset = positionToLocalSpace - boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;

        return Mathf.Abs(offset.x) <= halfSize.x
            && Mathf.Abs(offset.y) <= halfSize.y
            && Mathf.Abs(offset.z) <= halfSize.z;
    }

    private void OnEnable()
    {
        Vector3 scale = boxCollider.transform.localScale;
        if (scale != Vector3.one)
        {
            Debug.LogError($"Room '{boxCollider.name}' has a non-unit scale: {scale}", boxCollider);
        }

        RoomManager.RegisterRoom(this);
    }

    private void OnDisable()
    {
        RoomManager.UnregisterRoom(this);
    }
    
    void OnDrawGizmos()
    {
        Vector3 gizmoPosition = transform.TransformPoint(boxCollider.center);
        Gizmos.DrawIcon(gizmoPosition, "IconRoom.png", true);
    }
}
