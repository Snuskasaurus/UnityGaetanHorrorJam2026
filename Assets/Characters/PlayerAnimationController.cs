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

    void Start()
    {
        animateur = GetComponent<Animator>();
        joueur = GetComponentInParent<PlayerController>();
        corpsJoueur = GetComponentInParent<Rigidbody>();

        rendusZippo = modelZippo.GetComponentsInChildren<Renderer>(true);

        MasquerZippo();

        Debug.Log("Animator utilisé : " + animateur.name);
        Debug.Log("Controller utilisé : " + animateur.runtimeAnimatorController?.name);

        foreach (AnimatorControllerParameter parametre in animateur.parameters)
        {
            Debug.Log("Paramètre trouvé : [" + parametre.name + "]");
        }
                
    }



    void Update()
    {
        Vector3 vitesse = corpsJoueur.linearVelocity;
        vitesse.y = 0f;

        float vitesseActuelle = vitesse.magnitude;

        animateur.SetFloat("WalkSpeed", vitesseActuelle);

        bool estEnCourse = joueur.CurrentMovementState == MovementState.Running;
        bool estAccroupi = joueur.CurrentPosture == Posture.Crouching;

        animateur.SetBool("IsRunning", estEnCourse);
        animateur.SetBool("IsCrouch", estAccroupi);

        
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            zippoSorti = !zippoSorti;

            if (zippoSorti)
            {
                AfficherZippo();
            }

            animateur.SetBool("ZippoOut", zippoSorti);
        }

    }

    public void MasquerZippo()
    {
        foreach (Renderer rendu in rendusZippo)
        {
            rendu.enabled = false;
        }
    }

    public void AfficherZippo()
    {
        foreach (Renderer rendu in rendusZippo)
        {
            rendu.enabled = true;
        }
    }
}

