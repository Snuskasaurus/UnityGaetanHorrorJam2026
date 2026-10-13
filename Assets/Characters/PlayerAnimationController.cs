using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationController : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Animator animateur;
    [SerializeField] private PlayerController joueur;
    [SerializeField] private Rigidbody corpsJoueur;

    [Header("Zippo")]
    [SerializeField] private bool zippoSorti = false;

    void Start()
    {
        animateur = GetComponent<Animator>();
        joueur = GetComponentInParent<PlayerController>();
        corpsJoueur = GetComponentInParent<Rigidbody>();
        
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
            animateur.SetBool("ZippoOut", zippoSorti);
        }
        
    }
}
