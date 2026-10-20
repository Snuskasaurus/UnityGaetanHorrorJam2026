using UnityEngine;

// Artillery strikes falling around the player, used by ArtillerySpawner and ArtilleryShell
[CreateAssetMenu(fileName = "ArtillerySettings", menuName = "Game Settings/Artillery Settings")]
public class ArtillerySettings : ScriptableObject
{
    [Header("Spawn")]
    public GameObject shellPrefab;

    public float minInterval = 25f;  // seconds between two strikes, picked at random between min and max
    public float maxInterval = 70f;

    public float minDistance = 15f;  // meters from the player
    public float maxDistance = 60f;

    [Range(0f, 1f)]
    public float closeImpactChance = 0.15f;  // chance of a near miss, close enough to shake the camera hard

    public float closeMinDistance = 5f;
    public float closeMaxDistance = 12f;

    public LayerMask groundMask = ~0;  // what the shell lands on

    [Header("Trench Targeting")]
    [Range(0f, 1f)]
    public float trenchImpactChance = 0.75f;  // chance that a shell aims at a trench near the player instead of open ground

    public string[] trenchNamePrefixes = { "M_Tranche", "Trancher" };  // objects whose name starts with one of these are trench pieces

    public float trenchSpread = 6f;  // meters around the targeted trench piece, the shell mostly lands on the edges

    public float maxTrenchPieceSize = 10f;  // meters, bigger pieces (a whole ground plane) are not used as targets

    [Header("Fall")]
    public float fallHeight = 120f;  // meters above the impact where the shell appears
    public float fallSpeed = 50f;  // m/s, slow enough to hear the whistle before the blast

    [Range(0f, 45f)]
    public float fallAngle = 15f;  // degrees from vertical

    [Header("Sounds")]
    public AudioClip[] whistleSounds;  // played by the falling shell
    public AudioClip[] explosionSounds;

    [Range(0f, 1f)]
    public float explosionVolume = 1f;

    public float explosionMaxDistance = 300f;  // meters, explosions are heard far away

    public float monsterLoudness = 0f;  // loudness sent to the monster hearing, 0 = the monster ignores the strikes

    [Header("Flash")]
    public Color flashColor = new Color(1f, 0.7f, 0.4f);
    public float flashIntensity = 8f;
    public float flashRange = 30f;
    public float flashDuration = 0.7f;

    [Header("Camera Shake")]
    public float shakeRadius = 30f;  // meters, no shake beyond
    public float maxShakeMagnitude = 0.8f;  // shake when the shell lands on the player
    public float shakeDuration = 1.2f;

    [Header("Effects")]
    public Material particleMaterial;  // soft particle material used by the fire, the smoke, the dirt and the trail
    public Color fireColor = new Color(1f, 0.55f, 0.15f, 1f);
    public Color smokeColor = new Color(0.12f, 0.11f, 0.1f, 0.65f);
    public Color dirtColor = new Color(0.08f, 0.06f, 0.04f, 1f);
    public float smokeLifetime = 7f;  // seconds the smoke stays
}
