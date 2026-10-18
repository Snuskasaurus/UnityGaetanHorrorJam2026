using UnityEngine;

public enum InteractType
{
    OilLamp,
    WinCondition,
    Other
}

[RequireComponent(typeof(Collider))]
public class InteractComponent : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] string InteractName;

    [Header("Interact Type")]
    [SerializeField] InteractType interactType;

    InteractManager pickupManager;

    public string GetInteractName() { return InteractName; }

    public InteractType GetInteractType() { return interactType; }

    public void Interact(GameObject player)
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
        pickupManager = FindAnyObjectByType<InteractManager>();
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
