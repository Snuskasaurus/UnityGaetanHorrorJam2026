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

    // Detection settings
    [Header("Detection")]
    [SerializeField]
    private LayerMask wallMask = ~0;

    [SerializeField]
    [Range(0.01f, 0.3f)]
    private float probeRadius = 0.08f;

    // Retraction settings
    [Header("Retraction")]
    [SerializeField]
    [Range(0f, 1f)]
    private float maxRetract = 0.35f;

    [SerializeField]
    [Range(0f, 0.5f)]
    private float maxLower = 0.1f;

    [SerializeField]
    private Vector3 maxTuckAngles = new Vector3(15f, -10f, 0f);

    [SerializeField]
    private float retractSharpness = 20f;

    [SerializeField]
    private float extendSharpness = 8f;

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
        float sharpness = targetRetraction > currentRetraction ? retractSharpness : extendSharpness;
        currentRetraction = Mathf.Lerp(currentRetraction, targetRetraction, 1f - Mathf.Exp(-sharpness * Time.deltaTime));

        transform.localPosition = restPosition + new Vector3(0f, -maxLower, -maxRetract) * currentRetraction;
        transform.localRotation = restRotation * Quaternion.Euler(maxTuckAngles * currentRetraction);
    }

    // Returns how much the arms must be retracted, from 0 (free) to 1 (fully retracted)
    private float ComputeTargetRetraction()
    {
        if (maxRetract <= 0f)
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

        return Mathf.Clamp01((reach - hitDistance) / maxRetract);
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
        int count = Physics.SphereCastNonAlloc(origin, probeRadius, direction, castHits, distance, wallMask, QueryTriggerInteraction.Ignore);

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
