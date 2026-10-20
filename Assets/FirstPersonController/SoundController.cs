using UnityEngine;
using UnityEngine.Audio;

// Plays every sound of the player on its AudioSource: footsteps, landing, and the sounds wired from the Inspector.
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

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        player = GetComponent<PlayerController>();
        movement = GetComponent<MovementController>();
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
}
