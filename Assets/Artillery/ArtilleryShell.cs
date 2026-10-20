using System.Collections;
using UnityEngine;

// One artillery shell: falls with a whistle and a glowing trail, then explodes with a flash, fire, dirt and smoke,
// and shakes the camera of the player if he is close. Spawned by ArtillerySpawner, the effects are built by code.
public class ArtilleryShell : MonoBehaviour
{
    // Tuning values, read from the artillery settings of GameSettings
    private ArtillerySettings Settings => GameSettings.Artillery;

    // Internal variables
    private Vector3 startPoint;
    private Vector3 impactPoint;
    private float fallTime;
    private float elapsed;
    private bool hasExploded;
    private AudioSource whistleSource;
    private TrailRenderer trail;

    // Starts the fall towards the impact point
    public void Launch(Vector3 impact)
    {
        impactPoint = impact;

        // The shell comes slightly from the side, along a random direction
        Vector2 side = Random.insideUnitCircle.normalized;
        float sideDistance = Settings.fallHeight * Mathf.Tan(Settings.fallAngle * Mathf.Deg2Rad);
        startPoint = impact + Vector3.up * Settings.fallHeight + new Vector3(side.x, 0f, side.y) * sideDistance;
        fallTime = Vector3.Distance(startPoint, impactPoint) / Mathf.Max(1f, Settings.fallSpeed);
        transform.position = startPoint;

        CreateTrail();
        AudioClip whistle = PickRandom(Settings.whistleSounds);
        if (whistle != null)
        {
            whistleSource = CreateAudioSource(gameObject, 5f, 200f);
            whistleSource.clip = whistle;
            // The whistle is timed to end right on the impact, like a real incoming shell
            whistleSource.PlayDelayed(Mathf.Max(0f, fallTime - whistle.length));
        }
    }

    private void Update()
    {
        if (hasExploded)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float t = fallTime > 0f ? Mathf.Clamp01(elapsed / fallTime) : 1f;
        transform.position = Vector3.Lerp(startPoint, impactPoint, t);

        if (t >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;
        transform.position = impactPoint;

        if (whistleSource != null)
        {
            whistleSource.Stop();
        }
        if (trail != null)
        {
            trail.emitting = false;
        }

        AudioClip explosion = PickRandom(Settings.explosionSounds);
        if (explosion != null)
        {
            AudioSource source = CreateAudioSource(gameObject, 10f, Settings.explosionMaxDistance);
            source.pitch = Random.Range(0.85f, 1.1f);
            source.PlayOneShot(explosion, Settings.explosionVolume);
        }

        StartCoroutine(Flash());
        CreateFire();
        CreateDirt();
        CreateSmoke();
        ShakePlayer();

        if (Settings.monsterLoudness > 0f)
        {
            MonsterManager.SendSoundToMonsterHearing(impactPoint, Settings.monsterLoudness);
        }

        Destroy(gameObject, Settings.smokeLifetime + 2f);
    }

    // The closer the player, the stronger the shake (squared falloff)
    private void ShakePlayer()
    {
        CameraController cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController == null)
        {
            return;
        }

        float distance = Vector3.Distance(cameraController.transform.position, impactPoint);
        float closeness = Mathf.Clamp01(1f - distance / Settings.shakeRadius);
        if (closeness > 0f)
        {
            cameraController.ShakeCamera(Settings.shakeDuration, Settings.maxShakeMagnitude * closeness * closeness);
        }
    }

    // A strong light that dies fast, with a little flicker
    private IEnumerator Flash()
    {
        GameObject flashObject = new GameObject("ExplosionFlash");
        flashObject.transform.SetParent(transform, false);
        flashObject.transform.localPosition = Vector3.up * 1.5f;

        Light flash = flashObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = Settings.flashColor;
        flash.range = Settings.flashRange;
        flash.shadows = LightShadows.None;

        float time = 0f;
        while (time < Settings.flashDuration)
        {
            float fade = 1f - time / Settings.flashDuration;
            float flicker = Random.Range(0.8f, 1.1f);
            flash.intensity = Settings.flashIntensity * fade * fade * flicker;
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(flashObject);
    }

    private void CreateFire()
    {
        ParticleSystem fire = CreateParticleSystem("ExplosionFire", 0.5f);
        ParticleSystem.MainModule main = fire.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startColor = Settings.fireColor;
        main.gravityModifier = -0.2f;

        ParticleSystem.ShapeModule shape = fire.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.5f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        SetBurst(fire, 25);
        FadeOut(fire);
        fire.Play();
    }

    private void CreateDirt()
    {
        ParticleSystem dirt = CreateParticleSystem("ExplosionDirt", 0.5f);
        ParticleSystem.MainModule main = dirt.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = Settings.dirtColor;
        main.gravityModifier = 1.5f;

        ParticleSystem.ShapeModule shape = dirt.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        SetBurst(dirt, 40);
        dirt.Play();
    }

    // Big slow clouds that rise, grow and fade
    private void CreateSmoke()
    {
        ParticleSystem smoke = CreateParticleSystem("ExplosionSmoke", 1f);
        ParticleSystem.MainModule main = smoke.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(Settings.smokeLifetime * 0.6f, Settings.smokeLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Settings.smokeColor;
        main.gravityModifier = -0.05f;

        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.rateOverTime = 12f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)20) });

        ParticleSystem.SizeOverLifetimeModule size = smoke.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));

        ParticleSystem.LimitVelocityOverLifetimeModule drag = smoke.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 1f;

        FadeOut(smoke);
        smoke.Play();
    }

    // Small glowing streak behind the falling shell, so it can be spotted in the night sky
    private void CreateTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.sharedMaterial = Settings.particleMaterial;
        trail.startColor = Settings.fireColor;
        trail.endColor = new Color(Settings.fireColor.r, Settings.fireColor.g, Settings.fireColor.b, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private ParticleSystem CreateParticleSystem(string objectName, float duration)
    {
        GameObject effect = new GameObject(objectName);
        effect.transform.SetParent(transform, false);

        ParticleSystem system = effect.AddComponent<ParticleSystem>();
        // A new particle system starts playing, it must be stopped before changing its duration
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Settings.particleMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return system;
    }

    private static void SetBurst(ParticleSystem system, int count)
    {
        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    // Particles keep their color then fade to transparent at the end of their life
    private static void FadeOut(ParticleSystem system)
    {
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }

    private static AudioSource CreateAudioSource(GameObject owner, float minDistance, float maxDistance)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0.5f;
        return source;
    }

    private static AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }
        return clips[Random.Range(0, clips.Length)];
    }
}
