
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Key_Scripte : MonoBehaviour
{
    [Header("CLE")]
    public string nomCle = "Cle_1";
    public Camera cameraJoueur;
    public float distanceRamassage = 3f;
    public bool faireDisparaitre = true;

    [Header("AFFICHAGE")]
    public string texteRamassage = "[E] Ramasser la cle";

    [Header("SON RAMASSAGE")]
    public AudioClip sonRamassageCle;

    [Range(0f, 1f)]
    public float volumeRamassage = 1f;

    private static readonly HashSet<string> clesRamassees =
        new HashSet<string>();

    private bool visee;

    // Reinitialisation au lancement de chaque session.
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

    void Start()
    {
        if (cameraJoueur == null)
            cameraJoueur = Camera.main;
    }

    void Update()
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
            return;

        if (hit.collider.GetComponentInParent<Key_Scripte>() != this)
            return;

        visee = true;

        if (ToucheEAppuyee())
            RamasserCle();
    }

    void RamasserCle()
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

        // Joue le son independamment de l'objet cle.
        // Cela evite que le son s'arrete lorsque
        // la cle disparait de la scene.
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

    void OnGUI()
    {
        if (!visee)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.Max(16, Screen.height / 40);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(
                Screen.width / 2f - 160,
                Screen.height * 0.65f,
                320,
                45
            ),
            texteRamassage,
            style
        );
    }

    bool ToucheEAppuyee()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
