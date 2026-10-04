using System.Collections; // Cần dòng này để dùng bộ đếm thời gian (Coroutine)
using UnityEngine;

public class TreeHealth : MonoBehaviour
{
    public int health = 100; // Máu của cây
    public QuestManager questManager;

    private bool isDead = false; // Công tắc chặn chém bồi lúc cây đang đổ

    public void TakeDamage(int damage)
    {
        // Nếu cây đã hết máu (đang đổ) thì rìu chém vào không có tác dụng nữa
        if (isDead) return;

        health -= damage;

        if (health <= 0)
        {
            isDead = true; // Bật công tắc báo hiệu cây đã chết
            ChopDownTree(); // Gọi hàm làm đổ cây
        }
    }

    void ChopDownTree()
    {
        // 1. Báo cho QuestManager cộng thêm 1 củi vào bảng nhiệm vụ
        if (questManager != null)
        {
            questManager.CollectWood();
        }

        // 2. Nhét hiệu ứng Vật Lý (Rigidbody) vào cây để nó có trọng lượng
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Chỉnh cho cây nặng 50kg để nó đổ đầm và chân thực, không bị văng linh tinh
        rb.mass = 50f;

        // 3. Tác dụng một lực đẩy nhẹ để cây ngã ra (Ngã theo hướng ngẫu nhiên)
        Vector3 pushDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(pushDirection * 150f, ForceMode.Impulse);

        // 4. Hẹn giờ 4 giây sau sẽ làm cây biến mất (Bạn có thể đổi số 4 thành số khác)
        StartCoroutine(DestroyAfterTime(4f));
    }

    // Bộ đếm thời gian
    IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time); // Đứng chờ ở đây đúng số giây quy định
        Destroy(gameObject); // Xóa cái cây khỏi game
    }
}