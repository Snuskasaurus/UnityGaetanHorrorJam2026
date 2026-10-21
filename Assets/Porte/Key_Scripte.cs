using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Key_Scripte : MonoBehaviour
{
    [Header("CLE")]
    [Tooltip("Identifiant unique, identique a celui de la porte")]
    public string nomCle = "Cle_Infirmerie";
    public Camera cameraJoueur;
    public float distanceRamassage = 3f;
    public bool faireDisparaitre = true;

    [Header("AFFICHAGE")]
    public string texteRamassage = "[E] Ramasser la cle";

    // Inventaire de cles partage entre les objets de la scene.
    private static readonly HashSet<string> clesRamassees = new HashSet<string>();
    // Vide l'inventaire au lancement d'une nouvelle session, meme si
    // Unity utilise Enter Play Mode Options sans Domain Reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReinitialiserCles()
    {
        clesRamassees.Clear();
    }

    private bool visee;

    public static bool PossedeCle(string nom)
    {
        return !string.IsNullOrWhiteSpace(nom) && clesRamassees.Contains(nom.Trim());
    }

    private void Start()
    {
        if (cameraJoueur == null) cameraJoueur = Camera.main;
        // Le modele doit posseder un Collider (ou un de ses enfants).
    }

    private void Update()
    {
        visee = false;
        if (cameraJoueur == null || PossedeCle(nomCle)) return;

        Ray rayon = new Ray(cameraJoueur.transform.position, cameraJoueur.transform.forward);
        if (!Physics.Raycast(rayon, out RaycastHit hit, distanceRamassage,
                ~0, QueryTriggerInteraction.Ignore)) return;

        // Un collider sur la cle ou un de ses enfants.
        if (hit.collider.GetComponentInParent<Key_Scripte>() != this) return;

        visee = true;
        if (ToucheEAppuyee())
        {
            if (string.IsNullOrWhiteSpace(nomCle))
            {
                Debug.LogWarning("Key_Scripte : nomCle est vide !", this);
                return;
            }
            clesRamassees.Add(nomCle.Trim());
            visee = false;
            Debug.Log("Cle ramassee : " + nomCle);
            if (faireDisparaitre) gameObject.SetActive(false);
        }
    }

    private void OnGUI()
    {
        if (!visee) return;
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(16, Screen.height / 40),
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width / 2f - 160, Screen.height * 0.65f,
            320, 45), texteRamassage, style);
    }

    private bool ToucheEAppuyee()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
