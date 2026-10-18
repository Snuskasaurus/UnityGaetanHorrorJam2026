using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Gameplay of the zippo: light attempts, flame power, light and flame visual.
// The sounds and the sparks are wired on the UnityEvents in the Inspector.
// The animations are driven by PlayerAnimationController, which listens to LightAttemptStarted
// and relays the animation events back to this component.
[RequireComponent(typeof(PlayerController))]
public class ZippoController : MonoBehaviour
{
    [Header("Light Attempt")]
    [SerializeField]
    [Range(0f, 1f)]
    private float lightAttemptSuccessChance = 0.35f;

    [Header("Flame Visual")]
    [SerializeField]
    private SpriteRenderer zippoFlame;

    // The intensity and the flicker are handled by a LightFlicker on the light, driven by onFlamePowerChanged
    [Header("Light")]
    [SerializeField]
    private Light zippoLight;

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

    // Light range and flame size by flame power (0 to 1), relative to their values in the prefab
    [SerializeField]
    private AnimationCurve rangeByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    [SerializeField]
    private AnimationCurve flameScaleByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    [SerializeField]
    private float flamePowerSmoothing = 10f;  // how fast the flame visually reaches the wanted power

    // Hooks for feedback (sounds, effects, UI) that can be wired in the Inspector
    [Header("Events")]
    [SerializeField]
    public UnityEvent OnLightAttempt;  // the thumb strikes the wheel and makes sparks, whatever the result

    [SerializeField]
    public UnityEvent OnLightAttemptStarted;

    [SerializeField]
    public UnityEvent OnLightAttemptFailed;

    [SerializeField]
    public UnityEvent OnFlameLit;

    [SerializeField]
    public UnityEvent OnFlameExtinguished;

    [SerializeField]
    public UnityEvent<float> OnFlamePowerChanged;  // receives the new flame power, from 0 to 1

    [SerializeField]
    public UnityEvent OnZippoEquipped;

    // State
    public bool IsZippoOut
    {
        get;
        private set;
    }

    public bool IsZippoLit
    {
        get;
        private set;
    }

    // Raised when a light attempt starts, the result is applied when ResolveLightAttempt is called.
    // Code listeners use AddListener, the Inspector uses the onLightAttemptStarted field.
    public UnityEvent LightAttemptStarted => OnLightAttemptStarted;

    // Internal variables
    private bool isLightAttemptInProgress = false;
    private bool isLightAttemptSuccessful = false;
    private bool isFlameBurning = false;
    private float flamePower;
    private float displayedFlamePower;
    private float baseLightRange;
    private Vector3 baseFlameScale = Vector3.one;

    private void OnEnable()
    {
        InputManager.ZippoLightAttempt.performed += OnLightAttemptPerformed;
        InputManager.ZippoIntensity.performed += OnZippoIntensityPerformed;
    }

    private void OnDisable()
    {
        InputManager.ZippoLightAttempt.performed -= OnLightAttemptPerformed;
        InputManager.ZippoIntensity.performed -= OnZippoIntensityPerformed;
    }

    private void Start()
    {
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
        IsZippoOut = true;
        IsZippoLit = false;
        isLightAttemptInProgress = false;
        isLightAttemptSuccessful = false;

        ExtinguishFlame();

        // Gives the initial power to the listeners, the event only fires on changes afterwards
        OnFlamePowerChanged.Invoke(flamePower);
    }

    private void Update()
    {
        UpdateFlamePower();

        if (flamePower > fleeingPower)
        {
            MonsterManager.TryToFearMonster();
        }
    }

    // Starts a light attempt, the animation calls ResolveLightAttempt at the right moment
    private void OnLightAttemptPerformed(InputAction.CallbackContext context)
    {
        if (!IsZippoOut || IsZippoLit || isLightAttemptInProgress)
        {
            return;
        }

        isLightAttemptInProgress = true;
        isLightAttemptSuccessful = Random.value < lightAttemptSuccessChance;
        OnLightAttemptStarted.Invoke();
    }

    // A button (F) sets the power to 1, or back to its minimum if it is already high,
    // each scroll notch changes the power by one step
    private void OnZippoIntensityPerformed(InputAction.CallbackContext context)
    {
        if (context.control is ButtonControl)
        {
            SetFlamePower(flamePower >= minFlamePower + (1f - minFlamePower) / 2f ? minFlamePower : 1f);
            MonsterManager.SendSoundToMonsterHearing(transform.position, flamePower >= 1f ? 1f : 0.5f);
            return;
        }

        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        SetFlamePower(flamePower + Mathf.Sign(scroll) * scrollStep);
    }

    private void SetFlamePower(float power)
    {
        float newPower = Mathf.Clamp(power, minFlamePower, 1f);
        if (Mathf.Approximately(newPower, flamePower))
        {
            return;
        }

        flamePower = newPower;
        OnFlamePowerChanged.Invoke(flamePower);
    }

    // Applies the result of the current light attempt
    public void ResolveLightAttempt()
    {
        if (!isLightAttemptInProgress || !IsZippoOut)
        {
            return;
        }

        OnLightAttempt.Invoke();

        if (isLightAttemptSuccessful)
        {
            IsZippoLit = true;
            LightFlame();
            OnFlameLit.Invoke();
        }
        else
        {
            IsZippoLit = false;
            ExtinguishFlame();
            OnLightAttemptFailed.Invoke();
        }
    }

    public void EndLightAttempt()
    {
        isLightAttemptInProgress = false;
        isLightAttemptSuccessful = false;
    }

    public void NotifyZippoEquipped()
    {
        OnZippoEquipped.Invoke();
    }

    public void LightFlame()
    {
        isFlameBurning = true;

        if (zippoFlame != null)
        {
            zippoFlame.enabled = true;
        }

        if (zippoLight != null)
        {
            zippoLight.enabled = true;
        }
    }

    public void ExtinguishFlame()
    {
        // Only notify when a burning flame goes out, not when the flame is reset at start
        bool wasBurning = isFlameBurning;
        isFlameBurning = false;

        if (zippoFlame != null)
        {
            zippoFlame.enabled = false;
        }

        if (zippoLight != null)
        {
            zippoLight.enabled = false;
        }

        if (wasBurning)
        {
            OnFlameExtinguished.Invoke();
        }
    }

    // Smoothly moves the flame towards the wanted power and applies it to the light range and the flame size
    private void UpdateFlamePower()
    {
        float smoothing = 1f - Mathf.Exp(-flamePowerSmoothing * Time.deltaTime);
        displayedFlamePower = Mathf.Lerp(displayedFlamePower, flamePower, smoothing);

        if (zippoLight != null)
        {
            zippoLight.range = baseLightRange * rangeByPower.Evaluate(displayedFlamePower);
        }

        if (zippoFlame != null)
        {
            zippoFlame.transform.localScale = baseFlameScale * flameScaleByPower.Evaluate(displayedFlamePower);
        }
    }
}
