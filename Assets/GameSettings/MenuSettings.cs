using UnityEngine;

// Tuning of the main menu and the loading screen, used by MenuPrincipal
[CreateAssetMenu(fileName = "MenuSettings", menuName = "Game Settings/Menu Settings")]
public class MenuSettings : ScriptableObject
{
    [Header("Astuces de chargement")]
    [TextArea(2, 4)]
    public string[] astuces;

    [Header("Reglages chargement")]
    public string nomSceneJeu = "Scene_Demo";
    public float dureeFondu = 1f;
    public float attenteSecurite = 5f;
    public float dureeCourseMinimale = 3f;

    [Header("Reglages camera")]
    public float dureeDeplacement = 2f;

    public AnimationCurve courbeMouvement = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
