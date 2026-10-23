using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pause pour la scene de jeu.
/// Echappe ouvre / ferme le menu, met en pause la simulation et affiche
/// un flou URP (Volume). Les boutons UI appellent les methodes publiques.
/// </summary>
public class MenuPause : MonoBehaviour
{
    [Header("Input System - touche Pause")]
    [Tooltip("Facultatif si l'action s'appelle menu-IG/Pause dans InputSystem_Actions.")]
    [SerializeField] private InputActionReference actionPause;

    [Header("Panneaux UI")]
    [SerializeField] private GameObject panneauPause;
    [SerializeField] private GameObject panneauOptions;

    [Header("Retour au menu principal")]
    [SerializeField] private string sceneMenuPrincipal = "Scene_Menu";

    [Header("Flou URP - Global Volume")]
    [SerializeField] private Volume volumeFlou;
    [SerializeField, Min(0f)] private float dureeFonduFlou = 0.2f;

    [Header("Joueur")]
    [SerializeField] private CameraController cameraJoueur;
    [SerializeField] private bool mettreAudioEnPause = true;

    private InputAction entreePause;
    private InputActionMap commandesJoueur;
    private InputActionMap commandesZippo;
    private bool entreeActiveeParScript;
    private bool commandesJoueurEtaientActives;
    private bool commandesZippoEtaientActives;
    private bool cameraEtaitActive;
    private bool audioEtaitEnPause;
    private float ancienneEchelleTemps = 1f;
    private bool estEnPause;
    private Coroutine animationFlou;

    private void Awake()
    {
        if (panneauPause != null) panneauPause.SetActive(false);
        if (panneauOptions != null) panneauOptions.SetActive(false);
        if (volumeFlou != null) volumeFlou.weight = 0f;

        if (cameraJoueur == null)
            cameraJoueur = FindFirstObjectByType<CameraController>();

        // Les actions du joueur restent celles du InputManager existant.
        InputAction regarder = InputManager.PlayerLook;
        commandesJoueur = regarder != null ? regarder.actionMap : null;

        InputAction actionZippo = InputManager.ZippoLightAttempt;
        commandesZippo = actionZippo != null ? actionZippo.actionMap : null;

        // On peut glisser explicitement une action depuis l'Inspector.
        if (actionPause != null)
            entreePause = actionPause.action;

        // Sinon on cherche automatiquement l'action du screenshot.
        if (entreePause == null && commandesJoueur != null)
        {
            InputActionAsset asset = commandesJoueur.asset;
            if (asset != null)
                entreePause = asset.FindAction("menu-IG/Pause", false);
        }
    }

    private void OnEnable()
    {
        if (entreePause == null)
        {
            Debug.LogError(
                "MenuPause : action introuvable. Renomme 'New action' en 'Pause' " +
                "dans la map 'menu-IG', ou renseigne Action Pause dans l'Inspector.", this);
            return;
        }

        entreeActiveeParScript = !entreePause.enabled;
        entreePause.performed += QuandPauseAppuyee;
        if (entreeActiveeParScript) entreePause.Enable();
    }

    private void OnDisable()
    {
        if (entreePause != null)
        {
            entreePause.performed -= QuandPauseAppuyee;
            if (entreeActiveeParScript) entreePause.Disable();
        }

        // Ne jamais laisser Time.timeScale a zero en changeant de scene.
        if (estEnPause) RestaurerJeu(false);
    }

    private void QuandPauseAppuyee(InputAction.CallbackContext context)
    {
        if (!estEnPause)
        {
            OuvrirPause();
        }
        else if (panneauOptions != null && panneauOptions.activeSelf)
        {
            // Dans les Options, Echap sert a annuler le remappage d'une touche.
            // Le bouton Retour ramene au menu Pause.
            return;
        }
        else
        {
            Reprendre();
        }
    }

    private void OuvrirPause()
    {
        if (estEnPause) return;
        if (panneauPause == null)
        {
            Debug.LogError("MenuPause : attribue Panneau Pause dans l'Inspector.", this);
            return;
        }

        estEnPause = true;
        ancienneEchelleTemps = Time.timeScale;
        audioEtaitEnPause = AudioListener.pause;

        commandesJoueurEtaientActives = commandesJoueur != null && commandesJoueur.enabled;
        commandesZippoEtaientActives = commandesZippo != null && commandesZippo.enabled;
        cameraEtaitActive = cameraJoueur != null && cameraJoueur.enabled;

        if (commandesJoueurEtaientActives) commandesJoueur.Disable();
        if (commandesZippoEtaientActives) commandesZippo.Disable();
        if (cameraEtaitActive) cameraJoueur.enabled = false;

        Time.timeScale = 0f;
        if (mettreAudioEnPause) AudioListener.pause = true;

        panneauPause.SetActive(true);
        if (panneauOptions != null) panneauOptions.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AnimerFlou(1f);
    }

    // Bouton "Jouer" / "Reprendre"
    public void Reprendre()
    {
        if (!estEnPause) return;
        RestaurerJeu(true);
    }

    private void RestaurerJeu(bool verrouillerSouris)
    {
        estEnPause = false;

        if (panneauPause != null) panneauPause.SetActive(false);
        if (panneauOptions != null) panneauOptions.SetActive(false);

        Time.timeScale = ancienneEchelleTemps;
        AudioListener.pause = audioEtaitEnPause;

        if (commandesJoueurEtaientActives && commandesJoueur != null)
            commandesJoueur.Enable();
        if (commandesZippoEtaientActives && commandesZippo != null)
            commandesZippo.Enable();
        if (cameraEtaitActive && cameraJoueur != null)
            cameraJoueur.enabled = true;

        Cursor.lockState = verrouillerSouris ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !verrouillerSouris;
        AnimerFlou(0f);
    }

    // Bouton "Options"
    public void OuvrirOptions()
    {
        if (!estEnPause || panneauOptions == null) return;
        if (panneauPause != null) panneauPause.SetActive(false);
        panneauOptions.SetActive(true);
    }

    // Bouton "Retour" du panneau Options
    public void FermerOptions()
    {
        if (!estEnPause) return;
        if (panneauOptions != null) panneauOptions.SetActive(false);
        if (panneauPause != null) panneauPause.SetActive(true);
    }

    // Bouton "Quitter" : retourne au menu principal, pas au bureau.
    public void QuitterVersMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneMenuPrincipal))
        {
            Debug.LogError("MenuPause : scene absente des Build Profiles : " + sceneMenuPrincipal, this);
            return;
        }

        if (estEnPause) RestaurerJeu(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(sceneMenuPrincipal);
    }

    private void AnimerFlou(float cible)
    {
        if (volumeFlou == null) return;
        if (animationFlou != null) StopCoroutine(animationFlou);
        animationFlou = StartCoroutine(FonduFlou(cible));
    }

    private IEnumerator FonduFlou(float cible)
    {
        float debut = volumeFlou.weight;
        float temps = 0f;
        float duree = Mathf.Max(0.001f, dureeFonduFlou);

        while (temps < duree)
        {
            temps += Time.unscaledDeltaTime; // fonctionne meme avec Time.timeScale = 0
            volumeFlou.weight = Mathf.Lerp(debut, cible, Mathf.Clamp01(temps / duree));
            yield return null;
        }

        volumeFlou.weight = cible;
        animationFlou = null;
    }
}
