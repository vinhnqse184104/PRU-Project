using UnityEngine;
using UnityEngine.AI;

public class AnimalAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float normalSpeed = 2f;
    public float chaseSpeed = 4f;
    public float chaseRange = 10f;
    
    [Header("Combat Settings")]
    public float stopDistance = 1.5f; // Khoảng cách dừng lại để đánh nhau

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        timer = wanderTimer;

        agent.speed = normalSpeed;

        // Tự động ép con vật dính xuống mặt đất NavMesh ngay khi xuất hiện
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 50f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        // Tự động tìm Player theo Tag
        if (player == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        float distanceToPlayer = Mathf.Infinity;
        if (player != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, player.position);
        }

        // Nếu người chơi ở trong tầm phát hiện -> Đuổi theo
        if (distanceToPlayer <= chaseRange)
        {
            // Kiểm tra xem đã đến sát nhân vật chưa
            if (distanceToPlayer <= stopDistance)
            {
                // Dừng di chuyển để đứng đối đầu đánh nhau
                agent.isStopped = true; 

                if (animator != null)
                {
                    animator.SetBool("isChasing", false);
                    animator.SetBool("isWalking", false);
                }
            }
            else
            {
                // Vẫn đang ở xa -> Tiếp tục lao tới
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);

                if (animator != null)
                {
                    animator.SetBool("isChasing", true);
                    animator.SetBool("isWalking", false);
                }
            }
        }
        else // Ngoài tầm phát hiện -> Đi lang thang
        {
            agent.isStopped = false;

            if (animator != null)
            {
                animator.SetBool("isChasing", false);
            }

            agent.speed = normalSpeed;

            if (timer >= wanderTimer)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(newPos);
                timer = 0;
            }

            if (animator != null)
            {
                bool isWalking = agent.velocity.magnitude > 0.1f;
                animator.SetBool("isWalking", isWalking);
            }
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}