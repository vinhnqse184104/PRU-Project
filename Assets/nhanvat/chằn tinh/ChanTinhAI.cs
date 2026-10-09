using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ChanTinhAI : MonoBehaviour
{
    [Header("Mục tiêu & Tầm phát hiện")]
    public Transform player;             // Reference tới Thạch Sanh
    public float detectionRange = 7.0f;  // Khoảng cách phát hiện (mét)
    public float loseRange = 12.0f;       // Khoảng cách mất dấu khi chạy xa

    [Header("Tốc độ di chuyển")]
    public float patrolSpeed = 1.8f;     // Tốc độ đi dạo tuần tra
    public float chaseSpeed = 4.0f;      // Tốc độ chạy đuổi theo

    [Header("Cài đặt tuần tra tự do")]
    public float patrolRadius = 6.0f;    // Bán kính đi lòng vòng quanh vị trí ban đầu
    public float waitTimeAtPoint = 2.0f; // Thời gian dừng ngó nghiêng trước khi đổi điểm

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPoint;
    private bool isWaiting = false;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        // Lưu vị trí đứng ban đầu làm tâm vùng đi tuần
        spawnPoint = transform.position;

        // Tự động tìm Thạch Sanh nếu chưa kéo vào Inspector
        if (player == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj == null) pObj = GameObject.Find("thachsanh");
            if (pObj != null) player = pObj.transform;
        }

        // Bắt đầu đi tìm điểm đầu tiên
        PickNewPatrolPoint();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Gửi vận tốc thực tế vào Animator để đổi động tác Đứng -> Đi -> Chạy
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        // Tính khoảng cách tới Thạch Sanh
        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : 999f;

        // Kiểm tra điều kiện thấy người chơi
        if (distToPlayer <= detectionRange)
        {
            isChasing = true;
        }
        else if (distToPlayer > loseRange)
        {
            isChasing = false;
        }

        // Thực thi trạng thái
        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else
        {
            PatrolRoutine();
        }
    }

    // 1. TRẠNG THÁI: ĐUỔI THEO THẠCH SANH
    void ChasePlayer()
    {
        isWaiting = false;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // 2. TRẠNG THÁI: ĐI LÒNG VÒNG TRONG MIẾU
    void PatrolRoutine()
    {
        agent.speed = patrolSpeed;

        // Đến gần điểm tuần tra và chưa trong trạng thái đứng chờ
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
        agent.isStopped = false;
        isWaiting = false;
    }

    void PickNewPatrolPoint()
    {
        // Lấy ngẫu nhiên một điểm trong bán kính quanh chỗ ban đầu
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += spawnPoint;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Hiển thị vòng tròn tầm nhìn trong Scene để dễ căn chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Vòng vàng: Tầm phát hiện
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);       // Vòng đỏ: Tầm mất dấu
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, patrolRadius); // Vòng xanh: Vùng đi tuần
    }
}