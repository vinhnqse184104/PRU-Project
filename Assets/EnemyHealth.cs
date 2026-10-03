using UnityEngine;
using UnityEngine.UI; // Cần dòng này để dùng giao diện (Slider)

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100; // Máu tối đa
    private int currentHealth;

    public Slider healthBar; // Biến chứa thanh máu giao diện

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
        Debug.Log(gameObject.name + " đã gục ngã!");
        // (Tương lai bạn có thể gọi animation ngã lăn ra chết ở đây)
        Destroy(gameObject); // Tạm thời cứ hết máu là xóa mô hình
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