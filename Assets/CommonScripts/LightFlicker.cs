using UnityEngine;

// Makes a light flicker like a flame with Perlin noise.
// The power (0 to 1) scales the intensity, it can be driven by a UnityEvent<float> such as ZippoController.onFlamePowerChanged.
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker")]
    [SerializeField]
    private float minIntensity = 1.2f;

    [SerializeField]
    private float maxIntensity = 1.8f;

    [SerializeField]
    private float flickerSpeed = 7f;

    [Header("Power")]
    [SerializeField]
    [Range(0f, 1f)]
    private float power = 1f;

    [SerializeField]
    private float powerSmoothing = 10f;  // how fast the intensity reaches a new power

    // Internal variables
    private Light flickeringLight;
    private float targetPower;
    private float noiseOffset;

    private void Awake()
    {
        flickeringLight = GetComponent<Light>();
        targetPower = power;
        // Several lights using this component don't flicker in sync
        noiseOffset = Random.value * 100f;
    }

    public void SetPower(float value)
    {
        targetPower = Mathf.Clamp01(value);
    }

    private void Update()
    {
        power = Mathf.Lerp(power, targetPower, 1f - Mathf.Exp(-powerSmoothing * Time.deltaTime));

        if (!flickeringLight.enabled)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(noiseOffset + Time.time * flickerSpeed, 0f);
        flickeringLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise) * power;
    }
}
