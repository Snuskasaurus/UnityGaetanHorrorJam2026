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
    [System.Serializable]
    public class LibelleCommande
    {
        public string map;
        public string action;
        public string partie;
        public string nomAffiche;

        public LibelleCommande(string map, string action, string partie, string nomAffiche)
        {
            this.map = map;
            this.action = action;
            this.partie = partie;
            this.nomAffiche = nomAffiche;
        }
    }

    [Header("Noms affiches aux joueurs (modifiables)")]
    [Tooltip("Modifie seulement Nom Affiche. La Map, l'Action et la Partie designent la commande du Input System.")]
    [SerializeField] private List<LibelleCommande> nomsCommandes = new List<LibelleCommande>
    {
        new LibelleCommande("Player", "Move", "up", "Avancer"),
        new LibelleCommande("Player", "Move", "down", "Reculer"),
        new LibelleCommande("Player", "Move", "left", "Aller à gauche"),
        new LibelleCommande("Player", "Move", "right", "Aller à droite"),
        new LibelleCommande("Player", "Attack", "", "Attaquer"),
        new LibelleCommande("Player", "Interact", "", "Interagir"),
        new LibelleCommande("Player", "Crouch", "", "S'accroupir"),
        new LibelleCommande("Player", "Jump", "", "Sauter"),
        new LibelleCommande("Player", "Sprint", "", "Courir"),
        new LibelleCommande("Player", "Previous", "", "Objet précédent"),
        new LibelleCommande("Player", "Next", "", "Objet suivant"),
        new LibelleCommande("Zippo", "LightAttempt", "", "Allumer le briquet"),
        new LibelleCommande("Zippo", "Intensity", "", "Régler la flamme")
    };

    private string NomLisible(InputAction action, InputBinding binding)
    {
        string partie = binding.isPartOfComposite ? binding.name : "";
        string map = action.actionMap != null ? action.actionMap.name : "";

        foreach (LibelleCommande entree in nomsCommandes)
        {
            if (entree == null || string.IsNullOrWhiteSpace(entree.nomAffiche))
                continue;

            if (string.Equals(entree.map, map, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entree.action, action.name, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entree.partie ?? "", partie ?? "", System.StringComparison.OrdinalIgnoreCase))
            {
                return entree.nomAffiche;
            }
        }

        // Si une nouvelle action apparait, elle reste visible avec son nom technique.
        return binding.isPartOfComposite ? action.name + " - " + binding.name : action.name;
    }

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ChargerAuLancement()
    {
        ChargerTouches();
    }

    private static InputActionAsset ActionsDuJeu()
    {
        return InputManager.PlayerLook.actionMap.asset;
    }

    private static void ChargerTouches()
    {
        if (touchesChargees) return;
        InputActionAsset asset = ActionsDuJeu();
        if (asset == null) return;
        touchesChargees = true;
        string json = PlayerPrefs.GetString(CleTouches, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { asset.LoadBindingOverridesFromJson(json); }
            catch (System.Exception e) { Debug.LogWarning("Touches sauvegardees invalides : " + e.Message); }
        }
    }

    private void Awake()
    {
        ChargerTouches();
        actions = ActionsDuJeu();
        if (sliderSensibilite != null)
        {
            sliderSensibilite.minValue = 0.1f;
            sliderSensibilite.maxValue = 5f;
            sliderSensibilite.value = CameraController.SensibiliteSauvegardee;
            sliderSensibilite.onValueChanged.AddListener(ChangerSensibilite);
            ChangerSensibilite(sliderSensibilite.value);
        }
        if (modeleBoutonTouche != null) modeleBoutonTouche.gameObject.SetActive(false);
        if (texteInstruction != null) texteInstruction.text = "Clique sur une touche pour la modifier";
        CreerTouches();
    }

    public void ChangerSensibilite(float valeur)
    {
        CameraController.SauverSensibilite(valeur);
        if (texteSensibilite != null) texteSensibilite.text = valeur.ToString("0.00");
    }

    private void CreerTouches()
    {
        if (actions == null || contenuTouches == null || modeleBoutonTouche == null) return;
        foreach (InputActionMap map in actions.actionMaps)
        {
            // Le menu de l'utilisateur contient Player et Zippo. UI est gere par l'interface.
            if (map.name != "Player" && map.name != "Zippo") continue;
            foreach (InputAction action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    InputBinding binding = action.bindings[i];
                    if (binding.isComposite) continue; // Les parties WASD sont listees individuellement
                    string path = binding.effectivePath;
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.StartsWith("<Keyboard>") && !path.StartsWith("<Mouse>")) continue;
                    if (path.Contains("/delta") || path.Contains("/position") || path.Contains("/scroll")) continue;

                    int index = i;
                    Button bouton = Instantiate(modeleBoutonTouche, contenuTouches);
                    bouton.gameObject.SetActive(true);
                    boutonsCrees.Add(bouton);
                    string nom = NomLisible(action, binding);
                    MettreAJourTexte(bouton, nom, action, index);
                    bouton.onClick.AddListener(() => DemarrerRemappage(action, index, bouton, nom));
                }
            }
        }
    }

    private void MettreAJourTexte(Button bouton, string nom, InputAction action, int index)
    {
        TMP_Text texte = bouton.GetComponentInChildren<TMP_Text>();
        if (texte != null) texte.text = nom + " : " + action.GetBindingDisplayString(index);
    }

    private void DemarrerRemappage(InputAction action, int index, Button bouton, string nom)
    {
        if (operation != null) return;
        if (texteInstruction != null) texteInstruction.text = "Appuie sur la nouvelle touche (Echap pour annuler)";
        bool etaitActive = action.enabled;
        action.Disable();
        operation = action.PerformInteractiveRebinding(index)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(op => TerminerRemappage(op, action, etaitActive, bouton, nom, false))
            .OnComplete(op => TerminerRemappage(op, action, etaitActive, bouton, nom, true));
        if (action.bindings[index].effectivePath.StartsWith("<Mouse>"))
            operation.WithControlsHavingToMatchPath("<Mouse>");
        else
            operation.WithControlsHavingToMatchPath("<Keyboard>");
        operation.Start();
    }

    private void TerminerRemappage(InputActionRebindingExtensions.RebindingOperation op,
        InputAction action, bool etaitActive, Button bouton, string nom, bool sauvegarder)
    {
        op.Dispose();
        operation = null;
        if (etaitActive) action.Enable();
        if (sauvegarder) SauvegarderTouches();
        if (texteInstruction != null) texteInstruction.text = "Clique sur une touche pour la modifier";
        // Le binding concerne peut avoir change.
        ActualiserToutesLesTouches();
    }

    private void ActualiserToutesLesTouches()
    {
        // Le bouton qui a ete clique doit afficher la nouvelle touche.
        foreach (Button bouton in boutonsCrees)
        {
            // Les labels sont reutilises lors de la reconstruction.
            Destroy(bouton.gameObject);
        }
        boutonsCrees.Clear();
        CreerTouches();
    }

    private void SauvegarderTouches()
    {
        PlayerPrefs.SetString(CleTouches, actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    public void RestaurerTouches()
    {
        if (operation != null) return;
        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(CleTouches);
        PlayerPrefs.Save();
        ActualiserToutesLesTouches();
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
