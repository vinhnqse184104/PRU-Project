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
    private bool arenaKnockbackActive;
    private Vector3 arenaPushDirection;
    private float arenaPushRemaining;

    void Start()
    {
        currentHealth = maxHealth;
        if (thanhMauUI != null)
        {
            thanhMauUI.maxValue = maxHealth;
            thanhMauUI.value = currentHealth;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        Vector3 viTriNguoiDanh = transform.position;

        // Tự động tìm vị trí Thạch Sanh để Chằn Tinh biết đường văng lùi
        GameObject thachSanh = GameObject.Find("thachsanh");
        if (thachSanh != null)
        {
            viTriNguoiDanh = thachSanh.transform.position;
        }

        // Truyền đủ 2 thông tin cho hàm TakeDamage gốc ở bên dưới xử lý
        TakeDamage(damageAmount, viTriNguoiDanh);
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
            // --- THÊM 2 DÒNG NÀY ĐỂ KÍCH HOẠT HOẠT ẢNH LÙI/BỊ ĐAU ---
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("BiThuong");

            // Gọi hiệu ứng văng lùi vật lý
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

        // 5. Kích hoạt chuỗi sự kiện tiêu diệt Chằn Tinh & trở về nhà Lý Thông
        Chapter3BossBattleManager battleManager = Chapter3BossBattleManager.Instance;
        if (battleManager == null)
        {
            battleManager = Object.FindAnyObjectByType<Chapter3BossBattleManager>();
            if (battleManager == null)
            {
                GameObject managerObj = new GameObject("Chapter3BossBattleManager");
                battleManager = managerObj.AddComponent<Chapter3BossBattleManager>();
            }
        }
        battleManager.OnBossDefeated(gameObject);
    }

    private IEnumerator KnockbackRoutine(Vector3 attackerPosition)
    {
        if (gameObject.scene.name == "Chapter3_MieuChanTinh")
        {
            arenaPushDirection = Vector3.ProjectOnPlane(transform.position - attackerPosition, Vector3.up).normalized;
            arenaPushRemaining = Mathf.Max(0.01f, knockbackDuration);
            if (arenaKnockbackActive) yield break;
            arenaKnockbackActive = true;
            NavMeshAgent arenaAgent = GetComponent<NavMeshAgent>();
            ChanTinhAI ai = GetComponent<ChanTinhAI>();
            bool restoreAI = ai != null && ai.enabled;
            bool wasStopped = arenaAgent != null && arenaAgent.enabled && arenaAgent.isOnNavMesh && arenaAgent.isStopped;
            if (restoreAI) ai.enabled = false;
            try
            {
                if (arenaAgent == null || !arenaAgent.enabled || !arenaAgent.isOnNavMesh) yield break;
                arenaAgent.isStopped = true;
                arenaAgent.velocity = Vector3.zero;
                Collider body = GetComponent<Collider>();
                while (!isDead && arenaPushRemaining > 0f && arenaAgent.enabled && arenaAgent.isOnNavMesh)
                {
                    float dt = Mathf.Min(Time.deltaTime, arenaPushRemaining);
                    Vector3 motion = arenaPushDirection * Mathf.Max(0f, knockbackForce) * dt / Mathf.Max(0.01f, knockbackDuration);
                    motion = Chapter3BossBattleManager.LimitKnockback(body, motion);
                    if (arenaAgent.Raycast(arenaAgent.nextPosition + motion, out NavMeshHit edge))
                    {
                        float safeDistance = Mathf.Max(0f, Vector3.Distance(arenaAgent.nextPosition, edge.position) - 0.05f);
                        motion = Vector3.ClampMagnitude(motion, safeDistance);
                    }
                    arenaAgent.Move(motion);
                    arenaPushRemaining -= dt;
                    yield return null;
                }
            }
            finally
            {
                arenaKnockbackActive = false;
                if (!isDead)
                {
                    if (restoreAI && ai != null) ai.enabled = true;
                    if (arenaAgent != null && arenaAgent.enabled && arenaAgent.isOnNavMesh) arenaAgent.isStopped = wasStopped;
                }
            }
            yield break;
        }
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
