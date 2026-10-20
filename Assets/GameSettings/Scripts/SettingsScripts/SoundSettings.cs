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

    [Header("Rain")]
    public AudioClip rainLoop;  // continuous rain, played in loop around the player

    [Range(0f, 1f)]
    public float rainVolume = 0.6f;

    [Range(0f, 1f)]
    public float shelteredRainVolume = 0.15f;  // volume inside an InteriorVolume, 0 = silent

    public float shelteredRainCutoff = 800f;  // Hz, low-pass inside an InteriorVolume: the rain is heard muffled through the walls

    public float outdoorRainCutoff = 22000f;  // Hz, outside the rain is not filtered

    public float rainFadeTime = 0.6f;  // seconds to go from outdoor to sheltered rain, or back

    // Reverb of the player sounds inside an InteriorVolume, tuned for small concrete rooms
    [Header("Bunker Reverb")]
    [Range(-10000f, 0f)]
    public float reverbRoom = -1000f;  // mB, level of the reverb, -10000 = none

    [Range(-10000f, 0f)]
    public float reverbRoomHF = -400f;  // mB, high frequencies of the reverb, lower = darker

    [Range(0.1f, 20f)]
    public float reverbDecayTime = 1.4f;  // seconds, how long the echo lasts, longer = bigger room

    [Range(0.1f, 2f)]
    public float reverbDecayHFRatio = 0.6f;  // the high frequencies die faster on concrete

    [Range(-10000f, 1000f)]
    public float reverbReflectionsLevel = -700f;  // mB, first echoes on the walls

    [Range(0f, 0.3f)]
    public float reverbReflectionsDelay = 0.01f;  // seconds, small = close walls

    [Range(-10000f, 2000f)]
    public float reverbLevel = 100f;  // mB, late reverb tail

    [Range(0f, 0.1f)]
    public float reverbDelay = 0.015f;  // seconds

    // Crawler sounds, driven by the zippo light: the darker it is, the closer and louder the swarm.
    // The light goes from 0 (zippo off) to 1 (flame at full power).
    [Header("Crawlers")]
    public AudioClip crawlerSwarmLoop;  // continuous crawling, played in loop

    public AnimationCurve swarmVolumeByLight = AnimationCurve.Linear(0f, 0.85f, 1f, 0.05f);

    // The swarm comes from a direction that slowly drifts around the player, so it does not blend with the rain
    [Range(0f, 1f)]
    public float swarmSpatialBlend = 0.7f;  // 0 = everywhere like the rain, 1 = fully placed

    public float swarmDistance = 4f;  // meters from the player

    public float swarmDriftSpeed = 0.05f;  // how fast the swarm direction turns around the player

    [Range(0f, 1f)]
    public float swarmSwellDepth = 0.35f;  // how much the swarm volume breathes, 0 = steady

    public float swarmSwellSpeed = 0.3f;

    [Range(0f, 1f)]
    public float rainDuckInDark = 0.6f;  // rain volume multiplier in the dark, so the swarm stands out

    // Second swarm layer, only heard in the dark: it comes from the other side of the player
    public AudioClip crawlerDarkSwarmLoop;

    public AnimationCurve darkSwarmVolumeByLight = new AnimationCurve(
        new Keyframe(0f, 0.7f, 0f, -2.5f),
        new Keyframe(0.4f, 0f, 0f, 0f));

    public AudioClip[] crawlerScurrySounds;  // the swarm running away when the light grows

    [Range(0f, 1f)]
    public float scurryLightIncrease = 0.25f;  // light gained at once needed to make the swarm run away

    [Range(0f, 1f)]
    public float scurryVolume = 0.8f;

    public AudioClip[] crawlerChitterSounds;  // short clicks played around the player

    public AnimationCurve chitterIntervalByLight = AnimationCurve.Linear(0f, 0.8f, 1f, 6f);  // seconds between two clicks

    public AnimationCurve chitterDistanceByLight = AnimationCurve.Linear(0f, 1.5f, 1f, 8f);  // meters from the player

    [Range(0f, 1f)]
    public float chitterVolume = 0.6f;

    public float crawlerLightSmoothing = 3f;  // how fast the swarm reacts to a light change
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
