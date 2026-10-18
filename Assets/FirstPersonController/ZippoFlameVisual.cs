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

    // Tuning values, read from the zippoVisual section of GameSettings
    private ZippoVisualSettings Settings => GameSettings.Instance.zippoVisual;

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
        power = Mathf.Lerp(power, targetPower, 1f - Mathf.Exp(-Settings.powerSmoothing * Time.deltaTime));

        if (flameLight != null)
        {
            flameLight.range = baseLightRange * Settings.rangeByPower.Evaluate(power);
        }

        if (flameSprite != null)
        {
            flameSprite.transform.localScale = baseFlameScale * Settings.flameScaleByPower.Evaluate(power);
        }
    }
}
