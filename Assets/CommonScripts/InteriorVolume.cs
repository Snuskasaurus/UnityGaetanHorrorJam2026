using System.Collections.Generic;
using UnityEngine;

// Closed space (bunker, dugout, covered trench): the rain particles are killed inside it,
// the rain sound gets quieter and muffled and the player sounds get a reverb while the player is inside.
// Place it inside the place and scale it to fill the space below the roof.
[RequireComponent(typeof(BoxCollider))]
public class InteriorVolume : MonoBehaviour
{
    private static readonly List<InteriorVolume> volumes = new List<InteriorVolume>();

    // Every active interior volume of the scene
    public static IReadOnlyList<InteriorVolume> All => volumes;

    // Raised when a volume is added or removed, so the rain can update its list
    public static event System.Action Changed;

    public BoxCollider Volume
    {
        get;
        private set;
    }

    // True when the point is inside one of the interior volumes
    public static bool IsInsideAny(Vector3 worldPoint)
    {
        foreach (InteriorVolume volume in volumes)
        {
            if (volume.Contains(worldPoint))
            {
                return true;
            }
        }
        return false;
    }

    // Works with a rotated or scaled volume: the point is moved into the local space of the box
    public bool Contains(Vector3 worldPoint)
    {
        if (Volume == null)
        {
            return false;
        }

        Vector3 local = transform.InverseTransformPoint(worldPoint) - Volume.center;
        Vector3 halfSize = Volume.size * 0.5f;
        return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y && Mathf.Abs(local.z) <= halfSize.z;
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        Volume = GetComponent<BoxCollider>();
        Volume.isTrigger = true;
    }

    private void OnEnable()
    {
        volumes.Add(this);
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        volumes.Remove(this);
        Changed?.Invoke();
    }

    // Shows the volume in the Scene view even when it is not selected, so it is easy to place
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.08f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
