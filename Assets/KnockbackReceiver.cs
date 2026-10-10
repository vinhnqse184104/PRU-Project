using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KnockbackReceiver : MonoBehaviour
{
    [Header("Cài đặt văng lùi")]
    public float knockbackForce = 3.0f;    // Đã tăng lên 3 để thấy văng xa cho rõ
    public float knockbackDuration = 0.2f;

    [Header("Tag Vũ khí của kẻ địch")]
    [Tooltip("Điền Tag vũ khí của địch. Ví dụ: VuKhiThachSanh hoặc TayChanTinh")]
    public string theVuKhiKeDich = "Weapon";

    private bool isKnockedBack = false;

    public void ApplyKnockback(Vector3 attackerPosition)
    {
        if (!isKnockedBack)
        {
            StartCoroutine(KnockbackRoutine(attackerPosition));
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 attackerPosition)
    {
        isKnockedBack = true;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        MonoBehaviour playerMovement = GetComponent("PlayerMovement") as MonoBehaviour;

        // 1. SỬA LỖI NAVMESH: BẮT BUỘC TẮT HẲN (enabled = false) ĐỂ KHÔNG BỊ GHÌ CHÂN
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;

        // Tính hướng văng
        Vector3 pushDirection = (transform.position - attackerPosition).normalized;
        pushDirection.y = 0;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + pushDirection * knockbackForce;

        // Trượt lùi
        float elapsedTime = 0f;
        while (elapsedTime < knockbackDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / knockbackDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 2. BẬT LẠI DI CHUYỂN
        if (agent != null) agent.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;

        isKnockedBack = false;
    }

    // 3. SỬA LỖI LOGIC: Nạn nhân tự phát hiện vũ khí chạm vào người mình
    void OnTriggerEnter(Collider other)
    {
        // Nếu cái chạm vào người mình mang thẻ Tag đúng là vũ khí của địch
        if (other.CompareTag(theVuKhiKeDich))
        {
            // Lấy vị trí của kẻ tấn công (Chủ nhân của cái vũ khí đó)
            Vector3 attackerPos = other.transform.root.position;

            // Tự văng lùi bản thân
            ApplyKnockback(attackerPos);
        }
    }
}