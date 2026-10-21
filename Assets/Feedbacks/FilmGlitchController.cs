using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on the player's camera.
/// Finds the MonsterController at Start, measures how much the player is looking at it,
/// and drives the Hidden/FilmGlitch shader.
/// - The more centered the monster is on screen, the stronger the effect right away.
/// - The longer the player keeps looking, the more "exposure" builds up, until the film burns.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FilmGlitchController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Height above the monster's position that the player has to look at (head / chest).")]
    public float lookHeight = 1.6f;

    [Header("Gaze detection")]
    [Tooltip("Angle (degrees) within which the monster counts as being looked at.")]
    public float maxAngle = 40f;
    [Tooltip("Layers that block the view (walls, sandbags...). Hits on the monster itself are ignored.")]
    public LayerMask occluders = ~0;
    [Tooltip("The effect only triggers when the monster is closer than this (meters).")]
    public float effectRange = 2f;

    [Header("Build-up / recovery")]
    [Tooltip("Seconds of staring straight at the monster before the film fully burns.")]
    public float timeToBurn = 3f;
    [Tooltip("How fast the effect fades when looking away (per second).")]
    public float recoverySpeed = 0.6f;
    [Tooltip("Part of the effect applied instantly based on gaze, before any build-up.")]
    [Range(0f, 1f)] public float instantAmount = 0.35f;
    [Tooltip("Exposure level at which the burn starts.")]
    [Range(0f, 1f)] public float burnStart = 0.8f;

    [Header("Sound (optional)")]
    [Tooltip("Projector rattle loop, volume follows the effect.")]
    public AudioSource projectorLoop;
    public float maxVolume = 0.8f;

    [Header("Camera shake (magnitudes 0..1, like CameraController.AddTrauma)")]
    [Tooltip("Found automatically on the player if left empty.")]
    public CameraController cameraController;
    [Tooltip("Jolt when the effect kicks in (player starts looking).")]
    [Range(0f, 1f)] public float startShakeMagnitude = 0.35f;
    public float startShakeDuration = 0.3f;
    [Tooltip("Constant tremble while the effect is active, scaled by intensity. Shakes add up, keep it low.")]
    [Range(0f, 1f)] public float trembleMagnitude = 0.15f;
    public float trembleInterval = 0.1f;
    [Tooltip("Shake on each film 'reel jump', synced with the shader.")]
    [Range(0f, 1f)] public float jumpShakeMagnitude = 0.4f;
    public float jumpShakeDuration = 0.2f;
    [Tooltip("Big shake when the film burns out.")]
    [Range(0f, 1f)] public float burnShakeMagnitude = 0.8f;
    public float burnShakeDuration = 1f;

    [Header("Events")]
    [Tooltip("Called once when the film has fully burned (death, fainting, forced flinch...).")]
    public UnityEvent onBurnedOut;

    [Header("Debug (read only)")]
    [SerializeField] MonsterController debugMonster;
    [SerializeField] float debugGaze;
    [SerializeField] float debugExposure;
    [SerializeField] string debugBlockedBy;
    [Tooltip("Forces the effect to this value to test the shader alone (-1 = off).")]
    [SerializeField, Range(-1f, 1f)] float debugForceGlitch = -1f;

    public float Gaze { get; private set; }       // 0..1 : current gaze
    public float Exposure { get; private set; }   // 0..1 : accumulated exposure

    static readonly int GlitchId = Shader.PropertyToID("_FilmGlitch");
    static readonly int BurnId = Shader.PropertyToID("_FilmBurn");

    Camera cam;
    MonsterController monster;
    bool burnedOut;
    bool wasActive;
    float trembleTimer;
    float lastFilmFrame = -1f;

    const float FilmFrameRate = 14f; // must match FRAME_RATE in FilmGlitch.shader

    void Awake() => cam = GetComponent<Camera>();

    void Start()
    {
        monster = FindAnyObjectByType<MonsterController>();
        debugMonster = monster;
        if (monster == null)
            Debug.LogWarning("FilmGlitchController: no MonsterController found in the scene.", this);

        if (cameraController == null)
            cameraController = GetComponentInParent<CameraController>();
        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController == null)
            Debug.LogWarning("FilmGlitchController: no CameraController found, camera shake disabled.", this);
    }

    // CameraController.ShakeCamera takes (duration, magnitude), in that order
    void Shake(float magnitude, float duration)
    {
        if (cameraController != null)
            cameraController.ShakeCamera(duration, magnitude);
    }

    Vector3 MonsterLookPoint => monster.transform.position + Vector3.up * lookHeight;

    void Update()
    {
        Gaze = ComputeGaze();

        if (Gaze > 0f)
            Exposure += Gaze * Time.deltaTime / Mathf.Max(0.01f, timeToBurn);
        else
            Exposure -= recoverySpeed * Time.deltaTime;
        Exposure = Mathf.Clamp01(Exposure);

        float glitch = Mathf.Clamp01(Gaze * instantAmount + Exposure);
        float burn = Mathf.Clamp01((Exposure - burnStart) / Mathf.Max(0.01f, 1f - burnStart));

        debugGaze = Gaze;
        debugExposure = Exposure;
        if (debugForceGlitch >= 0f) { glitch = debugForceGlitch; burn = 0f; }

        Shader.SetGlobalFloat(GlitchId, glitch);
        Shader.SetGlobalFloat(BurnId, burn);

        if (projectorLoop != null)
        {
            projectorLoop.volume = glitch * maxVolume;
            projectorLoop.pitch = 1f + (Random.value - 0.5f) * 0.2f * glitch;
            if (glitch > 0.01f && !projectorLoop.isPlaying) projectorLoop.Play();
            else if (glitch <= 0.01f && projectorLoop.isPlaying) projectorLoop.Stop();
        }

        UpdateShake(glitch);

        if (burn >= 1f && !burnedOut)
        {
            burnedOut = true;
            Shake(burnShakeMagnitude, burnShakeDuration);
            onBurnedOut?.Invoke();
        }
        else if (Exposure <= 0f)
        {
            burnedOut = false;
        }
    }

    void UpdateShake(float glitch)
    {
        bool active = glitch > 0.02f;

        // 1. Jolt when the effect starts
        if (active && !wasActive)
            Shake(startShakeMagnitude, startShakeDuration);
        wasActive = active;

        if (!active) { trembleTimer = 0f; return; }

        // 2. Constant tremble, stronger with intensity
        trembleTimer -= Time.deltaTime;
        if (trembleTimer <= 0f)
        {
            trembleTimer = trembleInterval;
            Shake(trembleMagnitude * glitch, trembleInterval * 1.5f);
        }

        // 3. Shake on reel jumps, same random sequence as the shader (_Time.y)
        float filmFrame = Mathf.Floor(Time.timeSinceLevelLoad * FilmFrameRate);
        if (filmFrame != lastFilmFrame)
        {
            lastFilmFrame = filmFrame;
            float jumpChance = glitch * glitch * 0.5f;
            if (Hash11(filmFrame + 51.7f) < jumpChance)
                Shake(jumpShakeMagnitude * glitch, jumpShakeDuration);
        }
    }

    // Same hash as in the shader
    static float Hash11(float p)
    {
        p = Frac(p * 0.1031f);
        p *= p + 33.33f;
        p *= p + p;
        return Frac(p);
    }

    static float Frac(float x) => x - Mathf.Floor(x);

    float ComputeGaze()
    {
        if (monster == null || !monster.isActiveAndEnabled)
            return 0f;

        Vector3 origin = cam.transform.position;
        Vector3 toMonster = MonsterLookPoint - origin;
        float dist = toMonster.magnitude;
        if (dist > effectRange || dist < 0.01f) return 0f;

        float angle = Vector3.Angle(cam.transform.forward, toMonster);
        if (angle > maxAngle) return 0f;

        // Blocked only if we hit something that isn't part of the monster
        if (Physics.Raycast(origin, toMonster / dist, out RaycastHit hit, dist, occluders, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(monster.transform))
        {
            debugBlockedBy = hit.collider.name;
            return 0f;
        }
        debugBlockedBy = "";

        // 1 at screen center, 0 at the edge of the cone, smooth curve
        float t = 1f - angle / maxAngle;
        return t * t * (3f - 2f * t);
    }

    /// <summary>Resets the effect (respawn, cutscene...).</summary>
    public void ResetEffect()
    {
        Exposure = 0f;
        burnedOut = false;
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(GlitchId, 0f);
        Shader.SetGlobalFloat(BurnId, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (monster == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(MonsterLookPoint, 0.2f);
    }
}