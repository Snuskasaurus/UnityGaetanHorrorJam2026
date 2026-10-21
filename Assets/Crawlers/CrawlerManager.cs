using System.Collections.Generic;
using DataStructures.ViliWonka.KDTree;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class CrawlerManager : MonoBehaviour
{
    private static CrawlerManager INSTANCE;

    // Exits registered before this manager's Awake (script execution order) are buffered here instead of crashing.
    private static readonly List<Vector3> s_PendingExitPositions = new List<Vector3>();

    const int MAX_INSTANCE_COUNT_BY_GPU_CALL = 1023;
    const int JOB_BATCH_SIZE = 64;
    const float TWO_PI = 6.28318530718f;

    const int SPAWN_ATTEMPTS = 4;
    const int WANDER_ATTEMPTS = 3;
    const int ZONE_ESCAPE_ATTEMPTS = 3;

    // Per-crawler flags written by the job, read on the main thread next frame
    const byte FLAG_ARRIVED = 1 << 0;
    const byte FLAG_FAR = 1 << 1;
    const byte FLAG_FLEE_CANDIDATE = 1 << 2;
    const byte FLAG_IN_ZONE = 1 << 3; // inside a safe zone AND its target is inside one too -> needs to escape

    struct ZoneSnapshot
    {
        public Vector3 Center;
        public float Radius;
        public float FleeMargin;
    }

    [Header("Graphic")]
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    [Header("Performance")]
    [Tooltip("How often fleeing / respawn decisions are made (seconds). 0.02 = same rate as the old FixedUpdate.")]
    [SerializeField, Min(0f)] float logicTickInterval = 0.02f;
    [Tooltip("Max far crawlers re-spawned per logic tick (each respawn is a NavMesh query). Avoids spikes after a teleport.")]
    [SerializeField, Min(1)] int maxRespawnsPerTick = 256;

    private CrawlerSettings Settings;

    // References
    PlayerController playerController;
    ZippoController zippoController;

    int crawlerCount;
    int drawCallCount;
    RenderParams renderParams;

    // Crawler state (structure of arrays, persistent native memory shared with the Burst job)
    NativeArray<float3> crawlerPositions;
    NativeArray<float3> crawlerTargets;
    NativeArray<float> crawlerSpeeds;
    NativeArray<float> crawlerYaws;          // radians, rotation is yaw-only
    NativeArray<float> crawlerNextFleeTimes; // Time.time at which the crawler may flee again
    NativeArray<Matrix4x4> crawlerMatrices;
    NativeArray<byte> crawlerFlags;

    JobHandle updateJobHandle;
    bool updateJobScheduled;

    float fleeingRadius;
    float fleeingViewDot;
    float logicTimer;
    int respawnCursor;
    Vector3 framePlayerPosition;

    // Safe zones (snapshot taken once per frame)
    readonly List<ZoneSnapshot> zoneSnapshots = new List<ZoneSnapshot>();
    NativeArray<float4> zoneData; // (centerX, centerZ, radius², 0) for the job
    int zoneCount;

    // Exits
    readonly List<Vector3> crawlerExitPositions = new List<Vector3>();
    KDTree exitKDTree;
    readonly KDQuery exitKDTreeQuery = new KDQuery();
    readonly List<int> exitKDTreeResults = new List<int>();
    bool exitKDTreeDirty;
    bool exitQueryValid;

    bool loggedSpawnFailure;
    bool loggedWanderFailure;

    static readonly ProfilerMarker ProfilerMarker_ProcessJobResults = new ProfilerMarker("Crawlers.ProcessJobResults");
    static readonly ProfilerMarker ProfilerMarker_CompleteJob = new ProfilerMarker("Crawlers.CompleteJob");
    static readonly ProfilerMarker ProfilerMarker_HandleDrawCalls = new ProfilerMarker("Crawlers.HandleDrawCalls");
    static readonly ProfilerMarker ProfilerMarker_QueryExitKDTree = new ProfilerMarker("Crawlers.QueryExitKDTree");

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PUBLIC FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    public static void RegisterCrawlerExitPosition(Vector3 position)
    {
        if (INSTANCE != null)
        {
            INSTANCE.crawlerExitPositions.Add(position);
            INSTANCE.exitKDTreeDirty = true;
        }
        else
        {
            s_PendingExitPositions.Add(position);
        }
    }

    // Keeps statics clean when "Enter Play Mode Options" disables domain reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        INSTANCE = null;
        s_PendingExitPositions.Clear();
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  UNITY FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void Awake()
    {
        INSTANCE = this;
        crawlerExitPositions.AddRange(s_PendingExitPositions);
        s_PendingExitPositions.Clear();
        exitKDTreeDirty = true;

        Settings = GameSettings.Crawler;
        crawlerCount = Mathf.Max(0, Settings.crawlerCount);
        fleeingViewDot = math.cos(math.radians(Settings.fleeingViewAngle * 0.5f));

        playerController = Common.FindTypeInScene<PlayerController>();
        zippoController = Common.FindTypeInScene<ZippoController>();

        if (playerController == null || zippoController == null || mesh == null || material == null)
        {
            Debug.LogError("CrawlerManager: missing PlayerController, ZippoController, mesh or material. Disabling.", this);
            enabled = false;
            return;
        }

        if (Settings.spawnRadius >= Settings.despawnRadius)
        {
            Debug.LogWarning("CrawlerManager: spawnRadius >= despawnRadius, crawlers can spawn outside the despawn radius " +
                             "and get respawned over and over.", this);
        }

        if (!material.enableInstancing)
        {
            Debug.LogWarning("CrawlerManager: GPU instancing was disabled on the crawler material, enabling it.", material);
            material.enableInstancing = true;
        }

        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };

        crawlerPositions = new NativeArray<float3>(crawlerCount, Allocator.Persistent);
        crawlerTargets = new NativeArray<float3>(crawlerCount, Allocator.Persistent);
        crawlerSpeeds = new NativeArray<float>(crawlerCount, Allocator.Persistent);
        crawlerYaws = new NativeArray<float>(crawlerCount, Allocator.Persistent);
        crawlerNextFleeTimes = new NativeArray<float>(crawlerCount, Allocator.Persistent);
        crawlerMatrices = new NativeArray<Matrix4x4>(crawlerCount, Allocator.Persistent);
        crawlerFlags = new NativeArray<byte>(crawlerCount, Allocator.Persistent);
        zoneData = new NativeArray<float4>(8, Allocator.Persistent);

        Vector3 playerPosition = playerController.transform.position;
        for (int i = 0; i < crawlerCount; i++)
        {
            ParkCrawler(i, playerPosition);
        }

        drawCallCount = (crawlerCount + MAX_INSTANCE_COUNT_BY_GPU_CALL - 1) / MAX_INSTANCE_COUNT_BY_GPU_CALL;
        Debug.Log("There will be " + drawCallCount + " draw call(s) for crawlers each frame");
    }

    private void Start()
    {
        // Initial spawn is done in Start so every safe zone in the scene has had its OnEnable
        GatherZones();

        Vector3 playerPosition = playerController.transform.position;
        for (int i = 0; i < crawlerCount; i++)
        {
            InitializeCrawler(i, playerPosition);
        }
    }

    private void Update()
    {
        if (crawlerCount == 0)
            return;

        // Safety net, normally already completed in LateUpdate
        CompleteUpdateJob();

        float dt = Time.deltaTime;
        float now = Time.time;
        Transform playerTransform = playerController.transform;
        Vector3 playerPosition = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;
        framePlayerPosition = playerPosition;

        GatherZones();
        UpdateFleeingRadius(dt);

        bool logicTick = false;
        logicTimer += dt;
        if (logicTimer >= logicTickInterval)
        {
            logicTick = true;
            // No catch-up spiral: at most one logic tick per frame
            logicTimer = Mathf.Min(logicTimer - logicTickInterval, logicTickInterval);
        }

        ProcessJobResults(playerPosition, now, logicTick);
        ScheduleUpdateJob(playerPosition, playerForward, dt, now);
    }

    private void LateUpdate()
    {
        if (!updateJobScheduled)
            return;

        CompleteUpdateJob();
        HandleDrawCalls();
    }

    private void OnDisable()
    {
        CompleteUpdateJob();
    }

    private void OnDestroy()
    {
        CompleteUpdateJob();

        if (crawlerPositions.IsCreated) crawlerPositions.Dispose();
        if (crawlerTargets.IsCreated) crawlerTargets.Dispose();
        if (crawlerSpeeds.IsCreated) crawlerSpeeds.Dispose();
        if (crawlerYaws.IsCreated) crawlerYaws.Dispose();
        if (crawlerNextFleeTimes.IsCreated) crawlerNextFleeTimes.Dispose();
        if (crawlerMatrices.IsCreated) crawlerMatrices.Dispose();
        if (crawlerFlags.IsCreated) crawlerFlags.Dispose();
        if (zoneData.IsCreated) zoneData.Dispose();

        if (INSTANCE == this)
            INSTANCE = null;
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PRIVATE FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void CompleteUpdateJob()
    {
        if (!updateJobScheduled)
            return;

        using var marker = ProfilerMarker_CompleteJob.Auto();
        updateJobHandle.Complete();
        updateJobScheduled = false;
    }

    private void ScheduleUpdateJob(Vector3 playerPosition, Vector3 playerForward, float dt, float now)
    {
        float2 forwardXZ = new float2(playerForward.x, playerForward.z);
        float forwardLengthSq = math.lengthsq(forwardXZ);
        forwardXZ = forwardLengthSq > 1e-8f ? forwardXZ * math.rsqrt(forwardLengthSq) : new float2(0f, 1f);

        var job = new CrawlerUpdateJob
        {
            Positions = crawlerPositions,
            Yaws = crawlerYaws,
            Targets = crawlerTargets,
            Speeds = crawlerSpeeds,
            NextFleeTimes = crawlerNextFleeTimes,
            Zones = zoneData,
            Matrices = crawlerMatrices,
            Flags = crawlerFlags,

            ZoneCount = zoneCount,
            DeltaTime = dt,
            CurrentTime = now,
            SqrArriveThreshold = Settings.arriveThreshold * Settings.arriveThreshold,
            MaxTurnRadians = math.radians(Settings.turnSpeed) * dt,
            Scale = Settings.scaleFactor,
            PlayerPosition = playerPosition,
            PlayerForwardXZ = forwardXZ,
            SqrDespawnRadius = Settings.despawnRadius * Settings.despawnRadius,
            SqrFleeingRadius = fleeingRadius * fleeingRadius,
            FleeingViewDot = fleeingViewDot,
        };

        updateJobHandle = job.Schedule(crawlerCount, JOB_BATCH_SIZE);
        JobHandle.ScheduleBatchedJobs();
        updateJobScheduled = true;
    }

    // Consumes the flags produced by last frame's job. Only crawlers with a flag cost anything here.
    private void ProcessJobResults(Vector3 playerPosition, float now, bool logicTick)
    {
        using var marker = ProfilerMarker_ProcessJobResults.Auto();

        if (logicTick)
        {
            RebuildExitKDTreeIfNeeded();
            exitQueryValid = false; // exit query is done lazily, at most once per tick
        }

        float sqrFleeingRadius = fleeingRadius * fleeingRadius;
        int respawnsLeft = maxRespawnsPerTick;
        int start = respawnCursor;

        for (int k = 0; k < crawlerCount; k++)
        {
            int crawlerIndex = start + k;
            if (crawlerIndex >= crawlerCount)
                crawlerIndex -= crawlerCount;

            byte flags = crawlerFlags[crawlerIndex];
            if (flags == 0)
                continue;

            if ((flags & FLAG_FAR) != 0)
            {
                if (logicTick && respawnsLeft > 0)
                {
                    if (!InitializeCrawler(crawlerIndex, playerPosition))
                        ParkCrawler(crawlerIndex, playerPosition); // hidden, retried on a later tick

                    if (--respawnsLeft == 0)
                    {
                        // Round-robin so the same indices don't always wait when the budget is exhausted
                        respawnCursor = crawlerIndex + 1 == crawlerCount ? 0 : crawlerIndex + 1;
                    }
                }
                continue;
            }

            // Escaping a safe zone has priority over wandering and fleeing the player
            if ((flags & FLAG_IN_ZONE) != 0)
            {
                if (logicTick)
                {
                    int zoneIndex = FindContainingZone(crawlerPositions[crawlerIndex]);
                    if (zoneIndex >= 0)
                        TryEscapeZone(crawlerIndex, zoneIndex, now);
                }
                continue;
            }

            if ((flags & FLAG_ARRIVED) != 0)
                PickWanderTarget(crawlerIndex);

            // Fleeing overrides the wander target picked above
            if (logicTick && (flags & FLAG_FLEE_CANDIDATE) != 0)
                TryStartFleeing(crawlerIndex, playerPosition, sqrFleeingRadius, now);
        }
    }

    private void HandleDrawCalls()
    {
        using var marker = ProfilerMarker_HandleDrawCalls.Auto();

        // Explicit bounds so Unity doesn't compute them from thousands of matrices every frame.
        // Every live crawler is within despawnRadius of the player.
        float size = Settings.despawnRadius * 2f + 20f;
        renderParams.worldBounds = new Bounds(framePlayerPosition, new Vector3(size, size, size));

        for (int drawCallIndex = 0; drawCallIndex < drawCallCount; drawCallIndex++)
        {
            int startInstanceIndex = drawCallIndex * MAX_INSTANCE_COUNT_BY_GPU_CALL;
            int instanceCount = Mathf.Min(MAX_INSTANCE_COUNT_BY_GPU_CALL, crawlerCount - startInstanceIndex);
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, crawlerMatrices, instanceCount, startInstanceIndex);
        }
    }

    private void SetCrawlerWalkSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(Settings.minWalkSpeed, Settings.maxWalkSpeed);
    }

    private void SetCrawlerFleeSpeed(int crawlerIndex)
    {
        crawlerSpeeds[crawlerIndex] = Random.Range(Settings.minFleeingSpeed, Settings.maxFleeingSpeed);
    }

    // Puts a crawler far below the world: invisible, and outside the despawn radius so it gets respawned later.
    private void ParkCrawler(int crawlerIndex, Vector3 playerPosition)
    {
        Vector3 parkingPosition = playerPosition + Vector3.down * (Settings.despawnRadius * 4f + 10000f);
        crawlerPositions[crawlerIndex] = parkingPosition;
        crawlerTargets[crawlerIndex] = parkingPosition;
        crawlerSpeeds[crawlerIndex] = 0f;
    }

    private bool InitializeCrawler(int crawlerIndex, Vector3 origin)
    {
        bool navMeshFailed = false;

        for (int attempt = 0; attempt < SPAWN_ATTEMPTS; attempt++)
        {
            Vector2 randomPositionInRadius = Random.insideUnitCircle * Settings.spawnRadius;
            Vector3 candidate = origin + new Vector3(randomPositionInRadius.x, 0.0f, randomPositionInRadius.y);

            // Free rejection before paying for a NavMesh query
            if (IsInsideAnyZone(candidate))
                continue;

            // Use a local: never write a failed query result straight into the state arrays
            if (!NavMeshUtil.GetClosestPointInNavMesh_Big(candidate, out Vector3 spawnPosition))
            {
                navMeshFailed = true;
                continue;
            }

            if (IsInsideAnyZone(spawnPosition))
                continue;

            crawlerPositions[crawlerIndex] = spawnPosition;
            crawlerTargets[crawlerIndex] = spawnPosition;
            crawlerYaws[crawlerIndex] = 0f;
            crawlerNextFleeTimes[crawlerIndex] = 0f; // can flee immediately
            SetCrawlerWalkSpeed(crawlerIndex);
            return true;
        }

        if (navMeshFailed && !loggedSpawnFailure)
        {
            Debug.LogWarning("CrawlerManager: failed to find a crawler spawn position (will retry, logged once).", this);
            loggedSpawnFailure = true;
        }
        return false;
    }

    private void PickWanderTarget(int crawlerIndex)
    {
        SetCrawlerWalkSpeed(crawlerIndex);
        Vector3 crawlerPosition = crawlerPositions[crawlerIndex];

        for (int attempt = 0; attempt < WANDER_ATTEMPTS; attempt++)
        {
            if (!NavMeshUtil.PickNewTarget_RandomInCircle(crawlerPosition, Settings.wanderRadius, out Vector3 newTarget))
            {
                // The crawler keeps its target, stays "arrived" and retries next frame
                if (!loggedWanderFailure)
                {
                    Debug.LogWarning("CrawlerManager: failed to find a wander target (will retry, logged once).", this);
                    loggedWanderFailure = true;
                }
                return;
            }

            // Don't walk into or through a safe zone
            if (SegmentCrossesAnyZone(crawlerPosition, newTarget))
                continue;

            crawlerTargets[crawlerIndex] = newTarget;
            return;
        }
        // Every attempt crossed a zone: stay "arrived", retry next frame
    }

    private void UpdateFleeingRadius(float dt)
    {
        float flamePower = zippoController.flamePower;

        float target;
        switch (playerController.CurrentMovementState)
        {
            case MovementState.Walking:
                target = FloatRange.Lerp(Settings.fleeingRadiusWalking, flamePower);
                break;
            case MovementState.Running:
                target = FloatRange.Lerp(Settings.fleeingRadiusRunning, flamePower);
                break;
            default: // Crouching and any other state
                target = FloatRange.Lerp(Settings.fleeingRadiusStatic, flamePower);
                break;
        }

        // Framerate-independent exponential smoothing: ~95% of the way to the target after
        // fleeingRadiusTransitionDuration, and stays smooth when the target changes every frame (flame flicker).
        float duration = Settings.fleeingRadiusTransitionDuration;
        if (duration <= 0f)
        {
            fleeingRadius = target;
        }
        else
        {
            float k = 1f - math.exp(-3f * dt / duration);
            fleeingRadius = math.lerp(fleeingRadius, target, k);
        }
    }

    private void TryStartFleeing(int crawlerIndex, Vector3 playerPosition, float sqrFleeingRadius, float now)
    {
        Vector3 crawlerPosition = crawlerPositions[crawlerIndex];
        Vector3 playerToCrawler = crawlerPosition - playerPosition;

        if (playerToCrawler.sqrMagnitude > sqrFleeingRadius * Random.Range(0.2f, 1.0f))
            return;

        Vector3 away = new Vector3(playerToCrawler.x, 0f, playerToCrawler.z);
        float flatSqr = away.sqrMagnitude;
        if (flatSqr < 1e-6f)
            return;
        away *= 1f / Mathf.Sqrt(flatSqr);

        Vector3 targetCandidate = crawlerPosition + away * Settings.fleeingDistance;
        bool found = NavMeshUtil.GetClosestPointInNavMesh_Small(targetCandidate, out Vector3 newTarget)
                     && !SegmentCrossesAnyZone(crawlerPosition, newTarget);
        if (!found)
            found = TryFindCrawlerExitPosition(crawlerPosition, away, playerPosition, out newTarget);
        if (!found)
            return;

        SetCrawlerFleeSpeed(crawlerIndex);
        crawlerNextFleeTimes[crawlerIndex] = now + Settings.fleeingCooldown;
        crawlerTargets[crawlerIndex] = newTarget;
    }

    // Sends a crawler that is inside a zone to a point just outside it (radially, then random outward directions).
    private bool TryEscapeZone(int crawlerIndex, int zoneIndex, float now)
    {
        ZoneSnapshot zone = zoneSnapshots[zoneIndex];
        Vector3 crawlerPosition = crawlerPositions[crawlerIndex];

        float dx = crawlerPosition.x - zone.Center.x;
        float dz = crawlerPosition.z - zone.Center.z;
        float baseAngle = (dx * dx + dz * dz) > 1e-6f ? Mathf.Atan2(dz, dx) : Random.Range(0f, TWO_PI);
        float escapeDistance = zone.Radius + zone.FleeMargin;

        for (int attempt = 0; attempt < ZONE_ESCAPE_ATTEMPTS; attempt++)
        {
            float angle = attempt == 0 ? baseAngle : baseAngle + Random.Range(-0.5f * Mathf.PI, 0.5f * Mathf.PI);
            Vector3 candidate = new Vector3(
                zone.Center.x + Mathf.Cos(angle) * escapeDistance,
                crawlerPosition.y,
                zone.Center.z + Mathf.Sin(angle) * escapeDistance);

            if (IsInsideAnyZone(candidate))
                continue;
            if (!NavMeshUtil.GetClosestPointInNavMesh_Small(candidate, out Vector3 newTarget))
                continue;
            if (IsInsideAnyZone(newTarget))
                continue;

            SetCrawlerFleeSpeed(crawlerIndex);
            crawlerNextFleeTimes[crawlerIndex] = now + Settings.fleeingCooldown;
            crawlerTargets[crawlerIndex] = newTarget;
            return true;
        }
        return false; // retried next logic tick
    }

    private void RebuildExitKDTreeIfNeeded()
    {
        if (!exitKDTreeDirty)
            return;

        exitKDTreeDirty = false;
        if (crawlerExitPositions.Count == 0)
        {
            exitKDTree = null;
            return;
        }

        exitKDTree = new KDTree(crawlerExitPositions.ToArray());
        exitKDTree.Rebuild();
    }

    private bool TryFindCrawlerExitPosition(Vector3 crawlerPosition, Vector3 away, Vector3 playerPosition, out Vector3 result)
    {
        result = Vector3.zero;
        if (exitKDTree == null)
            return false;

        if (!exitQueryValid)
        {
            using var marker = ProfilerMarker_QueryExitKDTree.Auto();
            exitKDTreeResults.Clear();
            exitKDTreeQuery.Radius(exitKDTree, playerPosition, Settings.MaxDistanceToExit, exitKDTreeResults);
            exitQueryValid = true;
        }

        // Random exit whose path doesn't cross a safe zone, preferring one that is "away" from the player
        // (two reservoir samplings in one pass, no allocation)
        int chosenAway = -1, awayCount = 0;
        int chosenAny = -1, anyCount = 0;
        for (int i = 0; i < exitKDTreeResults.Count; i++)
        {
            int exitIndex = exitKDTreeResults[i];
            Vector3 exitPosition = crawlerExitPositions[exitIndex];
            if (SegmentCrossesAnyZone(crawlerPosition, exitPosition))
                continue;

            anyCount++;
            if (Random.Range(0, anyCount) == 0)
                chosenAny = exitIndex;

            Vector3 toExit = exitPosition - crawlerPosition;
            if (toExit.x * away.x + toExit.z * away.z > 0f)
            {
                awayCount++;
                if (Random.Range(0, awayCount) == 0)
                    chosenAway = exitIndex;
            }
        }

        int chosen = chosenAway >= 0 ? chosenAway : chosenAny;
        if (chosen < 0)
            return false;

        result = crawlerExitPositions[chosen];
        return true;
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  SAFE ZONES
    /// ----------------------------------------------------------------------------------------------------------------

    private void GatherZones()
    {
        zoneSnapshots.Clear();
        IReadOnlyList<CrawlerSafeZone> zones = CrawlerSafeZone.ActiveZones;
        for (int i = 0; i < zones.Count; i++)
        {
            CrawlerSafeZone zone = zones[i];
            if (zone == null || zone.Radius <= 0f)
                continue;

            zoneSnapshots.Add(new ZoneSnapshot
            {
                Center = zone.transform.position,
                Radius = zone.Radius,
                FleeMargin = zone.FleeMargin
            });
        }

        zoneCount = zoneSnapshots.Count;
        if (zoneData.Length < zoneCount)
        {
            // Only called while no job is running
            zoneData.Dispose();
            zoneData = new NativeArray<float4>(Mathf.Max(zoneCount, zoneData.Length * 2), Allocator.Persistent);
        }

        for (int i = 0; i < zoneCount; i++)
        {
            ZoneSnapshot zone = zoneSnapshots[i];
            zoneData[i] = new float4(zone.Center.x, zone.Center.z, zone.Radius * zone.Radius, 0f);
        }
    }

    private bool IsInsideAnyZone(Vector3 position)
    {
        return FindContainingZone(position) >= 0;
    }

    private int FindContainingZone(Vector3 position)
    {
        for (int i = 0; i < zoneSnapshots.Count; i++)
        {
            ZoneSnapshot zone = zoneSnapshots[i];
            float dx = position.x - zone.Center.x;
            float dz = position.z - zone.Center.z;
            if (dx * dx + dz * dz <= zone.Radius * zone.Radius)
                return i;
        }
        return -1;
    }

    // 2D (XZ) segment vs circle test, also true if either end is inside a zone
    private bool SegmentCrossesAnyZone(Vector3 from, Vector3 to)
    {
        float abx = to.x - from.x;
        float abz = to.z - from.z;
        float abLengthSq = abx * abx + abz * abz;

        for (int i = 0; i < zoneSnapshots.Count; i++)
        {
            ZoneSnapshot zone = zoneSnapshots[i];
            float acx = zone.Center.x - from.x;
            float acz = zone.Center.z - from.z;
            float t = abLengthSq > 1e-8f ? Mathf.Clamp01((acx * abx + acz * abz) / abLengthSq) : 0f;
            float px = abx * t - acx;
            float pz = abz * t - acz;
            if (px * px + pz * pz <= zone.Radius * zone.Radius)
                return true;
        }
        return false;
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  JOB
    /// ----------------------------------------------------------------------------------------------------------------

    // Moves every crawler, builds its matrix and classifies it (arrived / too far / in a zone / may flee).
    [BurstCompile]
    struct CrawlerUpdateJob : IJobParallelFor
    {
        public NativeArray<float3> Positions;
        public NativeArray<float> Yaws;
        [ReadOnly] public NativeArray<float3> Targets;
        [ReadOnly] public NativeArray<float> Speeds;
        [ReadOnly] public NativeArray<float> NextFleeTimes;
        [ReadOnly] public NativeArray<float4> Zones;
        [WriteOnly] public NativeArray<Matrix4x4> Matrices;
        [WriteOnly] public NativeArray<byte> Flags;

        public int ZoneCount;
        public float DeltaTime;
        public float CurrentTime;
        public float SqrArriveThreshold;
        public float MaxTurnRadians;
        public float Scale;
        public float3 PlayerPosition;
        public float2 PlayerForwardXZ;
        public float SqrDespawnRadius;
        public float SqrFleeingRadius;
        public float FleeingViewDot;

        public void Execute(int i)
        {
            float3 position = Positions[i];
            float3 target = Targets[i];
            float3 toTarget = target - position;
            float sqrDistToTarget = math.lengthsq(toTarget);

            byte flags = 0;
            if (sqrDistToTarget <= SqrArriveThreshold)
                flags |= FLAG_ARRIVED;

            // Same as Vector3.MoveTowards
            float step = Speeds[i] * DeltaTime;
            if (sqrDistToTarget <= step * step)
                position = target;
            else
                position += toTarget * (step * math.rsqrt(sqrDistToTarget));
            Positions[i] = position;

            // Yaw-only rotation (same as LookRotation on the flat direction + RotateTowards)
            float yaw = Yaws[i];
            float2 flatDirection = toTarget.xz;
            if (math.lengthsq(flatDirection) > 0.0001f)
            {
                float desiredYaw = math.atan2(flatDirection.x, flatDirection.y);
                float delta = WrapAngle(desiredYaw - yaw);
                yaw = WrapAngle(yaw + math.clamp(delta, -MaxTurnRadians, MaxTurnRadians));
                Yaws[i] = yaw;
            }

            // TRS with a Y rotation and uniform scale, written directly (no quaternion math)
            math.sincos(yaw, out float s, out float c);
            float sc = Scale;
            Matrices[i] = new Matrix4x4(
                new Vector4(c * sc, 0f, -s * sc, 0f),
                new Vector4(0f, sc, 0f, 0f),
                new Vector4(s * sc, 0f, c * sc, 0f),
                new Vector4(position.x, position.y, position.z, 1f));

            // Relation to the player and safe zones
            float3 playerToCrawler = position - PlayerPosition;
            float sqrDistToPlayer = math.lengthsq(playerToCrawler);
            if (sqrDistToPlayer > SqrDespawnRadius)
            {
                flags |= FLAG_FAR;
            }
            else if (ZoneCount > 0 && IsInsideAnyZone(position.xz) && IsInsideAnyZone(target.xz))
            {
                // Inside a zone and not already heading out of it
                flags |= FLAG_IN_ZONE;
            }
            else if (sqrDistToPlayer <= SqrFleeingRadius && CurrentTime >= NextFleeTimes[i])
            {
                float2 flat = playerToCrawler.xz;
                float flatSq = math.lengthsq(flat);
                if (flatSq > 1e-6f && math.dot(flat * math.rsqrt(flatSq), PlayerForwardXZ) >= FleeingViewDot)
                    flags |= FLAG_FLEE_CANDIDATE;
            }

            Flags[i] = flags;
        }

        bool IsInsideAnyZone(float2 point)
        {
            for (int z = 0; z < ZoneCount; z++)
            {
                float4 zone = Zones[z];
                if (math.lengthsq(point - zone.xy) <= zone.z)
                    return true;
            }
            return false;
        }

        static float WrapAngle(float angle)
        {
            return angle - TWO_PI * math.round(angle / TWO_PI);
        }
    }
}
