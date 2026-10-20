using UnityEngine;
using UnityEngine.Audio;

// Plays every sound of the player on its AudioSource: footsteps, landing, and the sounds wired from the Inspector.
// The rain loop has its own AudioSource, created at start. Inside an InteriorVolume it gets quieter and muffled by a low-pass filter.
// The player sounds (footsteps, zippo...) also get a reverb inside an InteriorVolume, so a bunker sounds closed.
// The footsteps follow MovementController.OnStep, their volume and how far the monster hears them depend on the gait.
[RequireComponent(typeof(AudioSource), typeof(PlayerController), typeof(MovementController))]
public class SoundController : MonoBehaviour
{
    // Tuning values, read from the sound settings of GameSettings
    private SoundSettings Settings => GameSettings.Sound;

    // References
    private AudioSource audioSource;
    private PlayerController player;
    private MovementController movement;

    // Internal variables
    private Collider lastGroundCollider;
    private SurfaceType lastGroundSurface;
    private AudioClip lastPlayedClip;
    private float airborneStartTime;
    private AudioSource rainSource;
    private AudioLowPassFilter rainFilter;
    private AudioReverbFilter interiorReverb;
    private float shelterBlend;  // 0 = outdoor, 1 = sheltered

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        player = GetComponent<PlayerController>();
        movement = GetComponent<MovementController>();
        CreateRainSource();
        CreateInteriorReverb();
    }

    private void Start()
    {
        // No fade at the start of the level, the player is already outside or inside
        shelterBlend = IsSheltered() ? 1f : 0f;
        ApplyShelterBlend();

        if (Settings.rainLoop != null)
        {
            rainSource.clip = Settings.rainLoop;
            rainSource.Play();
        }
    }

    private void Update()
    {
        UpdateShelter();
    }

    private void OnEnable()
    {
        movement.OnStep.AddListener(PlayFootstep);
        player.OnAirborneStateChanged.AddListener(OnAirborneStateChanged);
    }

    private void OnDisable()
    {
        movement.OnStep.RemoveListener(PlayFootstep);
        player.OnAirborneStateChanged.RemoveListener(OnAirborneStateChanged);
    }

    // Plays a sound on the player, the monster does not hear it. Can be wired on UnityEvents.
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Plays a clip or an Audio Random Container on the main voice of the source, it cuts the previous one.
    // Can be wired on UnityEvents.
    public void PlayResource(AudioResource resource)
    {
        if (resource != null)
        {
            audioSource.resource = resource;
            audioSource.Play();
        }
    }

    // Plays a sound that the monster can hear, from code. A loudness of 0 keeps it silent for the monster.
    public void PlaySound(AudioClip clip, float volume, float monsterLoudness)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        if (monsterLoudness > 0f)
        {
            MonsterManager.SendSoundToMonsterHearing(transform.position, monsterLoudness);
        }
    }

    private void PlayFootstep()
    {
        GaitSound gait = GetCurrentGait();
        PlaySound(PickRandomClip(GetFootstepClips()), gait.volume, gait.monsterLoudness);
    }

    // A landing sound only plays after a real fall, not when the ground check flickers on a small bump
    private void OnAirborneStateChanged(StateChange<AirborneState> change)
    {
        if (change.Previous == AirborneState.Grounded)
        {
            airborneStartTime = Time.time;
            return;
        }

        if (change.Current == AirborneState.Grounded && Time.time - airborneStartTime >= Settings.minAirTimeForLanding)
        {
            PlaySound(PickRandomClip(Settings.landingSounds), Settings.landingVolume, Settings.landingLoudness);
        }
    }

    private GaitSound GetCurrentGait()
    {
        if (player.CurrentPosture == Posture.Crouching)
        {
            return Settings.crouchGait;
        }
        if (player.CurrentMovementState == MovementState.Running)
        {
            return Settings.runGait;
        }
        return Settings.walkGait;
    }

    // Clips of the surface under the player, a ground without SurfaceType uses the default surface
    private AudioClip[] GetFootstepClips()
    {
        Collider groundCollider = player.IsGrounded ? player.GroundHit.collider : null;
        if (groundCollider != lastGroundCollider)
        {
            lastGroundCollider = groundCollider;
            lastGroundSurface = groundCollider != null ? groundCollider.GetComponentInParent<SurfaceType>() : null;
        }

        Surface surface = lastGroundSurface != null ? lastGroundSurface.Surface : Settings.defaultSurface;
        if (Settings.surfaceFootsteps != null)
        {
            foreach (SurfaceFootsteps surfaceFootsteps in Settings.surfaceFootsteps)
            {
                if (surfaceFootsteps.surface == surface && surfaceFootsteps.clips != null && surfaceFootsteps.clips.Length > 0)
                {
                    return surfaceFootsteps.clips;
                }
            }
        }
        return Settings.defaultFootsteps;
    }

    // Random clip that is never the same as the previous one, so the steps don't sound mechanical
    private AudioClip PickRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && clips[index] == lastPlayedClip)
        {
            index = (index + 1) % clips.Length;
        }

        lastPlayedClip = clips[index];
        return lastPlayedClip;
    }

    // A looping 2D source on a child object, so the rain never cuts the other sounds of the player
    private void CreateRainSource()
    {
        GameObject rainObject = new GameObject("RainAudio");
        rainObject.transform.SetParent(transform, false);

        rainSource = rainObject.AddComponent<AudioSource>();
        rainSource.loop = true;
        rainSource.playOnAwake = false;
        rainSource.spatialBlend = 0f;
        rainSource.volume = 0f;
        rainSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

        rainFilter = rainObject.AddComponent<AudioLowPassFilter>();
    }

    // Moves smoothly between outdoor and sheltered rain
    private void UpdateShelter()
    {
        float target = IsSheltered() ? 1f : 0f;
        float step = Settings.rainFadeTime > 0f ? Time.deltaTime / Settings.rainFadeTime : 1f;
        shelterBlend = Mathf.MoveTowards(shelterBlend, target, step);
        ApplyShelterBlend();
    }

    private void ApplyShelterBlend()
    {
        ApplyRainBlend();
        ApplyInteriorReverb();
    }

    // On the player AudioSource object, so it only affects the player sounds, not the rain which comes from outside
    private void CreateInteriorReverb()
    {
        interiorReverb = gameObject.AddComponent<AudioReverbFilter>();
        interiorReverb.reverbPreset = AudioReverbPreset.User;
        interiorReverb.room = -10000f;
    }

    // The reverb shape stays the same, only its level goes from none (outside) to the settings (inside)
    private void ApplyInteriorReverb()
    {
        interiorReverb.room = Mathf.Lerp(-10000f, Settings.reverbRoom, shelterBlend);
        interiorReverb.roomHF = Settings.reverbRoomHF;
        interiorReverb.decayTime = Settings.reverbDecayTime;
        interiorReverb.decayHFRatio = Settings.reverbDecayHFRatio;
        interiorReverb.reflectionsLevel = Mathf.Lerp(-10000f, Settings.reverbReflectionsLevel, shelterBlend);
        interiorReverb.reflectionsDelay = Settings.reverbReflectionsDelay;
        interiorReverb.reverbLevel = Mathf.Lerp(-10000f, Settings.reverbLevel, shelterBlend);
        interiorReverb.reverbDelay = Settings.reverbDelay;
    }

    // Volume and low-pass cutoff between the outdoor and the sheltered rain
    private void ApplyRainBlend()
    {
        rainSource.volume = Mathf.Lerp(Settings.rainVolume, Settings.shelteredRainVolume, shelterBlend);

        // The cutoff is blended on a logarithmic scale, the ear perceives frequencies that way
        float outdoorLog = Mathf.Log(Mathf.Max(10f, Settings.outdoorRainCutoff));
        float shelteredLog = Mathf.Log(Mathf.Max(10f, Settings.shelteredRainCutoff));
        rainFilter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(outdoorLog, shelteredLog, shelterBlend));
    }

    // True when the head of the player is inside an InteriorVolume, the same volumes that stop the rain particles
    private bool IsSheltered()
    {
        Vector3 head = player.PlayerCamera != null ? player.PlayerCamera.transform.position : transform.position;
        return InteriorVolume.IsInsideAny(head);
    }
}
