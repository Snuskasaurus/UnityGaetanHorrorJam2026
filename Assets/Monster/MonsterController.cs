using Assets.CommonScripts;
using System;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent), typeof(CapsuleCollider))]
public class MonsterController : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Animator monsterAnimator;

    private CapsuleCollider capsuleCollider;

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
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void Update()
    {
        UpdateAnimations();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !MonsterManager.IsFleeing)
        {
            GameManager.Loose();
        }
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
