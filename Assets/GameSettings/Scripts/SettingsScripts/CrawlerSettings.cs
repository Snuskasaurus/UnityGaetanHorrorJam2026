using UnityEngine;

// Tuning of the crawlers, used by CrawlerManager
[CreateAssetMenu(fileName = "CrawlerSettings", menuName = "Game Settings/Crawler Settings")]
public class CrawlerSettings : ScriptableObject
{
    [Header("Spawn")]
    public int crawlerCount = 20000;
    public float spawnRadius = 36.0f;
    public float despawnRadius = 200.0f;

    [Header("Wandering")]
    public float turnSpeed = 360f;
    public float wanderRadius = 5f;
    public float arriveThreshold = 0.05f;
    public float minWalkSpeed = 1.5f;
    public float maxWalkSpeed = 1.8f;

    [Header("Fleeing")]
    public FloatRange fleeingRadiusStatic = new FloatRange(1.0f, 1.0f);
    public FloatRange fleeingRadiusWalking = new FloatRange(4.0f, 4.0f);
    public FloatRange fleeingRadiusRunning = new FloatRange(10.0f, 10.0f);

    public float fleeingDistance = 10.0f;
    public float minFleeingSpeed = 2.8f;
    public float maxFleeingSpeed = 3.1f;
    public float fleeingRadiusTransitionDuration = 0.4f;
    public float fleeingCooldown = 1.0f;

    [Header("Graphic")]
    public float scaleFactor = 0.1f;
}
