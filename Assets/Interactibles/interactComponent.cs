using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractComponent : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] string InteractName;

    PickupManager pickupManager;

    public string GetInteractName() { return InteractName; }

    public void Interact()
    {
        pickupManager.UnregisterPickupComponent(this);
        Destroy(gameObject);
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        pickupManager = FindAnyObjectByType<PickupManager>();
        if (pickupManager == null)
        {
            Debug.LogError("Pickup component failed to find manager", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(pickupManager.GetPlayerTag())) 
            return;

        pickupManager.RegisterPickupComponent(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(pickupManager.GetPlayerTag())) 
            return;

        pickupManager.UnregisterPickupComponent(this);
    }
}
