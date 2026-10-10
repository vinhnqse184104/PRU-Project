using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ChanTinhAI : MonoBehaviour
{
    [Header("Mục tiêu & Tầm phát hiện")]
    public Transform player;
    public float detectionRange = 7.0f;
    public float loseRange = 12.0f;

    [Header("Tốc độ di chuyển")]
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 4.0f;

    [Header("Cài đặt tuần tra tự do")]
    public float patrolRadius = 6.0f;
    public float waitTimeAtPoint = 2.0f;

    [Header("Chiến đấu")]
    public float attackCooldown = 2.0f;
    private float lastAttackTime = 0f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPoint;
    private bool isWaiting = false;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        spawnPoint = transform.position;

        if (player == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj == null) pObj = GameObject.Find("thachsanh");
            if (pObj != null) player = pObj.transform;
        }

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : 999f;

        if (distToPlayer <= detectionRange) isChasing = true;
        else if (distToPlayer > loseRange) isChasing = false;

        if (isChasing && player != null)
        {
            ChasePlayer(distToPlayer);
        }
        else
        {
            PatrolRoutine();
        }

        if (animator != null)
        {
            float currentSpeed = agent.isStopped ? 0f : agent.desiredVelocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    void ChasePlayer(float distance)
    {
        isWaiting = false;

        if (distance <= agent.stoppingDistance)
        {
            agent.isStopped = true;

            // Xoay mặt nhìn Thạch Sanh
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 6f * Time.deltaTime);
            }

            // === HỆ THỐNG TUNG CHIÊU NGẪU NHIÊN ===
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (animator != null)
                {
                    // Trả về số nguyên ngẫu nhiên: 1, 2, hoặc 3 (số 4 ở đây nghĩa là cận dưới 4)
                    int randomAttack = Random.Range(1, 4);

                    if (randomAttack == 1) animator.SetTrigger("Attack_1");
                    else if (randomAttack == 2) animator.SetTrigger("Attack_2");
                    else if (randomAttack == 3) animator.SetTrigger("Attack_3");
                }
                lastAttackTime = Time.time;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    void PatrolRoutine()
    {
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f && !isWaiting)
        {
            StartCoroutine(WaitBeforeNextPoint());
        }
    }

    IEnumerator WaitBeforeNextPoint()
    {
        isWaiting = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(waitTimeAtPoint);

        PickNewPatrolPoint();
        isWaiting = false;
    }

    void PickNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += spawnPoint;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, patrolRadius);
    }
}