
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Camera et destinations")]
    [SerializeField] private Transform cameraMenu;
    [SerializeField] private Transform cibleJouer;
    [SerializeField] private Transform cibleQuitter;

    [Header("Vitesse du deplacement de la camera")]
    [SerializeField, Min(0.1f)]
    private float vitesseCamera = 1f;

    [Header("Menu Options")]
    [SerializeField] private GameObject panneauOptions;

    [Header("Ecran de chargement")]
    [SerializeField] private GameObject panelChargement;
    [SerializeField] private CanvasGroup canvasGroupChargement;
    [SerializeField] private TMP_Text texteChargement;

    // ====================================
    // ASTUCES MODIFIABLES DANS L'INSPECTOR
    // ====================================

    [Header("Astuces de chargement")]
    [SerializeField] private TMP_Text textetip;

    [TextArea(2, 4)]
    [SerializeField] private string[] astuces;

    // Parametres du menu
    private MenuSettings Settings => GameSettings.Menu;

    [Header("Personnage de chargement")]
    [SerializeField] private RectTransform personnageCourse;
    [SerializeField] private RectTransform pointDepart;
    [SerializeField] private RectTransform pointArrivee;

    private AsyncOperation chargementEnCours;
    private bool pretAContinuer = false;
    private bool transitionEnCours = false;

    private Button boutonContinuer;

    // ====================================
    // INITIALISATION DU MENU
    // ====================================

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasGroupChargement.alpha = 0f;
        canvasGroupChargement.blocksRaycasts = true;
        canvasGroupChargement.interactable = false;

        boutonContinuer = panelChargement.GetComponent<Button>();

        if (boutonContinuer == null)
        {
            boutonContinuer = panelChargement.AddComponent<Button>();
        }

        boutonContinuer.transition = Selectable.Transition.None;

        boutonContinuer.targetGraphic =
            panelChargement.GetComponent<Image>();

        boutonContinuer.interactable = false;
        boutonContinuer.onClick.AddListener(Continuer);

        panelChargement.SetActive(false);

        if (panneauOptions != null)
            panneauOptions.SetActive(false);
    }

    // ====================================
    // DEPLACEMENT DE LA CAMERA
    // ====================================

    private IEnumerator DeplacerCamera(Transform destination)
    {
        Vector3 positionDepart = cameraMenu.position;
        Quaternion rotationDepart = cameraMenu.rotation;

        float temps = 0f;

        float duree = Mathf.Max(
            0.01f,
            Settings.dureeDeplacement /
            Mathf.Max(0.1f, vitesseCamera)
        );

        while (temps < duree)
        {
            temps += Time.deltaTime;

            float progression = Mathf.Clamp01(temps / duree);

            float progresLisse =
                Settings.courbeMouvement.Evaluate(progression);

            cameraMenu.position = Vector3.Lerp(
                positionDepart,
                destination.position,
                progresLisse
            );

            cameraMenu.rotation = Quaternion.Slerp(
                rotationDepart,
                destination.rotation,
                progresLisse
            );

            yield return null;
        }

        cameraMenu.position = destination.position;
        cameraMenu.rotation = destination.rotation;
    }

    // ====================================
    // FONDU PROGRESSIF AU NOIR
    // ====================================

    private IEnumerator FonduChargement()
    {
        float temps = 0f;
        float duree = Mathf.Max(0.01f, Settings.dureeFondu);

        canvasGroupChargement.alpha = 0f;

        while (temps < duree)
        {
            temps += Time.deltaTime;

            float progression = Mathf.Clamp01(
                temps / duree
            );

            canvasGroupChargement.alpha = Mathf.Lerp(
                0f,
                1f,
                progression
            );

            yield return null;
        }

        canvasGroupChargement.alpha = 1f;
    }

    // ====================================
    // ASTUCES ALEATOIRES
    // ====================================

    private void AfficherAstuceAleatoire()
    {
        // Les astuces viennent de l'Inspector
        if (astuces == null ||
            astuces.Length == 0 ||
            textetip == null)
        {
            return;
        }

        // Choisir une astuce au hasard
        int index = Random.Range(0, astuces.Length);

        // Afficher l'astuce
        textetip.text = astuces[index];
    }

    // ====================================
    // BOUTON JOUER
    // ====================================

    public void Jouer()
    {
        if (transitionEnCours)
            return;

        transitionEnCours = true;

        StartCoroutine(JouerEtCharger());
    }

    // ====================================
    // BOUTON OPTIONS
    // ====================================

    public void Options()
    {
        if (transitionEnCours || panneauOptions == null)
            return;

        panneauOptions.SetActive(true);
    }

    public void FermerOptions()
    {
        if (panneauOptions != null)
            panneauOptions.SetActive(false);
    }

    // ====================================
    // BOUTON QUITTER
    // ====================================

    public void Quitter()
    {
        if (transitionEnCours)
            return;

        transitionEnCours = true;

        StartCoroutine(QuitterApresAnimation());
    }

    // ====================================
    // ANIMATION AVANT DE QUITTER
    // ====================================

    private IEnumerator QuitterApresAnimation()
    {
        yield return StartCoroutine(
            DeplacerCamera(cibleQuitter)
        );

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ====================================
    // ENCHAINEMENT DU MENU JOUER
    // ====================================

    private IEnumerator JouerEtCharger()
    {
        // 1. Deplacer la camera
        yield return StartCoroutine(
            DeplacerCamera(cibleJouer)
        );

        // 2. Placer le personnage a gauche
        personnageCourse.position = pointDepart.position;

        // 3. Afficher le panneau de chargement
        panelChargement.SetActive(true);

        // 4. Afficher une astuce aleatoire
        AfficherAstuceAleatoire();

        // 5. Fondu progressif
        yield return StartCoroutine(
            FonduChargement()
        );

        // 6. Charger le niveau
        yield return StartCoroutine(
            ChargerNiveau()
        );
    }

    // ====================================
    // CHARGEMENT ASYNCHRONE DU NIVEAU
    // ====================================

    private IEnumerator ChargerNiveau()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        pretAContinuer = false;

        texteChargement.text = "CHARGEMENT...";

        // Verifier que la scene existe
        if (!Application.CanStreamedLevelBeLoaded(
            Settings.nomSceneJeu))
        {
            texteChargement.text = "ERREUR DE CHARGEMENT";

            Cursor.visible = true;
            yield break;
        }

        // Demarrer le chargement
        chargementEnCours =
            SceneManager.LoadSceneAsync(Settings.nomSceneJeu);

        if (chargementEnCours == null)
        {
            texteChargement.text = "ERREUR DE CHARGEMENT";

            Cursor.visible = true;
            yield break;
        }

        // Attendre l'autorisation du joueur
        chargementEnCours.allowSceneActivation = false;

        float progressionAffichee = 0f;

        float vitesseCourse =
            1f / Mathf.Max(0.01f, Settings.dureeCourseMinimale);

        // ====================================
        // PROGRESSION DU CHARGEMENT
        // ====================================

        while (chargementEnCours.progress < 0.9f)
        {
            float progressionReelle = Mathf.Clamp01(
                chargementEnCours.progress / 0.9f
            );

            progressionAffichee = Mathf.MoveTowards(
                progressionAffichee,
                progressionReelle,
                Time.unscaledDeltaTime * vitesseCourse
            );

            personnageCourse.position = Vector3.Lerp(
                pointDepart.position,
                pointArrivee.position,
                progressionAffichee
            );

            yield return null;
        }

        // ====================================
        // ATTENTE DE SECURITE
        // ====================================

        float tempsAttente = 0f;

        while (tempsAttente < Settings.attenteSecurite ||
               progressionAffichee < 1f)
        {
            tempsAttente += Time.unscaledDeltaTime;

            progressionAffichee = Mathf.MoveTowards(
                progressionAffichee,
                1f,
                Time.unscaledDeltaTime * vitesseCourse
            );

            personnageCourse.position = Vector3.Lerp(
                pointDepart.position,
                pointArrivee.position,
                progressionAffichee
            );

            yield return null;
        }

        personnageCourse.position = pointArrivee.position;

        // ====================================
        // CHARGEMENT PRET
        // ====================================

        texteChargement.text = "CLIQUER POUR CONTINUER";

        pretAContinuer = true;

        canvasGroupChargement.interactable = true;
        boutonContinuer.interactable = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ====================================
    // CLIC POUR CONTINUER
    // ====================================

    public void Continuer()
    {
        if (!pretAContinuer)
            return;

        if (chargementEnCours == null)
            return;

        pretAContinuer = false;
        boutonContinuer.interactable = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        chargementEnCours.allowSceneActivation = true;
    }
}
