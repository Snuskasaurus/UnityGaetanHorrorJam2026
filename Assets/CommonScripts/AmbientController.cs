using UnityEngine;

// Ambient light and fog of an outdoor level: a dim night gradient outside, almost black inside the InteriorVolumes.
// Put it once in the scenes that need it (outdoor levels), not on the player: indoor scenes keep their own ambience.
public class AmbientController : MonoBehaviour
{
    // Tuning values, read from the ambience settings of GameSettings
    private AmbienceSettings Settings => GameSettings.Ambience;

    // References
    private Transform playerHead;

    // Internal variables
    private float interiorBlend;  // 0 = outside, 1 = inside

    private void Start()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            playerHead = player.PlayerCamera != null ? player.PlayerCamera.transform : player.transform;
        }

        // The gradient mode lights the surfaces facing up more than the ones facing down
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

        // No transition at the start of the level, the player is already outside or inside
        interiorBlend = IsInside() ? 1f : 0f;
        Apply();
    }

    private void Update()
    {
        float target = IsInside() ? 1f : 0f;
        float step = Settings.transitionTime > 0f ? Time.deltaTime / Settings.transitionTime : 1f;
        interiorBlend = Mathf.MoveTowards(interiorBlend, target, step);
        Apply();
    }

    private bool IsInside()
    {
        return playerHead != null && InteriorVolume.IsInsideAny(playerHead.position);
    }

    // Inside, the three gradient colors all go to the interior color, which is the same as a flat ambient
    private void Apply()
    {
        RenderSettings.ambientSkyColor = Color.Lerp(Settings.outdoorSkyColor, Settings.interiorAmbientColor, interiorBlend);
        RenderSettings.ambientEquatorColor = Color.Lerp(Settings.outdoorEquatorColor, Settings.interiorAmbientColor, interiorBlend);
        RenderSettings.ambientGroundColor = Color.Lerp(Settings.outdoorGroundColor, Settings.interiorAmbientColor, interiorBlend);
        RenderSettings.fogColor = Color.Lerp(Settings.outdoorFogColor, Settings.interiorFogColor, interiorBlend);
    }
}
