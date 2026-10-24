using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public enum MonsterBreathing
{
    None,
    Calm,
    Heavy,
}

/// <summary>
/// Every sound the monster makes. Creates its own 3D AudioSources (voice, breathing, footsteps)
/// on child objects if none are assigned. Assign the clips in the inspector; empty slots are simply silent.
/// </summary>
[DisallowMultipleComponent]
public class MonsterAudio : MonoBehaviour
{
    [Header("Audio sources (created automatically if left empty)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("3D settings (only applied to auto-created sources)")]
    [SerializeField] private float headHeight = 1.7f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 30.0f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Voice")]
    [SerializeField] private AudioClip[] spotPlayerClips;    // Starts chasing the player
    [SerializeField] private AudioClip[] lostPlayerClips;    // Lost the player, goes to his last known position
    [SerializeField] private AudioClip[] soundHeardClips;    // Heard a sound, goes to investigate
    [SerializeField] private AudioClip[] sniffClips;         // Stops and waits
    [SerializeField] private AudioClip[] fearedClips;        // Scared away by the player
    [SerializeField] private AudioClip[] attackClips;        // Catches the player
    [SerializeField] private AudioClip[] ambientGrowlClips;  // Random growls while roaming
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 1.0f;
    [SerializeField, Range(0f, 0.5f)] private float voicePitchVariation = 0.08f;

    [Header("Ambient growls")]
    [SerializeField] private float minTimeBetweenGrowls = 8.0f;
    [SerializeField] private float maxTimeBetweenGrowls = 20.0f;

    [Header("Breathing (looped)")]
    [SerializeField] private AudioClip calmBreathingClip;
    [SerializeField] private AudioClip heavyBreathingClip;
    [SerializeField, Range(0f, 1f)] private float breathingVolume = 0.6f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] walkFootstepClips;
    [SerializeField] private AudioClip[] runFootstepClips;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.8f;
    [SerializeField, Range(0f, 0.5f)] private float footstepPitchVariation = 0.1f;
    [SerializeField] private float runSpeedThreshold = 3.0f;
    [SerializeField] private float walkStrideLength = 1.2f;
    [SerializeField] private float runStrideLength = 2.0f;
    [SerializeField] private float minSpeedForFootsteps = 0.2f;

    private bool ambientGrowlsEnabled;
    private float nextGrowlDate;
    private float distanceSinceLastStep;

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PUBLIC FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    public void PlaySpotPlayer()  => PlayVoice(spotPlayerClips);
    public void PlayLostPlayer()  => PlayVoice(lostPlayerClips);
    public void PlaySoundHeard()  => PlayVoice(soundHeardClips);
    public void PlaySniff()       => PlayVoice(sniffClips);
    public void PlayFeared()      => PlayVoice(fearedClips);
    public void PlayAttack()      => PlayVoice(attackClips);

    // Plays a single clip on the voice source, interrupting the current voice line
    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume;
        voiceSource.pitch = 1.0f + Random.Range(-voicePitchVariation, voicePitchVariation);
        voiceSource.Play();

        // Don't let an ambient growl step on top of a reaction
        ScheduleNextGrowl();
    }

    public void SetBreathing(MonsterBreathing breathing)
    {
        AudioClip clip = null;
        switch (breathing)
        {
            case MonsterBreathing.Calm:  clip = calmBreathingClip;  break;
            case MonsterBreathing.Heavy: clip = heavyBreathingClip; break;
        }

        if (clip == null)
        {
            breathingSource.Stop();
            breathingSource.clip = null;
            return;
        }

        if (breathingSource.clip == clip && breathingSource.isPlaying)
            return;

        breathingSource.clip = clip;
        breathingSource.volume = breathingVolume;
        breathingSource.time = Random.Range(0f, clip.length); // Avoid always starting on the same breath
        breathingSource.Play();
    }

    public void SetAmbientGrowls(bool enabled)
    {
        if (enabled && !ambientGrowlsEnabled)
        {
            ScheduleNextGrowl();
        }
        ambientGrowlsEnabled = enabled;
    }

    // Called every frame by MonsterController with the agent's real speed: one step per stride length travelled
    public void UpdateFootsteps(float speed, float deltaTime)
    {
        if (speed < minSpeedForFootsteps)
        {
            // Half a stride so the first step comes quickly when the monster starts moving again
            distanceSinceLastStep = walkStrideLength * 0.5f;
            return;
        }

        bool isRunning = speed >= runSpeedThreshold;
        float strideLength = isRunning ? runStrideLength : walkStrideLength;

        distanceSinceLastStep += speed * deltaTime;
        if (distanceSinceLastStep >= strideLength)
        {
            distanceSinceLastStep = 0.0f;
            AudioClip[] clips = isRunning && runFootstepClips != null && runFootstepClips.Length > 0 ? runFootstepClips : walkFootstepClips;
            PlayOneShotRandom(footstepSource, clips, footstepVolume, footstepPitchVariation);
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  UNITY FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void Awake()
    {
        voiceSource     = CreateSource_IFN(voiceSource,     "Audio_Voice",     false, headHeight);
        breathingSource = CreateSource_IFN(breathingSource, "Audio_Breathing", true,  headHeight);
        footstepSource  = CreateSource_IFN(footstepSource,  "Audio_Footsteps", false, 0.0f);
    }

    private void Update()
    {
        if (ambientGrowlsEnabled && Time.time >= nextGrowlDate)
        {
            if (!voiceSource.isPlaying)
            {
                PlayVoice(ambientGrowlClips);
            }
            ScheduleNextGrowl();
        }
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PRIVATE FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void PlayVoice(AudioClip[] clips)
    {
        PlayVoice(GetRandomClip(clips));
    }

    private void ScheduleNextGrowl()
    {
        nextGrowlDate = Time.time + Random.Range(minTimeBetweenGrowls, maxTimeBetweenGrowls);
    }

    private static void PlayOneShotRandom(AudioSource source, AudioClip[] clips, float volume, float pitchVariation)
    {
        AudioClip clip = GetRandomClip(clips);
        if (clip == null)
            return;

        source.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }

    private static AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    private AudioSource CreateSource_IFN(AudioSource source, string sourceName, bool loop, float height)
    {
        if (source == null)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.up * height;

            source = child.AddComponent<AudioSource>();
            source.spatialBlend = 1.0f;
            source.rolloffMode = rolloffMode;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0.0f;
            source.outputAudioMixerGroup = outputMixerGroup;
        }

        source.playOnAwake = false;
        source.loop = loop;
        return source;
    }
}
