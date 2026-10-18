using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Mechanics of the zippo: light attempts, lit state and flame power.
// All the feedback (sounds, sparks, light, flame visual) is wired on the UnityEvents in the Inspector.
// The animations are driven by PlayerAnimationController, which listens to OnLightAttemptStarted
// and relays the animation events back to this component.
[RequireComponent(typeof(PlayerController))]
public class ZippoController : MonoBehaviour
{
    [Header("Light Attempt")]
    [SerializeField]
    [Range(0f, 1f)]
    private float lightAttemptSuccessChance = 0.35f;

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

    [Header("Ressource Management")]
    [SerializeField]
    private float consumptionPerSecond = 1f;
    [SerializeField]
    private float maxRessource = 100f;

    // Hooks for feedback (sounds, effects, UI) that can be wired in the Inspector
    [Header("Events")]
    public UnityEvent OnLightAttempt;  // the thumb strikes the wheel and makes sparks, whatever the result

    public UnityEvent OnLightAttemptStarted;

    public UnityEvent OnLightAttemptFailed;

    public UnityEvent OnFlameLit;

    public UnityEvent OnFlameExtinguished;

    public UnityEvent<float> OnFlamePowerChanged;  // receives the new flame power, from 0 to 1

    public UnityEvent OnZippoEquipped;

    public UnityEvent<float> OnRessourceChanged;  // receives the new ressource value, from 0 to maxRessource

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

    // Internal variables
    private bool isLightAttemptInProgress = false;
    private bool isLightAttemptSuccessful = false;
    private float flamePower;
    private float currentRessource;

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

        // The zippo is always out, only its flame can be lit or extinguished
        IsZippoOut = true;
        IsZippoLit = false;
        isLightAttemptInProgress = false;
        isLightAttemptSuccessful = false;
        currentRessource = maxRessource;

        // Gives the initial power to the listeners, the event only fires on changes afterwards
        OnFlamePowerChanged.Invoke(flamePower);
    }

    private void Update()
    {
        if (flamePower > fleeingPower)
        {
            MonsterManager.TryToFearMonster();
        }

        if (IsZippoLit)
        {
            float consumption = consumptionPerSecond * flamePower * Time.deltaTime;
            ConsumeRessource(consumption);
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
        isLightAttemptSuccessful = currentRessource > 0 && Random.value < lightAttemptSuccessChance;
        OnLightAttemptStarted.Invoke();
    }

    // A button (F) sets the power to 1, or back to its minimum if it is already high,
    // each scroll notch changes the power by one step
    private void OnZippoIntensityPerformed(InputAction.CallbackContext context)
    {
        if (!IsZippoOut || !IsZippoLit)
        {
            return;
        }

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
            OnFlameLit.Invoke();
        }
        else
        {
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

    // Puts the flame out, for the mechanics that extinguish the zippo (sprint, monster, fuel...)
    public void ExtinguishFlame()
    {
        if (!IsZippoLit)
        {
            return;
        }

        IsZippoLit = false;
        flamePower = 0.0f;
        OnFlameExtinguished.Invoke();
    }

    public void ConsumeRessource(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }
        currentRessource = Mathf.Max(0f, currentRessource - amount);
        OnRessourceChanged.Invoke(currentRessource);
        if (currentRessource <= 0f)
        {
            ExtinguishFlame();
        }
    }

    public void RefillRessource(float amount, bool overrideMax = false)
    {
        if (amount <= 0f)
        {
            return;
        }
        currentRessource = overrideMax ? currentRessource + amount : Mathf.Min(maxRessource, currentRessource + amount);
        OnRessourceChanged.Invoke(currentRessource);
    }
}
