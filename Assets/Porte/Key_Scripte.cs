
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Key_Scripte : MonoBehaviour
{
    [Header("CLE")]
    public string nomCle = "Cle_1";
    public Camera cameraJoueur;
    public float distanceRamassage = 3f;
    public bool faireDisparaitre = true;

    [Header("AFFICHAGE")]
    public string texteRamassage = "Ramasser la cle";

    [Header("SON RAMASSAGE")]
    public AudioClip sonRamassageCle;

    [Range(0f, 1f)]
    public float volumeRamassage = 1f;

    private static readonly HashSet<string> clesRamassees =
        new HashSet<string>();

    private bool visee;
    private InputAction actionRamassage;
    private bool avertissementAffiche;

    // ========================================
    // INITIALISATION
    // ========================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReinitialiserCles()
    {
        clesRamassees.Clear();
    }

    public static bool PossedeCle(string nom)
    {
        return !string.IsNullOrWhiteSpace(nom) &&
               clesRamassees.Contains(nom.Trim());
    }

    private void Start()
    {
        if (cameraJoueur == null)
            cameraJoueur = Camera.main;

        RecupererActionRamassage();
    }

    // ========================================
    // INPUT SYSTEM
    // ========================================

    private void RecupererActionRamassage()
    {
        if (actionRamassage != null)
            return;

        // Utilise le meme Input System que le joueur
        // et que le menu Options.
        InputAction actionLook = InputManager.PlayerLook;

        if (actionLook != null &&
            actionLook.actionMap != null &&
            actionLook.actionMap.asset != null)
        {
            actionRamassage =
                actionLook.actionMap.asset.FindAction(
                    "Player/Interact", false);
        }

        if (actionRamassage == null && !avertissementAffiche)
        {
            Debug.LogWarning(
                "Key_Scripte : action Player/Interact introuvable !",
                this
            );

            avertissementAffiche = true;
        }
    }

    private bool ToucheRamassageAppuyee()
    {
        if (actionRamassage == null)
            RecupererActionRamassage();

        return actionRamassage != null &&
               actionRamassage.enabled &&
               actionRamassage.WasPressedThisFrame();
    }

    // ========================================
    // AFFICHAGE DE LA TOUCHE
    // ========================================

    private string ObtenirToucheAffichee()
    {
        if (actionRamassage == null)
            RecupererActionRamassage();

        if (actionRamassage == null)
            return "?";

        // Recherche la touche clavier active,
        // y compris si elle a ete modifiee.
        for (int i = 0; i < actionRamassage.bindings.Count; i++)
        {
            string chemin =
                actionRamassage.bindings[i].effectivePath;

            if (string.IsNullOrEmpty(chemin))
                continue;

            if (chemin.StartsWith(
                "<Keyboard>/",
                System.StringComparison.OrdinalIgnoreCase))
            {
                return actionRamassage.GetBindingDisplayString(i);
            }
        }

        return actionRamassage.GetBindingDisplayString();
    }

    // ========================================
    // DETECTION DE LA CLE
    // ========================================

    private void Update()
    {
        visee = false;

        if (cameraJoueur == null || PossedeCle(nomCle))
            return;

        Ray rayon = new Ray(
            cameraJoueur.transform.position,
            cameraJoueur.transform.forward
        );

        if (!Physics.Raycast(
            rayon,
            out RaycastHit hit,
            distanceRamassage,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (hit.collider.GetComponentInParent<Key_Scripte>() != this)
            return;

        visee = true;

        if (ToucheRamassageAppuyee())
            RamasserCle();
    }

    // ========================================
    // RAMASSAGE
    // ========================================

    private void RamasserCle()
    {
        if (string.IsNullOrWhiteSpace(nomCle))
        {
            Debug.LogWarning(
                "Key_Scripte : le nom de la cle est vide !",
                this
            );
            return;
        }

        string identifiant = nomCle.Trim();

        if (!clesRamassees.Add(identifiant))
            return;

        visee = false;

        // Jouer le son meme si l'objet disparait.
        if (sonRamassageCle != null)
        {
            Vector3 positionSon = cameraJoueur != null
                ? cameraJoueur.transform.position
                : transform.position;

            AudioSource.PlayClipAtPoint(
                sonRamassageCle,
                positionSon,
                volumeRamassage
            );
        }

        Debug.Log("Cle ramassee : " + identifiant, this);

        if (faireDisparaitre)
            gameObject.SetActive(false);
    }

    // ========================================
    // TEXTE DE RAMASSAGE
    // ========================================

    private void OnGUI()
    {
        if (!visee)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.Max(16, Screen.height / 40);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string touche = ObtenirToucheAffichee();

        // Supprime une ancienne indication [E]
        // pour ne pas afficher deux touches.
        string description = texteRamassage;

        if (description.StartsWith("["))
        {
            int fin = description.IndexOf(']');

            if (fin >= 0)
                description = description.Substring(fin + 1).TrimStart();
        }

        string texteFinal = "[" + touche + "] " + description;

        GUI.Label(
            new Rect(
                Screen.width / 2f - 160,
                Screen.height * 0.65f,
                320,
                45
            ),
            texteFinal,
            style
        );
    }
}
