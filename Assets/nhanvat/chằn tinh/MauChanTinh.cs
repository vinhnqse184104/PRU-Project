using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class MauChanTinh : MonoBehaviour
{
    [Header("Máu của quái")]
    public int maxHealth = 100;
    public int currentHealth;

    [Tooltip("Kéo cái Slider thanh máu trên đầu Chằn Tinh thả vào đây")]
    public Slider thanhMauUI;

    [Header("Cài đặt Văng lùi khi mất máu")]
    public float knockbackForce = 1.5f; // Tôi giữ nguyên mức 1.5 cho bạn
    public float knockbackDuration = 0.2f;

    // Biến để ngăn quái chết nhiều lần
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (thanhMauUI != null)
        {
            thanhMauUI.maxValue = maxHealth;
            thanhMauUI.value = currentHealth;
        }
    }

    public void TakeDamage(int damageAmount, Vector3 attackerPosition)
    {
        // Nếu quái đã chết rồi thì chém không bị trừ máu hay văng lùi nữa
        if (isDead) return;

        currentHealth -= damageAmount;
        if (currentHealth < 0) currentHealth = 0;

        if (thanhMauUI != null) thanhMauUI.value = currentHealth;

        if (currentHealth > 0)
        {
            StartCoroutine(KnockbackRoutine(attackerPosition));
        }
        else
        {
            // GỌI HÀM CHẾT
            Die();
        }
    }

    // --- HÀM XỬ LÝ KHI CHẾT ---
    private void Die()
    {
        isDead = true; // Đánh dấu là đã chết
        Debug.Log(gameObject.name + " đã CHẾT!");

        // 1. Chạy hoạt ảnh chết trong Animator
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        // 2. Khóa chân (Tắt AI) để xác chết nằm yên, không bị trượt trên đất
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 3. Tắt Collider của con quái để Thạch Sanh có thể đi xuyên qua xác chết
        Collider coll = GetComponent<Collider>();
        if (coll != null) coll.enabled = false;

        // 4. Ẩn thanh máu trên đầu đi cho đỡ vướng mắt
        if (thanhMauUI != null) thanhMauUI.gameObject.SetActive(false);

        // 5. (Tùy chọn) Xóa hẳn xác quái vật sau 5 giây để nhẹ máy
        // Destroy(gameObject, 5f); 
    }

    private IEnumerator KnockbackRoutine(Vector3 attackerPosition)
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        Vector3 pushDirection = (transform.position - attackerPosition).normalized;
        pushDirection.y = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + pushDirection * knockbackForce;

        float elapsedTime = 0f;
        while (elapsedTime < knockbackDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / knockbackDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // CHỈ bật lại AI đi lại nếu con quái CHƯA CHẾT
        if (!isDead && agent != null) agent.enabled = true;
    }
}