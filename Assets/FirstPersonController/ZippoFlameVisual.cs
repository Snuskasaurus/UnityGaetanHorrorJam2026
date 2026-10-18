using UnityEngine;

// Visual of the zippo flame: shows or hides the light and the flame sprite, and makes their size follow the flame power.
// Wired on ZippoController: OnFlameLit -> Show, OnFlameExtinguished -> Hide, OnFlamePowerChanged -> SetPower.
// The intensity and the flicker are handled separately by a LightFlicker on the light.
public class ZippoFlameVisual : MonoBehaviour
{
    // References
    [SerializeField]
    private Light flameLight;

    [SerializeField]
    private SpriteRenderer flameSprite;

    // Light range and flame size by flame power (0 to 1), relative to their values in the prefab
    [Header("Power")]
    [SerializeField]
    private AnimationCurve rangeByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    [SerializeField]
    private AnimationCurve flameScaleByPower = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    [SerializeField]
    private float powerSmoothing = 10f;  // how fast the flame visually reaches a new power

    // Internal variables
    private float power = 1f;
    private float targetPower = 1f;
    private float baseLightRange;
    private Vector3 baseFlameScale = Vector3.one;

    private void Awake()
    {
        if (flameLight != null)
        {
            baseLightRange = flameLight.range;
        }
        if (flameSprite != null)
        {
            baseFlameScale = flameSprite.transform.localScale;
        }

        // The flame starts extinguished, it is shown when the zippo is lit
        Hide();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (flameLight != null)
        {
            flameLight.enabled = isVisible;
        }
        if (flameSprite != null)
        {
            flameSprite.enabled = isVisible;
        }
    }

    public void SetPower(float value)
    {
        targetPower = Mathf.Clamp01(value);
    }

    private void Update()
    {
        power = Mathf.Lerp(power, targetPower, 1f - Mathf.Exp(-powerSmoothing * Time.deltaTime));

        if (flameLight != null)
        {
            flameLight.range = baseLightRange * rangeByPower.Evaluate(power);
        }

        if (flameSprite != null)
        {
            flameSprite.transform.localScale = baseFlameScale * flameScaleByPower.Evaluate(power);
        }
    }
}
