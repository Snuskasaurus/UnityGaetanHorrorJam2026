using UnityEngine;

// Ambient light and fog of an outdoor level, outside and inside the InteriorVolumes, used by AmbientController
[CreateAssetMenu(fileName = "AmbienceSettings", menuName = "Game Settings/Ambience Settings")]
public class AmbienceSettings : ScriptableObject
{
    [Header("Outside")]
    [ColorUsage(false, true)]
    public Color outdoorSkyColor = new Color(0.043f, 0.063f, 0.094f);  // top of the gradient: surfaces facing up

    [ColorUsage(false, true)]
    public Color outdoorEquatorColor = new Color(0.025f, 0.035f, 0.05f);

    [ColorUsage(false, true)]
    public Color outdoorGroundColor = new Color(0.005f, 0.005f, 0.008f);  // surfaces facing down

    public Color outdoorFogColor = new Color(0.02f, 0.028f, 0.04f);  // not pure black so the silhouettes stand out

    [Header("Inside")]
    [ColorUsage(false, true)]
    public Color interiorAmbientColor = new Color(0.014150947f, 0.0014749538f, 0.00060074753f);  // almost black, like Scene_Interior

    public Color interiorFogColor = Color.black;

    [Header("Transition")]
    public float transitionTime = 0.8f;  // seconds to go from outside to inside, or back
}
