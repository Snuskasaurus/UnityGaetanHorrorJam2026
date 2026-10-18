using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Game Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    /// ----------------------------------------------------------------------------------------------------------------
    /// GAME SETTINGS
    /// ----------------------------------------------------------------------------------------------------------------

    public static MovementSettings Movement => Instance.movement;
    public MovementSettings movement;

    public static PlayerChecksSettings PlayerChecks => Instance.playerChecks;
    public PlayerChecksSettings playerChecks;

    public static CameraSettings Camera => Instance.camera;
    public CameraSettings camera;

    public static ArmSettings Arm => Instance.arm;
    public ArmSettings arm;

    public static ZippoSettings Zippo => Instance.zippo;
    public ZippoSettings zippo;

    public static ZippoVisualSettings ZippoVisual => Instance.zippoVisual;
    public ZippoVisualSettings zippoVisual;

    public static MonsterSettings Monster => Instance.monster;
    public MonsterSettings monster;

    public static CrawlerSettings Crawler => Instance.crawler;
    public CrawlerSettings crawler;

    public static InteractSettings Interact => Instance.interact;
    public InteractSettings interact;

    public static DoorSettings Door => Instance.door;
    public DoorSettings door;

    public static MenuSettings Menu => Instance.menu;
    public MenuSettings menu;

    /// ----------------------------------------------------------------------------------------------------------------
    /// ----------------------------------------------------------------------------------------------------------------
    /// ----------------------------------------------------------------------------------------------------------------

    private const string ResourcePath = "GameSettings";
    private static GameSettings instance;

    public static GameSettings Instance
    {
        get
        {
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
}
