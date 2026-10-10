using System;
using System.Collections.Generic;
using Assets;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickupManager : MonoBehaviour
{
    [Header("Selection")]
    [Range(0f, 1f)][SerializeField] float minLookDot = 0.8f;
    [SerializeField] string playerTag = "Player";

    [Header("References")]
    [SerializeField] Camera camera;
    [SerializeField] FirstPersonController controller;
    [SerializeField] TMP_Text interactPromptText;

    InteractComponent currentInteractComponent;
    readonly List<InteractComponent> interactComponents = new List<InteractComponent>();

    public string GetPlayerTag() { return playerTag; }

    public void RegisterPickupComponent(InteractComponent interactComponent)
    {
        if (!interactComponents.Contains(interactComponent))
            interactComponents.Add(interactComponent);
    }

    public void UnregisterPickupComponent(InteractComponent interactComponent)
    {
        interactComponents.Remove(interactComponent);
    }

    private void OnEnable()
    {
        InputManagerSingleton.PlayerInteract.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        InputManagerSingleton.PlayerInteract.performed -= OnInteractPerformed;
    }

    private void Update()
    {
        InteractComponent NewInteractComponent = FindMostLookedAt();
        if (NewInteractComponent != null)
        {
            currentInteractComponent = NewInteractComponent;

            interactPromptText.gameObject.SetActive(true);
            string promptString = new string("ramasser " + currentInteractComponent.GetInteractName());
            interactPromptText.text = promptString;
        }
        else
        {
            interactPromptText.gameObject.SetActive(false);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("pressed interact");
        
        if (currentInteractComponent == null)
            return;

        currentInteractComponent.Interact();
    }

    private InteractComponent FindMostLookedAt()
    {
        Transform cameraTransform = camera.transform;
        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
 
        InteractComponent bestInteractComponent = null;
        float bestDot = minLookDot;
 
        foreach (InteractComponent candidate in interactComponents)
        {
            Vector3 toTarget = candidate.transform.position - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.0001f) 
                continue;
 
            Vector3 direction = toTarget / distance;
            float dot = Vector3.Dot(forward, direction);
 
            if (dot <= bestDot) 
                continue;
 
            bestInteractComponent = candidate;
            bestDot = dot;
        }
 
        return bestInteractComponent;
    }
}