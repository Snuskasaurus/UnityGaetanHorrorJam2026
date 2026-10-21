using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Circular zone (on the XZ plane, height is ignored) that crawlers avoid:
/// - no crawler spawns inside it,
/// - wander / flee paths that would cross it are rejected,
/// - crawlers that end up inside it (zone enabled, moved or grown on top of them) flee out of it.
/// The zone is read every frame, so it can move (carried lamp) or change radius at runtime.
/// </summary>
public class CrawlerSafeZone : MonoBehaviour
{
    static readonly List<CrawlerSafeZone> s_ActiveZones = new List<CrawlerSafeZone>();
    public static IReadOnlyList<CrawlerSafeZone> ActiveZones => s_ActiveZones;

    [SerializeField, Min(0f)] float radius = 5f;
    [Tooltip("How far outside the radius crawlers run to when fleeing the zone.")]
    [SerializeField, Min(0f)] float fleeMargin = 1.5f;

    public float Radius
    {
        get => radius;
        set => radius = Mathf.Max(0f, value);
    }

    public float FleeMargin
    {
        get => fleeMargin;
        set => fleeMargin = Mathf.Max(0f, value);
    }

    void OnEnable()
    {
        if (!s_ActiveZones.Contains(this))
            s_ActiveZones.Add(this);
    }

    void OnDisable()
    {
        s_ActiveZones.Remove(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_ActiveZones.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        UnityEditor.Handles.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius);
        UnityEditor.Handles.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius + fleeMargin);
    }
#endif
}
