using UnityEngine;

[CreateAssetMenu(fileName = "GameFlowSettings", menuName = "Game Settings/Game Flow Settings")]
public class GameFlowSettings : ScriptableObject
{
    [Header("Win Conditions")]
    public int CollectibleToCollect = 3;

    [Header("Lose Conditions")]
    public float TimeBeforeLose = 10;
}
