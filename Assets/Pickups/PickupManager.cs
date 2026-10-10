using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera camera;

    [SerializeField] bool enableDebugs = false;

    private readonly List<PickupComponent> pickups = new List<PickupComponent>();

    public void RegisterPickupComponent(PickupComponent pickupComponent)
    {
        if (!pickups.Contains(pickupComponent))
            pickups.Add(pickupComponent);
    }

    public void UnregisterPickupComponent(PickupComponent pickupComponent)
    {
        pickups.Remove(pickupComponent);
    }

    private void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        foreach (var pickup in pickups)
        {
            bool isInside = pickup.IsInsideShape(camera, mousePosition);
            pickup.SetHovered(isInside);
        }
    }

    void OnGUI()
    {
        if (!enableDebugs)
            return;

        foreach (var pickup in pickups)
        {
            pickup.DrawRectangle(camera);
        }
    }
}