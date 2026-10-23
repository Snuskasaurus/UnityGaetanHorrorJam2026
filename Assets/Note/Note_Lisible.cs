using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Note_Lisible : MonoBehaviour
{
    [Header("INTERACTION")]
    public Camera cameraJoueur;
    public float distanceLecture = 3f;
    public string texteInteraction = "[E] Lire la note";

    [Header("IMAGE DE LA NOTE")]
    public Texture2D imageParchemin;

    [Header("CONTENU")]
    public string titreNote = "Journal du soldat";

    [TextArea(8, 30)]
    public string contenuNote =
        "21 octobre 1916\n\n" +
        "Nous entendons des bruits etranges\n" +
        "derriere les murs du bunker...";

    [Header("POLICE PIXEL")]
    public Font policePixel;
    public Color couleurTexte = new Color(0.16f, 0.11f, 0.08f, 1f);

    [Range(10, 80)]
    public int tailleTitre = 30;

    [Range(8, 60)]
    public int tailleContenu = 18;

    [Range(8, 50)]
    public int tailleTexteInteraction = 18;

    [Header("MISE EN PAGE")]
    [Range(0.2f, 1f)]
    public float largeurImageEcran = 0.38f;

    [Range(0.2f, 1f)]
    public float hauteurImageEcran = 0.82f;

    [Range(0f, 0.4f)]
    public float margeGauche = 0.15f;

    [Range(0f, 0.4f)]
    public float margeDroite = 0.15f;

    [Range(0f, 0.4f)]
    public float margeHaut = 0.13f;

    [Range(0f, 0.4f)]
    public float margeBas = 0.16f;

    [Range(-200f, 200f)]
    public float decalageTitreY = 0f;

    [Range(-200f, 200f)]
    public float decalageContenuY = 10f;

    [Header("FLOU")]
    [Range(4, 24)]
    public int reductionResolution = 12;

    [Range(1, 5)]
    public int intensiteFlou = 3;

    [Header("SONS")]
    public AudioClip sonPrendreNote;
    public AudioClip sonReposerNote;

    [Range(0f, 1f)]
    public float volumeSons = 1f;

    private bool joueurRegarde;
    private bool lectureActive;
    private bool ouvertureEnCours;

    private float ancienTimeScale;
    private bool ancienEtatCamera;
    private bool anciennePauseAudio;

    private CursorLockMode ancienCursorLock;
    private bool ancienCursorVisible;

    private CameraController controleCamera;
    private Texture2D fondFlou;
    private Vector2 scrollTexte;
    private AudioSource audioSource;

#if ENABLE_INPUT_SYSTEM
    // Meme action que le joueur et le menu Options : Player/Interact.
    private InputAction actionInteraction;
#endif

    void Start()
    {
        if (cameraJoueur == null)
            cameraJoueur = Camera.main;

        if (cameraJoueur != null)
            controleCamera = cameraJoueur.GetComponentInParent<CameraController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

#if ENABLE_INPUT_SYSTEM
        RecupererActionInteraction();
#endif
    }

    void Update()
    {
        if (lectureActive)
        {
            if (ToucheInteraction())
                FermerNote();

            return;
        }

        if (ouvertureEnCours)
            return;

        joueurRegarde = RegardeCetteNote();

        if (joueurRegarde && ToucheInteraction())
            StartCoroutine(OuvrirNote());
    }

    bool RegardeCetteNote()
    {
        if (cameraJoueur == null)
            return false;

        Ray rayon = new Ray(
            cameraJoueur.transform.position,
            cameraJoueur.transform.forward
        );

        if (!Physics.Raycast(
            rayon,
            out RaycastHit hit,
            distanceLecture,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.collider.transform == transform ||
               hit.collider.transform.IsChildOf(transform);
    }

    IEnumerator OuvrirNote()
    {
        ouvertureEnCours = true;
        joueurRegarde = false;

        yield return new WaitForEndOfFrame();

        Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();

        if (capture != null)
        {
            CreerFondFlou(capture);
            Destroy(capture);
        }

        ancienTimeScale = Time.timeScale;
        anciennePauseAudio = AudioListener.pause;

        ancienCursorLock = Cursor.lockState;
        ancienCursorVisible = Cursor.visible;

        if (controleCamera != null)
        {
            ancienEtatCamera = controleCamera.enabled;
            controleCamera.enabled = false;
        }

        JouerSon(sonPrendreNote);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        scrollTexte = Vector2.zero;
        lectureActive = true;
        ouvertureEnCours = false;
    }

    void FermerNote()
    {
        lectureActive = false;

        Time.timeScale = ancienTimeScale;
        AudioListener.pause = anciennePauseAudio;

        if (controleCamera != null)
            controleCamera.enabled = ancienEtatCamera;

        Cursor.lockState = ancienCursorLock;
        Cursor.visible = ancienCursorVisible;

        joueurRegarde = false;

        JouerSon(sonReposerNote);

        if (fondFlou != null)
        {
            Destroy(fondFlou);
            fondFlou = null;
        }
    }

    void OnDisable()
    {
        if (lectureActive)
            FermerNote();

        if (fondFlou != null)
        {
            Destroy(fondFlou);
            fondFlou = null;
        }
    }

    void CreerFondFlou(Texture2D capture)
    {
        if (fondFlou != null)
        {
            Destroy(fondFlou);
            fondFlou = null;
        }

        int largeur = Mathf.Max(16, capture.width / reductionResolution);
        int hauteur = Mathf.Max(16, capture.height / reductionResolution);

        RenderTexture rt = RenderTexture.GetTemporary(
            largeur, hauteur, 0, RenderTextureFormat.ARGB32
        );

        RenderTexture precedent = RenderTexture.active;

        Graphics.Blit(capture, rt);
        RenderTexture.active = rt;

        Texture2D petiteImage = new Texture2D(
            largeur, hauteur, TextureFormat.RGBA32, false
        );

        petiteImage.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0);
        petiteImage.Apply();

        RenderTexture.active = precedent;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = petiteImage.GetPixels32();

        for (int passage = 0; passage < intensiteFlou; passage++)
        {
            Color32[] resultat = new Color32[pixels.Length];

            for (int y = 0; y < hauteur; y++)
            {
                for (int x = 0; x < largeur; x++)
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int n = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = Mathf.Clamp(x + dx, 0, largeur - 1);
                            int yy = Mathf.Clamp(y + dy, 0, hauteur - 1);

                            Color32 c = pixels[yy * largeur + xx];

                            r += c.r;
                            g += c.g;
                            b += c.b;
                            n++;
                        }
                    }

                    resultat[y * largeur + x] = new Color32(
                        (byte)(r / n),
                        (byte)(g / n),
                        (byte)(b / n),
                        255
                    );
                }
            }

            pixels = resultat;
        }

        petiteImage.SetPixels32(pixels);
        petiteImage.filterMode = FilterMode.Bilinear;
        petiteImage.Apply();

        fondFlou = petiteImage;
    }

    void OnGUI()
    {
        if (lectureActive)
            DessinerLecture();
        else if (joueurRegarde && !ouvertureEnCours)
            DessinerInteraction();
    }

    void DessinerInteraction()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = tailleTexteInteraction;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        if (policePixel != null)
            style.font = policePixel;

        GUI.Label(
            new Rect(
                Screen.width / 2f - 200,
                Screen.height * 0.65f,
                400,
                45
            ),
            AfficherToucheDansTexte(texteInteraction),
            style
        );
    }

    void DessinerLecture()
    {
        float largeurEcran = Screen.width;
        float hauteurEcran = Screen.height;

        // Fond flou
        if (fondFlou != null)
        {
            GUI.DrawTexture(
                new Rect(0, 0, largeurEcran, hauteurEcran),
                fondFlou,
                ScaleMode.StretchToFill
            );
        }

        // Assombrissement
        Color ancienneCouleur = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(
            new Rect(0, 0, largeurEcran, hauteurEcran),
            Texture2D.whiteTexture
        );
        GUI.color = ancienneCouleur;

        // Taille de l'image
        float largeurImage = largeurEcran * largeurImageEcran;
        float hauteurImage = hauteurEcran * hauteurImageEcran;

        Rect rectImage = new Rect(
            (largeurEcran - largeurImage) / 2f,
            (hauteurEcran - hauteurImage) / 2f,
            largeurImage,
            hauteurImage
        );

        // Affichage du parchemin PNG avec transparence
        if (imageParchemin != null)
        {
            GUI.DrawTexture(
                rectImage,
                imageParchemin,
                ScaleMode.StretchToFill,
                true
            );
        }
        else
        {
            GUI.color = new Color(0.73f, 0.65f, 0.49f, 1f);
            GUI.DrawTexture(rectImage, Texture2D.whiteTexture);
            GUI.color = ancienneCouleur;
        }

        // Zone texte à l'intérieur du parchemin
        float xTexte = rectImage.x + rectImage.width * margeGauche;
        float yTexte = rectImage.y + rectImage.height * margeHaut;
        float largeurTexte = rectImage.width * (1f - margeGauche - margeDroite);
        float hauteurTexte = rectImage.height * (1f - margeHaut - margeBas);

        Rect zoneTitre = new Rect(
            xTexte,
            yTexte + decalageTitreY,
            largeurTexte,
            80f
        );

        GUIStyle styleTitre = new GUIStyle(GUI.skin.label);
        styleTitre.alignment = TextAnchor.UpperCenter;
        styleTitre.wordWrap = true;
        styleTitre.fontStyle = FontStyle.Bold;
        styleTitre.fontSize = tailleTitre;
        styleTitre.normal.textColor = couleurTexte;

        if (policePixel != null)
            styleTitre.font = policePixel;

        GUI.Label(zoneTitre, titreNote, styleTitre);

        float hauteurReelleTitre = styleTitre.CalcHeight(
            new GUIContent(titreNote),
            largeurTexte
        );

        Rect zoneContenuVisible = new Rect(
            xTexte,
            yTexte + hauteurReelleTitre + 20f + decalageContenuY,
            largeurTexte,
            hauteurTexte - hauteurReelleTitre - 40f
        );

        GUIStyle styleContenu = new GUIStyle(GUI.skin.label);
        styleContenu.alignment = TextAnchor.UpperLeft;
        styleContenu.wordWrap = true;
        styleContenu.fontSize = tailleContenu;
        styleContenu.normal.textColor = couleurTexte;

        if (policePixel != null)
            styleContenu.font = policePixel;

        float hauteurContenu = styleContenu.CalcHeight(
            new GUIContent(contenuNote),
            largeurTexte - 18f
        ) + 10f;

        Rect contenuTotal = new Rect(
            0f,
            0f,
            largeurTexte - 18f,
            Mathf.Max(zoneContenuVisible.height, hauteurContenu)
        );

        scrollTexte = GUI.BeginScrollView(
            zoneContenuVisible,
            scrollTexte,
            contenuTotal
        );

        GUI.Label(
            new Rect(
                0f,
                0f,
                contenuTotal.width,
                hauteurContenu
            ),
            contenuNote,
            styleContenu
        );

        GUI.EndScrollView();

        GUIStyle styleFermer = new GUIStyle(GUI.skin.label);
        styleFermer.alignment = TextAnchor.MiddleCenter;
        styleFermer.fontSize = tailleTexteInteraction;
        styleFermer.fontStyle = FontStyle.Bold;
        styleFermer.normal.textColor = Color.white;

        if (policePixel != null)
            styleFermer.font = policePixel;

        GUI.Label(
            new Rect(
                0f,
                hauteurEcran - 60f,
                largeurEcran,
                40f
            ),
            AfficherToucheDansTexte("Fermer la note"),
            styleFermer
        );
    }

    void JouerSon(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        bool anciennePause = AudioListener.pause;
        AudioListener.pause = false;

        audioSource.ignoreListenerPause = true;
        audioSource.PlayOneShot(clip, volumeSons);

        AudioListener.pause = anciennePause;
    }

    // Retrouve l'action deja utilisee par InputManager et MenuOptions.
#if ENABLE_INPUT_SYSTEM
    private void RecupererActionInteraction()
    {
        if (actionInteraction != null)
            return;

        InputAction regard = InputManager.PlayerLook;
        if (regard != null && regard.actionMap != null)
            actionInteraction = regard.actionMap.FindAction("Interact", false);
    }
#endif

    // Affiche la touche clavier effective, meme apres un remappage en jeu.
    private string NomToucheInteraction()
    {
#if ENABLE_INPUT_SYSTEM
        RecupererActionInteraction();

        if (actionInteraction == null)
            return "?";

        for (int i = 0; i < actionInteraction.bindings.Count; i++)
        {
            string chemin = actionInteraction.bindings[i].effectivePath;
            if (!string.IsNullOrEmpty(chemin) &&
                chemin.StartsWith("<Keyboard>/", System.StringComparison.OrdinalIgnoreCase))
                return actionInteraction.GetBindingDisplayString(i);
        }

        return actionInteraction.GetBindingDisplayString();
#elif ENABLE_LEGACY_INPUT_MANAGER
        return "E";
#else
        return "?";
#endif
    }

    // Conserve le libelle personnalise dans l'Inspector, sans garder [E] en dur.
    private string AfficherToucheDansTexte(string texte)
    {
        if (string.IsNullOrEmpty(texte))
            texte = "Lire la note";

        if (texte.StartsWith("["))
        {
            int finTouche = texte.IndexOf(']');
            if (finTouche >= 0)
                texte = texte.Substring(finTouche + 1).TrimStart();
        }

        return "[" + NomToucheInteraction() + "] " + texte;
    }

    private bool ToucheInteraction()
    {
#if ENABLE_INPUT_SYSTEM
        RecupererActionInteraction();
        return actionInteraction != null &&
               actionInteraction.enabled &&
               actionInteraction.WasPressedThisFrame();
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}