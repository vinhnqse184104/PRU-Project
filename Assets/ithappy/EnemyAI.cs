using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Cài đặt tấn công")]
    public Transform player; // Kéo Thạch Sanh vào đây
    public float attackRange = 2f; // Tầm đánh (Khoảng cách để cắn)
    public int attackDamage = 10; // Sát thương cắn
    public float timeBetweenAttacks = 2f; // Cứ 2 giây cắn 1 lần

    private float attackTimer;

    void Start()
    {
        attackTimer = timeBetweenAttacks;
    }

    void Update()
    {
        // Nếu chưa gắn Thạch Sanh thì bỏ qua
        if (player == null) return;

        // Đo khoảng cách giữa Thú và Thạch Sanh
        float distance = Vector3.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;

        // Nếu Thạch Sanh đứng đủ gần VÀ đã chờ đủ 2 giây -> Trừ máu luôn!
        if (distance <= attackRange && attackTimer <= 0f)
        {
            AttackPlayer();
            attackTimer = timeBetweenAttacks; // Đặt lại đồng hồ đếm ngược 2 giây
        }
    }

    // Hàm tự động trừ máu không cần Animation Event
    void AttackPlayer()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log("Á! Hổ cắn Thạch Sanh mất " + attackDamage + " máu!");
        }
    }
}