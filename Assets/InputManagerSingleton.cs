using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets
{
    public class InputManagerSingleton : MonoBehaviour
    {
        public static InputAction PlayerLook => InputSystem.actions.FindAction("Player/Look");
        public static InputAction PlayerMove => InputSystem.actions.FindAction("Player/Move");
        public static InputAction PlayerJump => InputSystem.actions.FindAction("Player/Jump");
        public static InputAction PlayerSprint => InputSystem.actions.FindAction("Player/Sprint");
        public static InputAction PlayerCrouch => InputSystem.actions.FindAction("Player/Crouch");
        public static InputAction PlayerInteract => InputSystem.actions.FindAction("Player/Interact");
    }
}