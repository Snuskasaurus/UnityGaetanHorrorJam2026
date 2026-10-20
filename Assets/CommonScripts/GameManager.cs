using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.CommonScripts
{
    public class GameManager : MonoBehaviour
    {
        public static int collectibleCollected = 0;
        public float timeBeforeLose;
        public static bool isLoosing = false;
        // References
        public PlayerController playerController;
        public ZippoController zippoController;
        public AudioSource audioSource;
        public SoundController soundController;

        private static GameManager instance;

        public UnityEvent OnStartLose;
        public UnityEvent OnCancelLose;
        public UnityEvent OnLose;
        public UnityEvent OnWin;

        void Awake()
        {
            instance = this;
        }

        // Use this for initialization
        void Start()
        {
            playerController = Common.FindTypeInScene<PlayerController>();
            zippoController = playerController.GetComponent<ZippoController>();
            audioSource = playerController.GetComponent<AudioSource>();
            soundController = playerController.GetComponent<SoundController>();
            timeBeforeLose = GameSettings.GameFlow.TimeBeforeLose;
        }

        // Update is called once per frame
        void Update()
        {
            if (!zippoController.IsZippoLit && timeBeforeLose > 0)
            {
                timeBeforeLose -= Time.deltaTime;
                if (!isLoosing)
                {
                    OnStartLose.Invoke();
                    isLoosing = true;
                }
                if (timeBeforeLose <= 0)
                {
                    OnLose.Invoke();
                }
            }
            else if (timeBeforeLose != 0)
            {
                timeBeforeLose = GameSettings.GameFlow.TimeBeforeLose;
                if (isLoosing)
                {
                    isLoosing = false;
                    OnCancelLose.Invoke();
                }
            }
        }

        // Plays a sound on the player from any script, the monster does not hear it
        public static void PlaySound(AudioClip clip)
        {
            if (instance != null && instance.soundController != null)
            {
                instance.soundController.PlaySound(clip);
            }
        }

        // Plays a sound on the player from any script, the monster hears it with the given loudness
        public static void PlaySound(AudioClip clip, float volume, float monsterLoudness)
        {
            if (instance != null && instance.soundController != null)
            {
                instance.soundController.PlaySound(clip, volume, monsterLoudness);
            }
        }

        public static void CollectibleCollected()
        {
            collectibleCollected++;
            if (collectibleCollected >= GameSettings.GameFlow.CollectibleToCollect)
            {
                Common.FindTypeInScene<GameManager>().OnWin.Invoke();
            }
        }
    }
}