using UnityEngine;

// Follows the position of the parent but keeps a fixed world rotation (rain, wind, weather effects)
public class KeepWorldRotation : MonoBehaviour
{
    [SerializeField]
    private Vector3 worldRotation = Vector3.zero;  // tilt it to make the rain fall with the wind

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(worldRotation);
    }
}
