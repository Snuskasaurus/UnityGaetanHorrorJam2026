using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A poser sur Panel_Options. Il utilise les MEMES InputActions que les scripts du joueur.
public class MenuOptions : MonoBehaviour
{
    // Ces libelles ne changent PAS le nom technique des actions Unity.
    // Ils servent seulement a afficher un texte clair dans le menu Options.

    [Header("Sensibilite souris")]
    [SerializeField] private Slider sliderSensibilite;
    [SerializeField] private TMP_Text texteSensibilite;

    [Header("Touches - liste automatique")]
    [SerializeField] private Transform contenuTouches;
    [SerializeField] private Button modeleBoutonTouche;
    [SerializeField] private TMP_Text texteInstruction;

    private const string CleTouches = "Options_Raccourcis_V1";
    private static bool touchesChargees;
    private InputActionAsset actions;
    private InputActionRebindingExtensions.RebindingOperation operation;
    private readonly List<Button> boutonsCrees = new List<Button>();

    private void Awake()
    {
        if (sliderSensibilite != null)
        {
            sliderSensibilite.minValue = 0.1f;
            sliderSensibilite.maxValue = 5f;
            sliderSensibilite.value = CameraController.SensibiliteSauvegardee;
            sliderSensibilite.onValueChanged.AddListener(ChangerSensibilite);
            ChangerSensibilite(sliderSensibilite.value);
        }
    }

    public void ChangerSensibilite(float valeur)
    {
        CameraController.SauverSensibilite(valeur);
        if (texteSensibilite != null) texteSensibilite.text = valeur.ToString("0.00");
    }

    private void OnDestroy()
    {
        if (operation != null)
        {
            operation.Cancel();
            if (operation != null) { operation.Dispose(); operation = null; }
        }
    }
}
