using UnityEngine;

// Sounds of the crawler swarm around the player, driven by the zippo light only (CrawlerManager is never touched):
// - a crawling loop, louder in the dark, placed in a direction that drifts around the player and lowering the rain,
// - a second crawling layer only heard in the dark, on the other side of the player,
// - a scurry when the light suddenly grows (the swarm runs away),
// - clicks at random positions around the player, closer and more frequent in the dark.
// It only plays in a scene that has a CrawlerManager.
[RequireComponent(typeof(ZippoController), typeof(AudioSource))]
public class CrawlerSoundController : MonoBehaviour
{
    private const int ChitterSourceCount = 4;

    // Tuning values, read from the sound settings of GameSettings
    private SoundSettings Settings => GameSettings.Sound;

    // References
    private ZippoController zippo;
    private AudioSource playerAudio;
    private SoundController soundController;

    // Internal variables
    private AudioSource swarmSource;
    private AudioSource darkSwarmSource;
    private AudioSource scurrySource;
    private readonly AudioSource[] chitterSources = new AudioSource[ChitterSourceCount];
    private int nextChitterSource = 0;
    private float smoothedLight = 0f;
    private float lastLight = 0f;
    private float nextChitterTime = 0f;
    private AudioClip lastChitterClip;

    private void Awake()
    {
        zippo = GetComponent<ZippoController>();
        playerAudio = GetComponent<AudioSource>();
        soundController = GetComponent<SoundController>();
    }

    private void Start()
    {
        // Read only check: the swarm is only heard where there are crawlers
        if (FindAnyObjectByType<CrawlerManager>() == null)
        {
            enabled = false;
            return;
        }

        CreateSources();

        smoothedLight = GetLight();
        lastLight = smoothedLight;
        if (Settings.crawlerSwarmLoop != null)
        {
            swarmSource.clip = Settings.crawlerSwarmLoop;
            UpdateSwarm();
            swarmSource.Play();
        }
        if (Settings.crawlerDarkSwarmLoop != null)
        {
            darkSwarmSource.clip = Settings.crawlerDarkSwarmLoop;
            UpdateSwarm();
            darkSwarmSource.Play();
        }
        ScheduleNextChitter();
    }

    private void Update()
    {
        float light = GetLight();

        // A sudden gain of light makes the swarm run away
        if (light - lastLight >= Settings.scurryLightIncrease)
        {
            PlayScurry(light - lastLight);
        }
        lastLight = light;

        smoothedLight = Mathf.Lerp(smoothedLight, light, 1f - Mathf.Exp(-Settings.crawlerLightSmoothing * Time.deltaTime));
        if (swarmSource.isPlaying || darkSwarmSource.isPlaying)
        {
            UpdateSwarm();
        }

        if (Time.time >= nextChitterTime)
        {
            PlayChitter();
            ScheduleNextChitter();
        }
    }

    // Volume from the light with a slow breathing, position drifting around the player, rain lowered in the dark
    private void UpdateSwarm()
    {
        float swell = Mathf.Lerp(1f - Settings.swarmSwellDepth, 1f, Mathf.PerlinNoise(Time.time * Settings.swarmSwellSpeed, 0.5f));
        swarmSource.volume = Settings.swarmVolumeByLight.Evaluate(smoothedLight) * swell;

        float angle = Mathf.PerlinNoise(Time.time * Settings.swarmDriftSpeed, 3.7f) * Mathf.PI * 4f;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        swarmSource.transform.position = transform.position + direction * Settings.swarmDistance;
        swarmSource.spatialBlend = Settings.swarmSpatialBlend;

        // The dark layer breathes on its own rhythm and comes from the opposite side, so the player feels surrounded
        float darkSwell = Mathf.Lerp(1f - Settings.swarmSwellDepth, 1f, Mathf.PerlinNoise(Time.time * Settings.swarmSwellSpeed, 7.3f));
        darkSwarmSource.volume = Settings.darkSwarmVolumeByLight.Evaluate(smoothedLight) * darkSwell;
        darkSwarmSource.transform.position = transform.position - direction * Settings.swarmDistance;
        darkSwarmSource.spatialBlend = Settings.swarmSpatialBlend;

        if (soundController != null)
        {
            soundController.RainVolumeMultiplier = Mathf.Lerp(Settings.rainDuckInDark, 1f, smoothedLight);
        }
    }

    // 0 when the zippo is off, the flame power when it is lit
    private float GetLight()
    {
        return zippo.IsZippoLit ? Mathf.Clamp01(zippo.flamePower) : 0f;
    }

    private void PlayScurry(float lightGain)
    {
        AudioClip clip = PickRandom(Settings.crawlerScurrySounds, null);
        if (clip != null)
        {
            scurrySource.PlayOneShot(clip, Settings.scurryVolume * Mathf.Clamp01(lightGain * 2f));
        }
    }

    // A click somewhere around the player, closer in the dark
    private void PlayChitter()
    {
        AudioClip clip = PickRandom(Settings.crawlerChitterSounds, lastChitterClip);
        if (clip == null)
        {
            return;
        }
        lastChitterClip = clip;

        float distance = Settings.chitterDistanceByLight.Evaluate(smoothedLight);
        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector3 position = transform.position + new Vector3(direction.x, 0f, direction.y) * distance;

        AudioSource source = chitterSources[nextChitterSource];
        nextChitterSource = (nextChitterSource + 1) % ChitterSourceCount;
        source.transform.position = position;
        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(clip, Settings.chitterVolume);
    }

    private void ScheduleNextChitter()
    {
        float interval = Mathf.Max(0.1f, Settings.chitterIntervalByLight.Evaluate(smoothedLight));
        nextChitterTime = Time.time + interval * Random.Range(0.6f, 1.4f);
    }

    private static AudioClip PickRandom(AudioClip[] clips, AudioClip avoid)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && clips[index] == avoid)
        {
            index = (index + 1) % clips.Length;
        }
        return clips[index];
    }

    private void CreateSources()
    {
        // The swarm is placed around the player every frame, the scurry is all around: 2D
        swarmSource = CreateSource("CrawlerSwarmAudio", transform, Settings.swarmSpatialBlend);
        swarmSource.loop = true;
        swarmSource.minDistance = 2f;
        swarmSource.maxDistance = 20f;
        swarmSource.rolloffMode = AudioRolloffMode.Linear;

        darkSwarmSource = CreateSource("CrawlerDarkSwarmAudio", transform, Settings.swarmSpatialBlend);
        darkSwarmSource.loop = true;
        darkSwarmSource.minDistance = 2f;
        darkSwarmSource.maxDistance = 20f;
        darkSwarmSource.rolloffMode = AudioRolloffMode.Linear;

        scurrySource = CreateSource("CrawlerScurryAudio", transform, 0f);

        // The clicks come from precise places: 3D sources moved in the world, not attached to the player
        for (int i = 0; i < ChitterSourceCount; i++)
        {
            chitterSources[i] = CreateSource("CrawlerChitterAudio", null, 1f);
            chitterSources[i].minDistance = 1f;
            chitterSources[i].maxDistance = 15f;
            chitterSources[i].rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private AudioSource CreateSource(string objectName, Transform parent, float spatialBlend)
    {
        GameObject sourceObject = new GameObject(objectName);
        if (parent != null)
        {
            sourceObject.transform.SetParent(parent, false);
        }

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.outputAudioMixerGroup = playerAudio.outputAudioMixerGroup;
        return source;
    }

    private void OnDisable()
    {
        // Gives the rain its full volume back when the swarm stops
        if (soundController != null)
        {
            soundController.RainVolumeMultiplier = 1f;
        }
    }

    private void OnDestroy()
    {
        // The click sources are not children of the player, they are cleaned with it
        foreach (AudioSource source in chitterSources)
        {
            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }
    }
}
