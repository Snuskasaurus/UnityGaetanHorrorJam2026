using UnityEngine;
using System.Collections;


public class MenuPrincipal : MonoBehaviour
{
    [Header("Camera et destination")]
    [SerializeField] private Transform cameraMenu;
    [SerializeField] private Transform cibleJouer;
    [SerializeField] private Transform cibleQuitter;

    [Header("Reglage")]

    [SerializeField] private float dureeDeplacement = 2f; //duree de deplacement de la camera
    [SerializeField] private AnimationCurve courbeMouvement =
    AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private IEnumerator DeplacerCamera(Transform destination)
    {
        Vector3 positionDepart = cameraMenu.position;
        Quaternion rotationDepart = cameraMenu.rotation;

        float temps = 0f;

        while (temps < dureeDeplacement)
        {
            temps += Time.deltaTime;

            float progression = temps / dureeDeplacement;
            float progresLisse = courbeMouvement.Evaluate(
                Mathf.Clamp01(progression)
            );
            
            //Position
            cameraMenu.position = Vector3.Lerp(
                positionDepart,
                destination.position,
                progresLisse
            );

            //Rotation
            cameraMenu.rotation = Quaternion.Slerp(
                rotationDepart,
                destination.rotation,
                progresLisse
            );

            yield return null;

            
        }
        cameraMenu.position = destination.position;
        cameraMenu.rotation = destination.rotation;

        
    }
    public void Jouer()
    {
        StartCoroutine(DeplacerCamera(cibleJouer));
    }
}