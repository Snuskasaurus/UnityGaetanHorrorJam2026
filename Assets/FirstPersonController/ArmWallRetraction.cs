using UnityEngine;

// Pulls the first person arms back towards the body when a wall is too close,
// so the arms and the zippo light never go through walls.
// Put it on the empty parent of the animated arms, it never touches the Animator.
[DefaultExecutionOrder(100)]
public class ArmWallRetraction : MonoBehaviour
{
    // References
    [SerializeField]
    private Transform playerCamera;

    [SerializeField]
    private Transform zippoTip;

    // Tuning values, read from the arm section of GameSettings
    private ArmSettings Settings => GameSettings.Instance.arm;

    // Internal variables
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float currentRetraction = 0f;
    private readonly RaycastHit[] castHits = new RaycastHit[8];

    private void Start()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;

        if (playerCamera == null || zippoTip == null)
        {
            Debug.LogWarning("ArmWallRetraction : missing reference.", this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        currentRetraction = 0f;
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
    }

    private void LateUpdate()
    {
        float targetRetraction = ComputeTargetRetraction();

        // Retract fast so the arms don't enter the wall, extend slowly so it doesn't shake along uneven walls
        float sharpness = targetRetraction > currentRetraction ? Settings.retractSharpness : Settings.extendSharpness;
        currentRetraction = Mathf.Lerp(currentRetraction, targetRetraction, 1f - Mathf.Exp(-sharpness * Time.deltaTime));

        transform.localPosition = restPosition + new Vector3(0f, -Settings.maxLower, -Settings.maxRetract) * currentRetraction;
        transform.localRotation = restRotation * Quaternion.Euler(Settings.maxTuckAngles * currentRetraction);
    }

    // Returns how much the arms must be retracted, from 0 (free) to 1 (fully retracted)
    private float ComputeTargetRetraction()
    {
        if (Settings.maxRetract <= 0f)
        {
            return 0f;
        }

        // Where the zippo would be without our offset, so the retraction doesn't feed back into the detection
        Vector3 zippoRestPosition = GetZippoPositionAtRest();
        Vector3 toZippo = zippoRestPosition - playerCamera.position;
        float reach = toZippo.magnitude;
        if (reach < 0.001f)
        {
            return 0f;
        }

        if (!SphereCast(playerCamera.position, toZippo / reach, reach, out float hitDistance))
        {
            return 0f;
        }

        return Mathf.Clamp01((reach - hitDistance) / Settings.maxRetract);
    }

    private Vector3 GetZippoPositionAtRest()
    {
        Vector3 zippoLocal = transform.InverseTransformPoint(zippoTip.position);
        Vector3 restLocal = restPosition + restRotation * Vector3.Scale(transform.localScale, zippoLocal);
        return transform.parent != null ? transform.parent.TransformPoint(restLocal) : restLocal;
    }

    // Closest hit that does not belong to the player
    private bool SphereCast(Vector3 origin, Vector3 direction, float distance, out float hitDistance)
    {
        hitDistance = distance;
        int count = Physics.SphereCastNonAlloc(origin, Settings.probeRadius, direction, castHits, distance, Settings.wallMask, QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = castHits[i];
            if (hit.collider.transform.IsChildOf(transform.root))
            {
                continue;
            }
            if (hit.distance < hitDistance || !found)
            {
                hitDistance = hit.distance;
                found = true;
            }
        }
        return found;
    }
}
