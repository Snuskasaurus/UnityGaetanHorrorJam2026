using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class RoomManager
{
    private static List<Room> RoomList = new List<Room>();

    public static void RegisterRoom(Room room)
    {
        RoomList.Add(room);
    }
    public static void UnregisterRoom(Room room)
    {
        RoomList.Remove(room);
    }

    public static bool TryGetRandomRoom(ref Room room)
    {
        if (RoomList.Count == 0)
        {
            Debug.LogError("There are no rooms registered in list");
            return false;
        }

        int roomIndex = 0;
        if (RoomList.Count > 1)
        {
            roomIndex = Random.Range(0, RoomList.Count);
        }
        room = RoomList[roomIndex];
        return true;
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

        Vector3 targetPositionLocalSpace = NavMeshUtil.PickNewTarget_RandomInBox(origin, halfX, halfZ, height);;
        return boxCollider.gameObject.transform.TransformPoint(targetPositionLocalSpace);
    }

    private void OnEnable()
    {
        RoomManager.RegisterRoom(this);
    }

    private void OnDisable()
    {
        RoomManager.UnregisterRoom(this);
    }
}
