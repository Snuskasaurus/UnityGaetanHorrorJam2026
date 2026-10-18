using UnityEngine;

// Tuning shared by every interactive door, used by PorteInteractive
[CreateAssetMenu(fileName = "DoorSettings", menuName = "Game Settings/Door Settings")]
public class DoorSettings : ScriptableObject
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
