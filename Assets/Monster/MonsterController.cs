using System;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class MonsterController : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Animator monsterAnimator;

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PUBLIC FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------


    /// ----------------------------------------------------------------------------------------------------------------
    ///  UNITY FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------
    private void Awake()
    {
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        monsterAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateAnimations();
    }

    /// ----------------------------------------------------------------------------------------------------------------
    ///  PRIVATE FUNCTIONS
    /// ----------------------------------------------------------------------------------------------------------------

    private void UpdateAnimations()
    {
        float monsterSpeed = navMeshAgent.desiredVelocity.magnitude;

        int animatorSpeedParam = Animator.StringToHash("Speed");
        monsterAnimator.SetFloat(animatorSpeedParam, monsterSpeed);
    }
}
