using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets
{
    public class InputManagerSingleton : MonoBehaviour
    {

        public static Vector2 PlayerLook => InputSystem.actions.FindAction("Player/Look").ReadValue<Vector2>();
        public static Vector2 PlayerMove => InputSystem.actions.FindAction("Player/Move").ReadValue<Vector2>();
        public static bool PlayerJump => InputSystem.actions.FindAction("Player/Jump").ReadValue<float>() > 0.5f;
        public static bool PlayerSprint => InputSystem.actions.FindAction("Player/Sprint").ReadValue<float>() > 0.5f;
        public static bool PlayerCrouch => InputSystem.actions.FindAction("Player/Crouch").ReadValue<float>() > 0.5f;
    }
}