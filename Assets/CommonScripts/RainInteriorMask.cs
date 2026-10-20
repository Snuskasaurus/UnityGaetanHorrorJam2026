using UnityEngine;

// Kills the rain particles inside every InteriorVolume, through the Triggers module of the rain.
// The Triggers module only checks the colliders it is given, so the list is rebuilt when a volume appears or disappears.
[RequireComponent(typeof(ParticleSystem))]
public class RainInteriorMask : MonoBehaviour
{
    private ParticleSystem rain;

    private void Awake()
    {
        rain = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        InteriorVolume.Changed += RefreshVolumes;
        RefreshVolumes();
    }

    private void OnDisable()
    {
        InteriorVolume.Changed -= RefreshVolumes;
    }

    private void RefreshVolumes()
    {
        ParticleSystem.TriggerModule trigger = rain.trigger;
        trigger.enabled = true;
        trigger.inside = ParticleSystemOverlapAction.Kill;
        trigger.outside = ParticleSystemOverlapAction.Ignore;

        while (trigger.colliderCount > 0)
        {
            trigger.RemoveCollider(0);
        }
        foreach (InteriorVolume volume in InteriorVolume.All)
        {
            if (volume.Volume != null)
            {
                trigger.AddCollider(volume.Volume);
            }
        }
    }
}
