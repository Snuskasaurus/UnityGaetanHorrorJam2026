using UnityEngine;

// Every tuning value of the game, gathered in a single asset.
// Each system reads its own section, for example GameSettings.Instance.movement.
// The asset lives in a Resources folder so it can be reached from anywhere with GameSettings.Instance.
[CreateAssetMenu(fileName = "GameSettings", menuName = "Game Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    private const string ResourcePath = "GameSettings";

    private static GameSettings instance;

    // Loads Assets/GameSettings/Resources/GameSettings.asset the first time it is used
    public static GameSettings Instance
    {
        get
        {
            // Plain reference check: this getter is called in hot loops and an asset is never destroyed at runtime
            if (ReferenceEquals(instance, null))
            {
                instance = Resources.Load<GameSettings>(ResourcePath);
                if (instance == null)
                {
                    Debug.LogError($"GameSettings : no asset found at Resources/{ResourcePath}.");
                }
            }
            return instance;
        }
    }

    [Header("Player")]
    public MovementSettings movement;
    public PlayerChecksSettings playerChecks;
    public CameraSettings camera;
    public ArmSettings arm;

    [Header("Zippo")]
    public ZippoSettings zippo;
    public ZippoVisualSettings zippoVisual;

    [Header("Enemies")]
    public MonsterSettings monster;
    public CrawlerSettings crawler;

    [Header("Level")]
    public InteractSettings interact;
    public DoorSettings door;

    [Header("Menu")]
    public MenuSettings menu;
}

// Tuning of the player movement, used by MovementController
[System.Serializable]
public class MovementSettings
{
    [Header("Acceleration")]
    public float acceleration = 20f;  // m/s² at multiplier 1

    public float deceleration = 25f;

    // Acceleration and deceleration multipliers by speed ratio (current speed / max speed)
    public AnimationCurve accelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -2f),
        new Keyframe(1f, 0.15f, 0f, 0f));

    public AnimationCurve decelerationBySpeed = new AnimationCurve(
        new Keyframe(0f, 0.3f, 0f, 0f),
        new Keyframe(1f, 1f, 1.4f, 0f));

    public float lateralGrip = 40f;  // m/s², how fast sideways drift is removed when changing direction

    [Header("Movement Speeds")]
    [Range(0f, 10f)]
    public float walkSpeed = 5f;

    [Range(0f, 10f)]
    public float runSpeed = 10f;

    [Range(0f, 10f)]
    public float crouchSpeed = 2.5f;

    [Header("Slopes")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 45f;

    public float groundStickForce = 5f;
}

// Tuning of the ground and ceiling detection, used by PlayerController
[System.Serializable]
public class PlayerChecksSettings
{
    [Header("Environment Checks")]
    public LayerMask environmentMask = ~0;

    [Range(0.01f, 1f)]
    public float groundCheckDistance = 0.1f;

    [Range(0.01f, 2f)]
    public float ceilingCheckDistance = 0.5f;

    [Range(0.5f, 1f)]
    public float checkRadiusFactor = 0.95f;

    public float risingVelocityThreshold = 0.1f;
}

// Tuning of the first person camera, used by CameraController.
// The player options (sensitivity, invert) are not here, they belong to the options menu.
[System.Serializable]
public class CameraSettings
{
    [Header("Look Angles")]
    [Range(0f, 90f)]
    public float maxBottomVerticalAngle = 45f;

    [Range(0f, 90f)]
    public float maxTopVerticalAngle = 85f;
}

// Tuning of the first person arms pulled back near walls, used by ArmWallRetraction
[System.Serializable]
public class ArmSettings
{
    [Header("Detection")]
    public LayerMask wallMask = ~0;

    [Range(0.01f, 0.3f)]
    public float probeRadius = 0.08f;

    [Header("Retraction")]
    [Range(0f, 1f)]
    public float maxRetract = 0.35f;

    [Range(0f, 0.5f)]
    public float maxLower = 0.1f;

    public Vector3 maxTuckAngles = new Vector3(15f, -10f, 0f);

    public float retractSharpness = 20f;

    public float extendSharpness = 8f;
}

// Tuning of the zippo mechanics, used by ZippoController
[System.Serializable]
public class ZippoSettings
{
    [Header("Light Attempt")]
    [Range(0f, 1f)]
    public float lightAttemptSuccessChance = 0.35f;

    [Header("Flame Power")]
    [Range(0.05f, 1f)]
    public float minFlamePower = 0.2f;

    [Range(0.05f, 1f)]
    public float startFlamePower = 0.6f;

    [Range(0.05f, 1f)]
    public float fleeingPower = 0.8f;

    [Range(0.01f, 0.5f)]
    public float scrollStep = 0.1f;

    [Header("Ressource Management")]
    public float consumptionPerSecond = 1f;

    public float maxRessource = 100f;
}

// Tuning of the zippo flame visual, used by ZippoFlameVisual
[System.Serializable]
public class ZippoVisualSettings
{
    // Light range and flame size by flame power (0 to 1), relative to their values in the prefab
    [Header("Power")]
    public AnimationCurve rangeByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    public AnimationCurve flameScaleByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    public float powerSmoothing = 10f;  // how fast the flame visually reaches a new power
}

// Tuning of the monster behaviour, used by MonsterManager
[System.Serializable]
public class MonsterSettings
{
    public MonsterBehaviourState DefaultBehaviour = MonsterBehaviourState.Wandering;

    [Header("Common")]
    public float radiusTargetReached = 1.0f;

    [Header("Movement")]
    public float MovementSpeedDefault = 10.0f;
    public float MovementSpeedChasing = 10.0f;
    public float MovementSpeedFleeing = 10.0f;

    [Header("State | Wandering")]
    [Range(0, 100)]
    public int chanceToWaitAfterTargetReached = 100;

    [Header("State | Chasing")]
    public float radiusLoosingPlayer = 10.0f;

    [Header("State | InvestigatingSound")]
    public float minRadiusToSound = 1.0f;
    public float maxRadiusToSound = 8.0f;

    [Header("State | Waiting")]
    public float minTimeToWait = 0.5f;
    public float maxTimeToWait = 1.0f;

    [Header("State | Fleeing")]
    public float FleeingRadius = 10.0f;

    [Header("State | OffMap")]
    public float MinTimeSpentOffMap = 1.0f;
    public float MaxTimeSpentOffMap = 3.0f;

    [Header("Vision System")]
    public bool visionSystemEnabled = true;
    public float radiusVisibility = 6.0f;

    [Header("Hearing System")]
    public bool hearingSystemEnabled = true;
    public float hearingThreshold = 0.5f;
    public float soundSnapDistance = 5.0f;
    public float perfectPrecisionLoudnessRatio = 4.0f;

    [Header("Scare System")]
    public bool fearSystemEnabled = true;

    [Header("Debugs")]
    public bool showDebug = true;
    public bool showDebugRadius = true;
    public bool showDebugHearing = true;
    public float timeShowingAudioEvent = 0.5f;
}

// Tuning of the crawlers, used by CrawlerManager
[System.Serializable]
public class CrawlerSettings
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

// Tuning of the interaction selection, used by InteractManager
[System.Serializable]
public class InteractSettings
{
    [Header("Selection")]
    [Range(0f, 1f)]
    public float minLookDot = 0.8f;

    public string playerTag = "Player";
}

// Tuning shared by every interactive door, used by PorteInteractive
[System.Serializable]
public class DoorSettings
{
    [Header("JOUEUR")]
    public float distanceInteraction = 3f;

    [Header("OUVERTURE")]
    public float sensibiliteSouris = 0.4f;
    public bool inverserSens = false;
    public float angleMinimum = -100f;
    public float angleMaximum = 100f;

    [Header("PHYSIQUE")]
    public float masse = 10f;
    public float force = 500f;
    public float amortissement = 55f;
}

// Tuning of the main menu and the loading screen, used by MenuPrincipal
[System.Serializable]
public class MenuSettings
{
    [Header("Astuces de chargement")]
    [TextArea(2, 4)]
    public string[] astuces;

    [Header("Reglages chargement")]
    public string nomSceneJeu = "Scene_Demo";
    public float dureeFondu = 1f;
    public float attenteSecurite = 5f;
    public float dureeCourseMinimale = 3f;

    [Header("Reglages camera")]
    public float dureeDeplacement = 2f;

    public AnimationCurve courbeMouvement = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
