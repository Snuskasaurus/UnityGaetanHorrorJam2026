
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    [Header("Effet visuel du Zippo")]
    [SerializeField] private SpriteRenderer flammezippo;

    [Header("Etincelles du Zippo")]
    [SerializeField] private ParticleSystem etincellesZippo;
    [SerializeField] private Light flashEtincelle;

    private Coroutine routineFlash;

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
        EteindreFlamme();
        ArreterEffetsEtincelles();

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
                EteindreFlamme();

                Debug.Log("Zippo sorti.");
            }
            else
            {
                zippoAllume = false;

                EteindreFlamme();
                ArreterEffetsEtincelles();

                tentativeEnCours = false;
                tentativeReussie = false;

                animateur.ResetTrigger("ZippoOn");

                Debug.Log("Zippo range.");
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

    public void MasquerZippo()
    {
        if (rendusZippo != null)
        {
            foreach (Renderer rendu in rendusZippo)
            {
                if (rendu != null)
                {
                    rendu.enabled = false;
                }
            }
        }

        EteindreFlamme();
        ArreterEffetsEtincelles();
    }

    public void AfficherZippo()
    {
        if (rendusZippo == null)
            return;

        foreach (Renderer rendu in rendusZippo)
        {
            if (rendu != null && rendu != flammezippo)
            {
                rendu.enabled = true;
            }
        }
    }

    public void AllumerFlamme()
    {
        if (flammezippo != null)
        {
            flammezippo.enabled = true;
        }
    }

    public void EteindreFlamme()
    {
        if (flammezippo != null)
        {
            flammezippo.enabled = false;
        }
    }

    public void VerifierAllumage()
    {
        if (!tentativeEnCours || !zippoSorti)
            return;

        JouerEtincellesZippo();

        if (tentativeReussie)
        {
            zippoAllume = true;

            AllumerFlamme();

            if (audioZippo != null && sonReussite != null)
            {
                audioZippo.PlayOneShot(sonReussite);
            }

            Debug.Log("Zippo Allume ! Flamme activee !");
        }
        else
        {
            zippoAllume = false;

            EteindreFlamme();

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

    private void JouerEtincellesZippo()
    {
        if (etincellesZippo != null)
        {
            etincellesZippo.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            etincellesZippo.Play(true);
        }

        if (flashEtincelle != null)
        {
            if (routineFlash != null)
            {
                StopCoroutine(routineFlash);
                routineFlash = null;
            }

            routineFlash = StartCoroutine(
                FlashEtincelleCoroutine()
            );
        }
    }

    private IEnumerator FlashEtincelleCoroutine()
    {
        flashEtincelle.enabled = true;

        yield return new WaitForSeconds(0.05f);

        if (flashEtincelle != null)
        {
            flashEtincelle.enabled = false;
        }

        routineFlash = null;
    }

    private void ArreterEffetsEtincelles()
    {
        if (etincellesZippo != null)
        {
            etincellesZippo.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (routineFlash != null)
        {
            StopCoroutine(routineFlash);
            routineFlash = null;
        }

        if (flashEtincelle != null)
        {
            flashEtincelle.enabled = false;
        }
    }
}
