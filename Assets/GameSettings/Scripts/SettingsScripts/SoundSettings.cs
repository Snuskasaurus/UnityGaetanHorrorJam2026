using UnityEngine;

// Sounds of the player, used by SoundController.
// monsterLoudness is the volume sent to the monster hearing: 0 = silent, the hearing radius grows with it.
[CreateAssetMenu(fileName = "SoundSettings", menuName = "Game Settings/Sound Settings")]
public class SoundSettings : ScriptableObject
{
    [Header("Footsteps")]
    public Surface defaultSurface = Surface.Dirt;  // surface of the grounds that have no SurfaceType

    public AudioClip[] defaultFootsteps;  // used when the surface has no clips

    public SurfaceFootsteps[] surfaceFootsteps;

    [Header("Gaits")]
    public GaitSound crouchGait = new GaitSound(0.3f, 0f);
    public GaitSound walkGait = new GaitSound(0.7f, 1.5f);
    public GaitSound runGait = new GaitSound(1f, 5f);

    [Header("Landing")]
    public AudioClip[] landingSounds;

    [Range(0f, 1f)]
    public float landingVolume = 1f;

    public float landingLoudness = 4f;

    public float minAirTimeForLanding = 0.3f;  // seconds in the air before a landing sound plays, avoids sounds on small bumps
}

[System.Serializable]
public class SurfaceFootsteps
{
    public Surface surface;
    public AudioClip[] clips;
}

[System.Serializable]
public struct GaitSound
{
    [Range(0f, 1f)]
    public float volume;

    public float monsterLoudness;

    public GaitSound(float volume, float monsterLoudness)
    {
        this.volume = volume;
        this.monsterLoudness = monsterLoudness;
    }
}
