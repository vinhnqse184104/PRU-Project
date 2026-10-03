using UnityEngine;
using UnityEngine.UI; // Cần dòng này để dùng giao diện (Slider)

public class EnemyHealth : MonoBehaviour
{
    [Header("Cài đặt Máu")]
    public int maxHealth = 100; // Máu tối đa
    private int currentHealth;
    public Slider healthBar; // Biến chứa thanh máu giao diện

    [Header("Nhiệm vụ (Gõ: Chicken, Deer, Horse, Tiger)")]
    public string animalType; // Tên con vật để hệ thống đếm

    private bool isDead = false; // Biến kiểm tra xem đã chết chưa (để không bị đếm dư điểm)

    void Start()
    {
        currentHealth = maxHealth;

        // Cập nhật thanh máu lúc mới bắt đầu
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        // Nếu đã chết rồi thì chém thêm cũng không trừ máu hay đếm thêm điểm
        if (isDead) return;

        currentHealth -= damage; // Trừ máu

        if (healthBar != null)
        {
            healthBar.value = currentHealth; // Tụt thanh máu trên UI
        }

        Debug.Log(gameObject.name + " bị chém! Máu còn: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true; // Đánh dấu là đã chết
        Debug.Log(gameObject.name + " đã gục ngã!");

        // --- ĐOẠN CODE LIÊN KẾT NHIỆM VỤ ---
        QuestManager quest = FindObjectOfType<QuestManager>();
        if (quest != null)
        {
            quest.AddKill(animalType); // Gửi tên con vật về cho máy đếm
        }

        // Tạm thời cứ hết máu là xóa mô hình (chờ 0.2 giây trước khi xóa để game mượt hơn)
        Destroy(gameObject, 0.2f);
    }

    // Giúp thanh máu luôn hướng mặt về phía Camera để người chơi nhìn rõ
    void LateUpdate()
    {
        if (healthBar != null)
        {
            healthBar.transform.LookAt(healthBar.transform.position + Camera.main.transform.forward);
        }
    }
}