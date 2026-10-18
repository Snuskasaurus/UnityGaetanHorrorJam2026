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

        public UnityEvent OnStartLose;
        public UnityEvent OnCancelLose;
        public UnityEvent OnLose;
        public UnityEvent OnWin;

        // Use this for initialization
        void Start()
        {
            playerController = Common.FindTypeInScene<PlayerController>();
            zippoController = playerController.GetComponent<ZippoController>();
            audioSource = playerController.GetComponent<AudioSource>();
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