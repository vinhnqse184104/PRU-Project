using UnityEngine;
using UnityEngine.UI; // BẮT BUỘC phải có dòng này để dùng thanh máu (Slider)

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthBar; // Cái hộp để bạn kéo thanh máu vào

    void Start()
    {
        currentHealth = maxHealth;

        // Chỉnh mức máu tối đa cho thanh UI lúc mới vào game
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Tụt thanh máu trên màn hình
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Thạch Sanh đã hết máu (Game Over)!");
        }
    }
}