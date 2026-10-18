using UnityEngine;

// Single entry point to every tuning asset of the game.
// Each system has its own settings asset, this one gathers them, for example GameSettings.Movement.
// The asset lives in a Resources folder so it can be reached from anywhere with GameSettings.Instance.
[CreateAssetMenu(fileName = "GameSettings", menuName = "Game Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    private const string ResourcePath = "GameSettings";

    private static GameSettings instance;

    // Loads Assets/GameSettings/Resources/GameSettings.asset the first time it is used,
    // the settings assets it references are loaded with it
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

    // Static shortcuts, for example GameSettings.Movement.walkSpeed
    public static MovementSettings Movement => Instance.movement;
    public static PlayerChecksSettings PlayerChecks => Instance.playerChecks;
    public static CameraSettings Camera => Instance.camera;
    public static ArmSettings Arm => Instance.arm;
    public static ZippoSettings Zippo => Instance.zippo;
    public static ZippoVisualSettings ZippoVisual => Instance.zippoVisual;
    public static MonsterSettings Monster => Instance.monster;
    public static CrawlerSettings Crawler => Instance.crawler;
    public static InteractSettings Interact => Instance.interact;
    public static DoorSettings Door => Instance.door;
    public static MenuSettings Menu => Instance.menu;

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
