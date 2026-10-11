
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

    [Header("Ecran de chargement")]
    [SerializeField] private GameObject panelChargement;
    [SerializeField] private CanvasGroup canvasGroupChargement;
    [SerializeField] private TMP_Text texteChargement;

    [Header("Astuces de chargement")]
    [SerializeField] private TMP_Text textetip;

    [TextArea(2, 4)]
    [SerializeField] private string[] astuces;

    [Header("Personnage de chargement")]
    [SerializeField] private RectTransform personnageCourse;
    [SerializeField] private RectTransform pointDepart;
    [SerializeField] private RectTransform pointArrivee;

    [Header("Reglages chargement")]
    [SerializeField] private string nomSceneJeu = "Scene_Demo";
    [SerializeField] private float dureeFondu = 1f;
    [SerializeField] private float attenteSecurite = 5f;
    [SerializeField] private float dureeCourseMinimale = 3f;

    [Header("Reglages camera")]
    [SerializeField] private float dureeDeplacement = 2f;

    [SerializeField] private AnimationCurve courbeMouvement =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private AsyncOperation chargementEnCours;
    private bool pretAContinuer = false;
    private bool transitionEnCours = false;

    private Button boutonContinuer;


    // ====================================
    // INITIALISATION DU MENU
    // ====================================

    private void Start()
    {
        // Afficher la souris dans le menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Initialisation du panneau de chargement
        canvasGroupChargement.alpha = 0f;
        canvasGroupChargement.blocksRaycasts = true;
        canvasGroupChargement.interactable = false;

        // Recuperer le bouton du panneau
        boutonContinuer = panelChargement.GetComponent<Button>();

        // Ajouter le bouton s'il n'existe pas
        if (boutonContinuer == null)
        {
            boutonContinuer = panelChargement.AddComponent<Button>();
        }

        boutonContinuer.transition = Selectable.Transition.None;

        boutonContinuer.targetGraphic =
            panelChargement.GetComponent<Image>();

        boutonContinuer.interactable = false;
        boutonContinuer.onClick.AddListener(Continuer);

        // Masquer le panneau au lancement
        panelChargement.SetActive(false);
    }


    // ====================================
    // DEPLACEMENT DE LA CAMERA
    // ====================================

    private IEnumerator DeplacerCamera(Transform destination)
    {
        Vector3 positionDepart = cameraMenu.position;
        Quaternion rotationDepart = cameraMenu.rotation;

        float temps = 0f;
        float duree = Mathf.Max(0.01f, dureeDeplacement);

        while (temps < duree)
        {
            temps += Time.deltaTime;

            float progression = Mathf.Clamp01(temps / duree);

            float progresLisse =
                courbeMouvement.Evaluate(progression);

            // Position de la camera
            cameraMenu.position = Vector3.Lerp(
                positionDepart,
                destination.position,
                progresLisse
            );

            // Rotation de la camera
            cameraMenu.rotation = Quaternion.Slerp(
                rotationDepart,
                destination.rotation,
                progresLisse
            );

            yield return null;
        }

        // Position et rotation exactes
        cameraMenu.position = destination.position;
        cameraMenu.rotation = destination.rotation;
    }


    // ====================================
    // FONDU PROGRESSIF AU NOIR
    // ====================================

    private IEnumerator FonduChargement()
    {
        float temps = 0f;
        float duree = Mathf.Max(0.01f, dureeFondu);

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
        // Verifier que des astuces existent
        if (astuces == null || astuces.Length == 0)
            return;

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
        // Eviter plusieurs clics successifs
        if (transitionEnCours)
            return;

        transitionEnCours = true;

        StartCoroutine(JouerEtCharger());
    }


    // ====================================
    // BOUTON QUITTER
    // ====================================

    public void Quitter()
    {
        // Eviter plusieurs clics successifs
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
        // Deplacer la camera vers la sortie
        yield return StartCoroutine(
            DeplacerCamera(cibleQuitter)
        );

        // Fermer le jeu apres l'animation
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

        // 3. Afficher le panneau
        panelChargement.SetActive(true);

        // 4. Choisir une astuce au hasard
        AfficherAstuceAleatoire();

        // 5. Fondu progressif au noir
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
        // Masquer la souris pendant le chargement
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        pretAContinuer = false;
        texteChargement.text = "CHARGEMENT...";

        // Verifier que le niveau existe
        if (!Application.CanStreamedLevelBeLoaded(nomSceneJeu))
        {
            Debug.LogError(
                "Scene introuvable : " + nomSceneJeu
            );

            texteChargement.text = "ERREUR DE CHARGEMENT";
            Cursor.visible = true;
            yield break;
        }

        // Demarrer le chargement asynchrone
        chargementEnCours =
            SceneManager.LoadSceneAsync(nomSceneJeu);

        if (chargementEnCours == null)
        {
            Debug.LogError("Chargement impossible.");

            texteChargement.text = "ERREUR DE CHARGEMENT";
            Cursor.visible = true;

            yield break;
        }

        // Ne pas activer la scene automatiquement
        chargementEnCours.allowSceneActivation = false;

        float progressionAffichee = 0f;

        float vitesseCourse =
            1f / Mathf.Max(0.01f, dureeCourseMinimale);

        // ====================================
        // PROGRESSION DU CHARGEMENT
        // ====================================

        while (chargementEnCours.progress < 0.9f)
        {
            // Progression reelle du chargement
            float progressionReelle = Mathf.Clamp01(
                chargementEnCours.progress / 0.9f
            );

            // Faire avancer le personnage progressivement
            progressionAffichee = Mathf.MoveTowards(
                progressionAffichee,
                progressionReelle,
                Time.unscaledDeltaTime * vitesseCourse
            );

            // Deplacer le personnage
            personnageCourse.position = Vector3.Lerp(
                pointDepart.position,
                pointArrivee.position,
                progressionAffichee
            );

            yield return null;
        }


        // ====================================
        // ATTENTE DE SECURITE : 5 SECONDES
        // ====================================

        float tempsAttente = 0f;

        while (tempsAttente < attenteSecurite ||
               progressionAffichee < 1f)
        {
            tempsAttente += Time.unscaledDeltaTime;

            // Terminer la course
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

        // Placer le personnage au point d'arrivee
        personnageCourse.position = pointArrivee.position;

        // ====================================
        // CHARGEMENT PRET
        // ====================================

        texteChargement.text = "CLIQUER POUR CONTINUER";

        // Autoriser le joueur a continuer
        pretAContinuer = true;

        canvasGroupChargement.interactable = true;
        boutonContinuer.interactable = true;

        // Faire reapparaitre la souris
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    // ====================================
    // CLIC POUR CONTINUER
    // ====================================

    public void Continuer()
    {
        // Verifier que le chargement est pret
        if (!pretAContinuer)
            return;

        if (chargementEnCours == null)
            return;

        // Bloquer les clics supplementaires
        pretAContinuer = false;
        boutonContinuer.interactable = false;

        // Masquer et verrouiller la souris
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Autoriser l'activation du niveau
        chargementEnCours.allowSceneActivation = true;
    }
}
