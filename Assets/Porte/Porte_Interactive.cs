
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PorteInteractive : MonoBehaviour
{
    [Header("MODELES 3D")]
    public Transform modelePorte;
    public Transform contourPorte;

    [Header("JOUEUR")]
    public Camera cameraJoueur;
    public CameraController scriptRotationCamera;

    [Header("VERROUILLAGE")]
    public bool porteBloquee = false;
    public string nomCleNecessaire = "Cle_1";

    [Header("OBJET 3D APRES DEVERROUILLAGE")]
    public GameObject objetApresDeverrouillage;

    [Header("INTERACTION E")]
    public string texteDeverrouillage =
        "[E] Deverrouiller la porte";

    [Header("SONS")]
    public AudioClip sonPorteBloquee;
    public AudioClip sonOuverture;
    public AudioClip sonDeverrouillage;

    [Range(0f, 1f)]
    public float volumeSons = 1f;

    public AudioSource sourceAudio;

    private DoorSettings Settings => GameSettings.Door;

    private Rigidbody rb;
    private HingeJoint charniere;

    private bool interaction;
    private bool deverrouillePendantPartie;
    private bool cameraEtaitActive;
    private bool peutDeverrouiller;

    private float angleCible;
    private float sensInteraction;

    void Start()
    {
        if (modelePorte == null)
        {
            Debug.LogError(
                "Aucun modele de porte assigne !",
                this
            );
            enabled = false;
            return;
        }

        if (cameraJoueur == null)
            cameraJoueur = Camera.main;

        deverrouillePendantPartie = false;

        // L'objet affiche apres deverrouillage
        // doit rester uniquement graphique.
        if (objetApresDeverrouillage != null)
        {
            Transform visuel =
                objetApresDeverrouillage.transform;

            if (visuel == modelePorte ||
                modelePorte.IsChildOf(visuel))
            {
                Debug.LogError(
                    "Le visuel ne doit pas etre la porte ou son parent.",
                    this
                );

                objetApresDeverrouillage = null;
            }
            else
            {
                RendreObjetPurementVisuel(
                    objetApresDeverrouillage
                );

                if (porteBloquee)
                    objetApresDeverrouillage.SetActive(false);
            }
        }

        // Colliders de la vraie porte uniquement.
        AjouterColliders(modelePorte);

        rb = modelePorte.GetComponent<Rigidbody>();

        if (rb == null)
            rb = modelePorte.gameObject.AddComponent<Rigidbody>();

        rb.mass = Settings.masse;
        rb.useGravity = false;
        rb.isKinematic = porteBloquee;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode = porteBloquee
            ? CollisionDetectionMode.ContinuousSpeculative
            : CollisionDetectionMode.ContinuousDynamic;

        charniere = modelePorte.GetComponent<HingeJoint>();

        if (charniere == null)
        {
            charniere =
                modelePorte.gameObject.AddComponent<HingeJoint>();
        }

        charniere.connectedBody = null;
        charniere.anchor = Vector3.zero;

        charniere.axis =
            modelePorte.InverseTransformDirection(Vector3.up);

        charniere.autoConfigureConnectedAnchor = false;
        charniere.connectedAnchor = modelePorte.position;

        JointLimits limites = charniere.limits;

        limites.min = Settings.angleMinimum;
        limites.max = Settings.angleMaximum;

        charniere.limits = limites;
        charniere.useLimits = true;

        JointSpring ressort = charniere.spring;

        ressort.spring = Settings.force;
        ressort.damper = Settings.amortissement;
        ressort.targetPosition = 0f;

        charniere.spring = ressort;
        charniere.useSpring = true;
    }

    void Update()
    {
        if (cameraJoueur == null || charniere == null)
            return;

        if (!interaction)
        {
            peutDeverrouiller = false;

            if (porteBloquee &&
                !deverrouillePendantPartie &&
                ViseCettePorte())
            {
                peutDeverrouiller =
                    Key_Scripte.PossedeCle(nomCleNecessaire);

                if (peutDeverrouiller && ToucheEAppuyee())
                    Deverrouiller();
            }

            if (ClicAppuye())
                EssayerAttraper();

            return;
        }

        peutDeverrouiller = false;

        if (!ClicMaintenu())
        {
            Relacher();
            return;
        }

        float mouvement = SourisY();

        angleCible +=
            mouvement *
            Settings.sensibiliteSouris *
            sensInteraction;

        angleCible = Mathf.Clamp(
            angleCible,
            Settings.angleMinimum,
            Settings.angleMaximum
        );

        JointSpring ressort = charniere.spring;

        ressort.targetPosition = angleCible;
        ressort.spring = Settings.force;
        ressort.damper = Settings.amortissement;

        charniere.spring = ressort;
    }

    void EssayerAttraper()
    {
        if (!ViseCettePorte())
            return;

        // La porte reste bloquee tant que
        // le joueur ne l'a pas deverrouillee avec E.
        if (porteBloquee && !deverrouillePendantPartie)
        {
            JouerSon(sonPorteBloquee);
            return;
        }

        JouerSon(sonOuverture);

        interaction = true;
        angleCible = charniere.angle;

        Vector3 direction =
            rb.worldCenterOfMass - modelePorte.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = modelePorte.right;
            direction.y = 0f;
        }

        Vector3 normale = Vector3.Cross(
            Vector3.up,
            direction.normalized
        );

        Vector3 versJoueur =
            cameraJoueur.transform.position -
            modelePorte.position;

        sensInteraction =
            Vector3.Dot(versJoueur, normale) >= 0f
            ? 1f : -1f;

        if (Settings.inverserSens)
            sensInteraction *= -1f;

        if (scriptRotationCamera != null)
        {
            cameraEtaitActive = scriptRotationCamera.enabled;
            scriptRotationCamera.enabled = false;
        }
    }

    bool ViseCettePorte()
    {
        if (cameraJoueur == null || rb == null)
            return false;

        Ray rayon = new Ray(
            cameraJoueur.transform.position,
            cameraJoueur.transform.forward
        );

        if (!Physics.Raycast(
            rayon,
            out RaycastHit hit,
            Settings.distanceInteraction,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.collider.attachedRigidbody == rb;
    }

    void Deverrouiller()
    {
        if (!porteBloquee || deverrouillePendantPartie)
            return;

        if (!Key_Scripte.PossedeCle(nomCleNecessaire))
            return;

        deverrouillePendantPartie = true;

        rb.isKinematic = false;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.WakeUp();

        // NOUVEAU : son de la cle dans la serrure.
        JouerSon(sonDeverrouillage);

        // Afficher le modele 3D decoratif.
        if (objetApresDeverrouillage != null)
        {
            RendreObjetPurementVisuel(
                objetApresDeverrouillage
            );

            objetApresDeverrouillage.SetActive(true);
        }

        peutDeverrouiller = false;

        Debug.Log(
            "Porte deverrouillee avec : " + nomCleNecessaire,
            this
        );
    }

    void RendreObjetPurementVisuel(GameObject objet)
    {
        if (objet == null)
            return;

        // Desactiver les colliders existants.
        Collider[] colliders =
            objet.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
            col.enabled = false;

        // Desactiver la physique des Rigidbody du visuel.
        Rigidbody[] corps =
            objet.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody corpsRigide in corps)
        {
            corpsRigide.isKinematic = true;
            corpsRigide.detectCollisions = false;
        }
    }

    void AjouterColliders(Transform racine)
    {
        MeshFilter[] meshes =
            racine.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mesh in meshes)
        {
            // Ne jamais creer de collider sur le
            // modele graphique de deverrouillage.
            if (objetApresDeverrouillage != null)
            {
                Transform visuel =
                    objetApresDeverrouillage.transform;

                if (mesh.transform == visuel ||
                    mesh.transform.IsChildOf(visuel))
                {
                    continue;
                }
            }

            if (mesh.sharedMesh == null)
                continue;

            if (mesh.GetComponent<Collider>() != null)
                continue;

            BoxCollider box =
                mesh.gameObject.AddComponent<BoxCollider>();

            box.center = mesh.sharedMesh.bounds.center;
            box.size = mesh.sharedMesh.bounds.size;
        }
    }

    void Relacher()
    {
        interaction = false;

        JointSpring ressort = charniere.spring;

        ressort.targetPosition = charniere.angle;
        ressort.spring = Settings.force * 0.25f;
        ressort.damper = Settings.amortissement;

        charniere.spring = ressort;

        if (scriptRotationCamera != null)
            scriptRotationCamera.enabled = cameraEtaitActive;
    }

    void OnDisable()
    {
        if (interaction && charniere != null)
            Relacher();
    }

    void OnGUI()
    {
        if (!peutDeverrouiller || interaction)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.Max(16, Screen.height / 40);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(
                Screen.width / 2f - 200,
                Screen.height * 0.65f,
                400,
                45
            ),
            texteDeverrouillage,
            style
        );
    }

    void JouerSon(AudioClip clip)
    {
        if (clip == null)
            return;

        if (sourceAudio == null)
        {
            sourceAudio = GetComponent<AudioSource>();

            if (sourceAudio == null)
            {
                sourceAudio =
                    gameObject.AddComponent<AudioSource>();
            }

            sourceAudio.playOnAwake = false;
            sourceAudio.spatialBlend = 1f;
        }

        sourceAudio.PlayOneShot(clip, volumeSons);
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

    bool ClicAppuye()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    bool ClicMaintenu()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    float SourisY()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.delta.ReadValue().y
            : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetAxisRaw("Mouse Y") * 12f;
#else
        return 0f;
#endif
    }
}
