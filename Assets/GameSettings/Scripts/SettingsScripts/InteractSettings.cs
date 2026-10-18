using UnityEngine;

// Tuning of the interaction selection, used by InteractManager
[CreateAssetMenu(fileName = "InteractSettings", menuName = "Game Settings/Interact Settings")]
public class InteractSettings : ScriptableObject
{
    [Header("Selection")]
    [Range(0f, 1f)]
    public float minLookDot = 0.8f;

    public string playerTag = "Player";
}
