using UnityEngine;

// Splashes of the rain: a small particle system created at start and played as a collision sub-emitter
// of the rain, so every drop that hits the ground or a roof bursts into a few droplets.
// The rain itself must have its Collision module in World mode with a Lifetime Loss of 1.
[RequireComponent(typeof(ParticleSystem))]
public class RainSplashes : MonoBehaviour
{
    [Header("Splash")]
    [SerializeField]
    [Range(0f, 1f)]
    private float splashProbability = 0.5f;  // part of the drops that make a splash, lower = cheaper

    [SerializeField]
    private int dropletsPerSplash = 3;

    [SerializeField]
    private Vector2 dropletLifetime = new Vector2(0.15f, 0.3f);  // seconds, min and max

    [SerializeField]
    private Vector2 dropletSize = new Vector2(0.015f, 0.04f);  // meters, min and max

    [SerializeField]
    private float upwardSpeed = 1.5f;  // m/s, how high the droplets jump

    [SerializeField]
    private float sidewaysSpeed = 0.8f;  // m/s, how far the droplets spread

    [SerializeField]
    private int maxDroplets = 1000;

    private void Awake()
    {
        ParticleSystem rain = GetComponent<ParticleSystem>();
        ParticleSystem splash = CreateSplashSystem(rain);

        ParticleSystem.SubEmittersModule subEmitters = rain.subEmitters;
        subEmitters.enabled = true;
        // Removes the empty entry the prefab may have, then adds the splash on collision
        while (subEmitters.subEmittersCount > 0)
        {
            subEmitters.RemoveSubEmitter(0);
        }
        subEmitters.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, splashProbability);
    }

    private ParticleSystem CreateSplashSystem(ParticleSystem rain)
    {
        GameObject splashObject = new GameObject("RainSplash");
        splashObject.transform.SetParent(transform, false);

        ParticleSystem splash = splashObject.AddComponent<ParticleSystem>();
        // A new particle system starts playing, it must be stopped before changing its duration
        splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = splash.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(dropletLifetime.x, dropletLifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(dropletSize.x, dropletSize.y);
        main.startColor = rain.main.startColor;
        main.gravityModifier = 1f;
        main.maxParticles = maxDroplets;

        // Every splash emits a small burst, the sub-emitter triggers it at the collision point
        ParticleSystem.EmissionModule emission = splash.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)dropletsPerSplash) });

        ParticleSystem.ShapeModule shape = splash.shape;
        shape.enabled = false;

        // Droplets jump up and spread around, gravity brings them back down
        ParticleSystem.VelocityOverLifetimeModule velocity = splash.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-sidewaysSpeed, sidewaysSpeed);
        velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.5f, upwardSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(-sidewaysSpeed, sidewaysSpeed);

        // Same look as the rain, but as small round droplets instead of stretched streaks
        ParticleSystemRenderer rainRenderer = rain.GetComponent<ParticleSystemRenderer>();
        ParticleSystemRenderer splashRenderer = splash.GetComponent<ParticleSystemRenderer>();
        splashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        splashRenderer.sharedMaterial = rainRenderer.sharedMaterial;

        return splash;
    }
}
