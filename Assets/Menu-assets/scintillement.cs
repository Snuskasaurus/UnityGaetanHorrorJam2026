
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LumiereFlamme : MonoBehaviour
{
    [Header("Intensité de la flamme")]
    public float intensiteMin = 0.8f;
    public float intensiteMax = 1.5f;

    [Header("Vitesse du vacillement")]
    public float vitesse = 3f;

    private Light lumiere;
    private float seed;

    void Start()
    {
        lumiere = GetComponent<Light>();
        seed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float bruit = Mathf.PerlinNoise(
            Time.time * vitesse, seed
        );

        lumiere.intensity = Mathf.Lerp(
            intensiteMin,
            intensiteMax,
            bruit
        );
    }
}
