using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour
{
    // Animator parameters
    private static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");
    private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");
    private static readonly int IsCrouchParam = Animator.StringToHash("IsCrouch");
    private static readonly int ZippoOutParam = Animator.StringToHash("ZippoOut");
    private static readonly int ZippoOnParam = Animator.StringToHash("ZippoOn");

    [Header("References")]
    [SerializeField, FormerlySerializedAs("animateur")] private Animator animator;
    [SerializeField, FormerlySerializedAs("joueur")] private PlayerController player;
    [SerializeField, FormerlySerializedAs("corpsJoueur")] private Rigidbody playerBody;
    [SerializeField, FormerlySerializedAs("modelZippo")] private GameObject zippoModel;

    [Header("Zippo")]
    [SerializeField, FormerlySerializedAs("zippoSorti")] private bool isZippoOut = false;

    [Header("Zippo Ignition")]
    [SerializeField, FormerlySerializedAs("zippoAllume")] private bool isZippoLit = false;

    [Range(0f, 1f)]
    [SerializeField, FormerlySerializedAs("chanceAllumage")] private float ignitionChance = 0.35f;

    [Header("Zippo Sounds")]
    [SerializeField, FormerlySerializedAs("audioZippo")] private AudioSource zippoAudio;
    [SerializeField, FormerlySerializedAs("sonEchec1")] private AudioClip failSound1;
    [SerializeField, FormerlySerializedAs("sonEchec2")] private AudioClip failSound2;
    [SerializeField, FormerlySerializedAs("sonReussite")] private AudioClip successSound;
    [SerializeField, FormerlySerializedAs("sonSortirZippo")] private AudioClip takeOutZippoSound;

    [Header("Zippo Flame Visual")]
    [SerializeField, FormerlySerializedAs("flammeZippo")] private SpriteRenderer zippoFlame;

    // The spark flash light is spawned by the Lights module of this particle system
    [Header("Zippo Sparks")]
    [SerializeField, FormerlySerializedAs("etincellesZippo")] private ParticleSystem zippoSparks;

    [Header("Zippo Light")]
    [SerializeField, FormerlySerializedAs("lumiereZippo")] private Light zippoLight;
    [SerializeField, FormerlySerializedAs("intensiteMin")] private float minIntensity = 1.2f;
    [SerializeField, FormerlySerializedAs("intensiteMax")] private float maxIntensity = 1.8f;
    [SerializeField, FormerlySerializedAs("vitesseScintillement")] private float flickerSpeed = 7f;

    [Header("Flame Power")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float minFlamePower = 0.2f;

    [SerializeField]
    [Range(0.05f, 1f)]
    private float startFlamePower = 0.6f;

    [SerializeField]
    [Range(0.05f, 1f)]
    private float fleeingPower = 0.8f;

    [SerializeField]
    [Range(0.01f, 0.5f)]
    private float scrollStep = 0.1f;

    [SerializeField]
    [Range(0.05f, 1f)]
    private float minRangeFactor = 0.4f;  // light range at minimum power, relative to its range in the prefab

    [SerializeField]
    [Range(0.05f, 1f)]
    private float minFlameScaleFactor = 0.4f;  // flame sprite scale at minimum power, relative to its scale in the prefab

    [SerializeField]
    private float flamePowerSmoothing = 10f;  // how fast the flame visually reaches the wanted power

    // Internal variables
    private Renderer[] zippoRenderers;
    private bool isAttemptInProgress = false;
    private bool isAttemptSuccessful = false;
    private float flamePower;
    private float displayedFlamePower;
    private float baseLightRange;
    private Vector3 baseFlameScale = Vector3.one;

    private void OnEnable()
    {
        Subscribe(InputManager.ZippoOn, OnZippoOnPerformed);
        Subscribe(InputManager.ZippoIntensity, OnZippoIntensityPerformed);
    }

    private void OnDisable()
    {
        Unsubscribe(InputManager.ZippoOn, OnZippoOnPerformed);
        Unsubscribe(InputManager.ZippoIntensity, OnZippoIntensityPerformed);
    }

    private static void Subscribe(InputAction action, System.Action<InputAction.CallbackContext> callback)
    {
        if (action != null)
        {
            action.performed += callback;
        }
    }

    private static void Unsubscribe(InputAction action, System.Action<InputAction.CallbackContext> callback)
    {
        if (action != null)
        {
            action.performed -= callback;
        }
    }

    private void Start()
    {
        // Only look for the references that were not assigned in the Inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }
        if (playerBody == null)
        {
            playerBody = GetComponentInParent<Rigidbody>();
        }

        if (animator == null || player == null || playerBody == null || zippoModel == null)
        {
            Debug.LogWarning("PlayerAnimationController : missing reference, component disabled.", this);
            enabled = false;
            return;
        }

        zippoRenderers = zippoModel.GetComponentsInChildren<Renderer>(true);
        flamePower = Mathf.Clamp(startFlamePower, minFlamePower, 1f);
        displayedFlamePower = flamePower;
        if (zippoLight != null)
        {
            baseLightRange = zippoLight.range;
        }
        if (zippoFlame != null)
        {
            baseFlameScale = zippoFlame.transform.localScale;
        }

        // The zippo is always out, only its flame can be lit or extinguished
        isZippoOut = true;
        isZippoLit = false;
        isAttemptInProgress = false;
        isAttemptSuccessful = false;

        ShowZippo();
        ExtinguishFlame();
        StopSparkEffects();

        animator.SetBool(ZippoOutParam, true);
    }

    private void Update()
    {
        Vector3 velocity = playerBody.linearVelocity;
        velocity.y = 0f;

        animator.SetFloat(WalkSpeedParam, velocity.magnitude);
        animator.SetBool(IsRunningParam, player.CurrentMovementState == MovementState.Running);
        animator.SetBool(IsCrouchParam, player.CurrentPosture == Posture.Crouching);

        UpdateFlamePower();
        UpdateFlicker();

        if (flamePower > fleeingPower)
        {
            MonsterManager.TryToFearMonster();
        }
    }

    // Starts an ignition attempt, the result is applied by the CheckIgnition animation event
    private void OnZippoOnPerformed(InputAction.CallbackContext context)
    {
        if (!isZippoOut || isZippoLit || isAttemptInProgress || animator == null)
        {
            return;
        }

        isAttemptInProgress = true;
        isAttemptSuccessful = Random.value < ignitionChance;
        animator.SetTrigger(ZippoOnParam);
    }

    // A button (F) sets the power to 1, or back to its minimum if it is already at 1,
    // each scroll notch changes the power by one step
    private void OnZippoIntensityPerformed(InputAction.CallbackContext context)
    {
        if (context.control is ButtonControl)
        {
            flamePower = flamePower >= minFlamePower + (1f - minFlamePower)/2f ? minFlamePower : 1f;
            MonsterManager.SendSoundToMonsterHearing(transform.position, flamePower >= 1f ? 1f : 0.5f);
            return;
        }

        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        flamePower = Mathf.Clamp(flamePower + Mathf.Sign(scroll) * scrollStep, minFlamePower, 1f);
    }

    // The flame sprite is excluded, it is driven by LightFlame / ExtinguishFlame
    public void ShowZippo()
    {
        if (zippoRenderers == null)
        {
            return;
        }

        foreach (Renderer zippoRenderer in zippoRenderers)
        {
            if (zippoRenderer != null && zippoRenderer != zippoFlame)
            {
                zippoRenderer.enabled = true;
            }
        }
    }

    public void LightFlame()
    {
        if (zippoFlame != null)
        {
            zippoFlame.enabled = true;
        }

        if (zippoLight != null)
        {
            zippoLight.intensity = maxIntensity * displayedFlamePower;
            zippoLight.enabled = true;
        }
    }

    public void ExtinguishFlame()
    {
        if (zippoFlame != null)
        {
            zippoFlame.enabled = false;
        }

        if (zippoLight != null)
        {
            zippoLight.enabled = false;
        }
    }

    // Animation event
    public void CheckIgnition()
    {
        if (!isAttemptInProgress || !isZippoOut)
        {
            return;
        }

        PlayZippoSparks();

        if (isAttemptSuccessful)
        {
            isZippoLit = true;
            LightFlame();
            PlaySound(successSound);
        }
        else
        {
            isZippoLit = false;
            ExtinguishFlame();
            PlaySound(Random.value < 0.5f ? failSound1 : failSound2);
        }
    }

    // Animation event
    public void EndIgnitionAttempt()
    {
        isAttemptInProgress = false;
        isAttemptSuccessful = false;
    }

    // Animation event
    public void PlayTakeOutZippoSound()
    {
        PlaySound(takeOutZippoSound);
    }

    private void UpdateFlicker()
    {
        if (!isZippoLit || zippoLight == null || !zippoLight.enabled)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        zippoLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise) * displayedFlamePower;
    }

    // Smoothly moves the flame towards the wanted power and applies it to the light range and the flame size
    private void UpdateFlamePower()
    {
        float smoothing = 1f - Mathf.Exp(-flamePowerSmoothing * Time.deltaTime);
        displayedFlamePower = Mathf.Lerp(displayedFlamePower, flamePower, smoothing);

        if (zippoLight != null)
        {
            zippoLight.range = baseLightRange * Mathf.Lerp(minRangeFactor, 1f, displayedFlamePower);
        }

        if (zippoFlame != null)
        {
            zippoFlame.transform.localScale = baseFlameScale * Mathf.Lerp(minFlameScaleFactor, 1f, displayedFlamePower);
        }
    }

    private void PlayZippoSparks()
    {
        if (zippoSparks != null)
        {
            zippoSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            zippoSparks.Play(true);
        }
    }

    private void StopSparkEffects()
    {
        if (zippoSparks != null)
        {
            zippoSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (zippoAudio != null && clip != null)
        {
            zippoAudio.PlayOneShot(clip);
        }
    }
}
