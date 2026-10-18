
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PorteInteractive : MonoBehaviour
{
    [Header("MODELES 3D")]
    public Transform modelePorte;
    public Transform contourPorte; // Facultatif

    [Header("JOUEUR")]
    public Camera cameraJoueur;
    public CameraController scriptRotationCamera;
    // Tuning values, read from the DoorSettings asset of GameSettings
    private DoorSettings Settings => GameSettings.Door;

    private Rigidbody rb;
    private HingeJoint charniere;
    private bool interaction;
    private bool cameraEtaitActive;
    private float angleCible;
    private float sensInteraction;

    void Start()
    {
        if (modelePorte == null)
        {
            Debug.LogError("Aucun modele de porte assigne !");
            enabled = false;
            return;
        }

        if (cameraJoueur == null)
            cameraJoueur = Camera.main;

        // Colliders automatiques.
        AjouterColliders(modelePorte, true);

        //if (contourPorte != null)
          //  AjouterColliders(contourPorte, false);

        // Rigidbody directement sur le modele.
        rb = modelePorte.GetComponent<Rigidbody>();

        if (rb == null)
            rb = modelePorte.gameObject.AddComponent<Rigidbody>();

        rb.mass = Settings.masse;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        
        charniere = modelePorte.GetComponent<HingeJoint>();

        if (charniere == null)
            charniere = modelePorte.gameObject.AddComponent<HingeJoint>();

        charniere.connectedBody = null;

        // Utilise le pivot existant du modele Blender.
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
            if (ClicAppuye())
                EssayerAttraper();

            return;
        }

        if (!ClicMaintenu())
        {
            Relacher();
            return;
        }

        float mouvement = SourisY();

        angleCible +=
            mouvement * Settings.sensibiliteSouris * sensInteraction;

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
            return;

        
        if (hit.collider.attachedRigidbody != rb)
            return;

        interaction = true;
        angleCible = charniere.angle;

        // Determine de quel cote de la porte est le joueur.
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
            cameraJoueur.transform.position - modelePorte.position;

        sensInteraction =
            Vector3.Dot(versJoueur, normale) >= 0f ? 1f : -1f;

        if (Settings.inverserSens)
            sensInteraction *= -1f;

        
        if (scriptRotationCamera != null)
        {
            cameraEtaitActive = scriptRotationCamera.enabled;
            scriptRotationCamera.enabled = false;
        }
    }

    void Relacher()
    {
        interaction = false;

        // La porte reste vers l'angle atteint.
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

    void AjouterColliders(Transform racine, bool mobile)
    {
        MeshFilter[] meshes =
            racine.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mesh in meshes)
        {
            if (mesh.sharedMesh == null)
                continue;

            if (mesh.GetComponent<Collider>() != null)
                continue;

            if (mobile)
            {
                BoxCollider box =
                    mesh.gameObject.AddComponent<BoxCollider>();

                box.center = mesh.sharedMesh.bounds.center;
                box.size = mesh.sharedMesh.bounds.size;
            }
            else
            {
                MeshCollider collider =
                    mesh.gameObject.AddComponent<MeshCollider>();

                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = false;
            }
        }
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