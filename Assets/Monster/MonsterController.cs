using Assets.CommonScripts;
using System;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent), typeof(CapsuleCollider))]
public class MonsterController : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Animator monsterAnimator;

    private CapsuleCollider capsuleCollider;

    public MonsterAudio Audio { get; private set; }

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

        // Added automatically on monsters that don't have it yet
        Audio = GetComponent<MonsterAudio>();
        if (Audio == null)
        {
            Audio = gameObject.AddComponent<MonsterAudio>();
        }
    }

    private void Update()
    {
        UpdateAnimations();
        UpdateFootsteps();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !MonsterManager.IsFleeing)
        {
            Audio.PlayAttack();
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

    private void UpdateFootsteps()
    {
        // Real velocity (not desired) so there are no steps while the agent is blocked
        Audio.UpdateFootsteps(navMeshAgent.velocity.magnitude, Time.deltaTime);
    }
}
