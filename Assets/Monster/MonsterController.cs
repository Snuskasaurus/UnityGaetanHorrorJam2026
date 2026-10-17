using System;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(UnityEngine.AI.NavMeshAgent))]
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
        monsterAnimator = GetComponent<Animator>();
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
        float monsterSpeed = navMeshAgent.speed;
        monsterAnimator.SetFloat("Speed", monsterSpeed);
    }
}
