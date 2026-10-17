using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class EscapeZoneManager
{
    private static List<EscapeZone> escapeZoneList = new List<EscapeZone>();
    private static List<int> CacheIndexes = new List<int>();
    public static bool showDebugs = true;

    public static bool GetRandomEscapeZone(ref EscapeZone escapeZone, Vector3 position, float radius)
    {
        if (escapeZoneList.Count <= 0)
            return false;

        for (int i = 0; i < escapeZoneList.Count; i++)
        {
            EscapeZone candidateEscapeZone = escapeZoneList[i];
            float distance = Vector3.Distance(candidateEscapeZone.transform.position, position);
            if (distance >= radius)
            {
                CacheIndexes.Add(i);
            }
        }

        if (CacheIndexes.Count == 0)
            return false;

        int RandomIndex = Random.Range(0, CacheIndexes.Count);
        int escapeZoneIndex = CacheIndexes[RandomIndex];
        escapeZone = escapeZoneList[escapeZoneIndex];
        CacheIndexes.Clear();

        return true;
    }

    public static void RegisterEscapeZone(EscapeZone escapeZone)
    {
        escapeZoneList.Add(escapeZone);
    }

    public static void UnregisterEscapeZone(EscapeZone escapeZone)
    {
        escapeZoneList.Remove(escapeZone);
    }
}

public class EscapeZone : MonoBehaviour
{
    private void OnEnable()
    {
        EscapeZoneManager.RegisterEscapeZone(this);
    }

    private void OnDisable()
    {
        EscapeZoneManager.UnregisterEscapeZone(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "IconEscapeZone.png", true);
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (!EscapeZoneManager.showDebugs)
            return;

        Camera mainCamera = Camera.main;

        DebugDraw.InitializeDebugs_IFN();

        DebugDraw.DebugCircle(mainCamera, transform.position, 1.0f, Color.red);
#endif
    }
}
