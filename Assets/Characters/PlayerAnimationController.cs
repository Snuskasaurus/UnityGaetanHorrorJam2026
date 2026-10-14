
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animateur;
    [SerializeField] private PlayerController joueur;
    [SerializeField] private Rigidbody corpsJoueur;
    [SerializeField] private GameObject modelZippo;

    private Renderer[] rendusZippo;

    [Header("Zippo")]
    [SerializeField] private bool zippoSorti = false;

    [Header("Allumage du Zippo")]
    [SerializeField] private bool zippoAllume = false;

    [Range(0f, 1f)]
    [SerializeField] private float chanceAllumage = 0.35f;

    private bool tentativeEnCours = false;
    private bool tentativeReussie = false;

    [Header("Sons du Zippo")]
    [SerializeField] private AudioSource audioZippo;
    [SerializeField] private AudioClip sonEchec1;
    [SerializeField] private AudioClip sonEchec2;
    [SerializeField] private AudioClip sonReussite;



    void Start()
    {
        animateur = GetComponent<Animator>();
        joueur = GetComponentInParent<PlayerController>();
        corpsJoueur = GetComponentInParent<Rigidbody>();

        if (animateur == null ||
            joueur == null ||
            corpsJoueur == null ||
            modelZippo == null)
        {
            Debug.LogError(
                "PlayerAnimationController : reference manquante !",
                this
            );

            enabled = false;
            return;
        }

        rendusZippo =
            modelZippo.GetComponentsInChildren<Renderer>(true);

        zippoSorti = false;
        zippoAllume = false;
        tentativeEnCours = false;
        tentativeReussie = false;

        MasquerZippo();

        animateur.SetBool("ZippoOut", false);

        Debug.Log("Animator utilise : " + animateur.name);
        Debug.Log("Controller utilise : " +
            animateur.runtimeAnimatorController?.name);

        foreach (AnimatorControllerParameter parametre
            in animateur.parameters)
        {
            Debug.Log(
                "Parametre trouve : [" + parametre.name + "]"
            );
        }
    }



    void Update()
    {


        Vector3 vitesse = corpsJoueur.linearVelocity;
        vitesse.y = 0f;

        float vitesseActuelle = vitesse.magnitude;

        animateur.SetFloat("WalkSpeed", vitesseActuelle);




        bool estEnCourse =
            joueur.CurrentMovementState == MovementState.Running;

        bool estAccroupi =
            joueur.CurrentPosture == Posture.Crouching;

        animateur.SetBool("IsRunning", estEnCourse);
        animateur.SetBool("IsCrouch", estAccroupi);




        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            zippoSorti = !zippoSorti;

            if (zippoSorti)
            {

                AfficherZippo();
            }
            else
            {

                zippoAllume = false;


                tentativeEnCours = false;
                tentativeReussie = false;


            }

            animateur.SetBool("ZippoOut", zippoSorti);
        }




        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (zippoSorti &&
                !zippoAllume &&
                !tentativeEnCours)
            {
                tentativeEnCours = true;


                tentativeReussie =
                    Random.value < chanceAllumage;


                animateur.SetTrigger("ZippoOn");

                Debug.Log("Tentative d'allumage du Zippo...");
            }
        }
    }


    // MASQUER LE MODELE 3D DU ZIPPO

    public void MasquerZippo()
    {
        if (rendusZippo == null)
            return;

        foreach (Renderer rendu in rendusZippo)
        {
            if (rendu != null)
            {
                rendu.enabled = false;
            }
        }
    }




    public void AfficherZippo()
    {
        if (rendusZippo == null)
            return;

        foreach (Renderer rendu in rendusZippo)
        {
            if (rendu != null)
            {
                rendu.enabled = true;
            }
        }
    }




    public void VerifierAllumage()
    {
        if (!tentativeEnCours || !zippoSorti)
            return;

        if (tentativeReussie)
        {


            zippoAllume = true;

            if (audioZippo != null && sonReussite != null)
            {
                audioZippo.PlayOneShot(sonReussite);
            }

            Debug.Log("Zippo Allume !");
        }
        else
        {


            int choixSon = Random.Range(0, 2);

            AudioClip sonChoisi;

            if (choixSon == 0)
            {
                sonChoisi = sonEchec1;
            }
            else
            {
                sonChoisi = sonEchec2;
            }

            if (audioZippo != null && sonChoisi != null)
            {
                audioZippo.PlayOneShot(sonChoisi);
            }

            Debug.Log("Echec de l'allumage !");
        }
    }




    public void TerminerTentative()
    {
        tentativeEnCours = false;
        tentativeReussie = false;

        Debug.Log("Tentative terminee.");
    }
}
