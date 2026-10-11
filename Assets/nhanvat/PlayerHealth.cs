using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Máu của Thạch Sanh")]
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthBar;

    [Header("Giao diện Game Over")]
    public GameObject bangGameOver;

    [Header("Cài đặt Văng lùi khi bị đánh")]
    public float knockbackForce = 1.5f;
    public float knockbackDuration = 0.2f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (bangGameOver != null) bangGameOver.SetActive(false);
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (healthBar != null) healthBar.value = currentHealth;

        if (currentHealth > 0)
        {
            StartCoroutine(KnockbackRoutine(attackerPosition));
        }
        else
        {
            // Thay vì gọi hàm Die() ngay lập tức, ta gọi Coroutine đếm thời gian
            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 attackerPosition)
    {
        CharacterController controller = GetComponent<CharacterController>();
        PlayerMovement moveScript = GetComponent<PlayerMovement>();

        if (controller != null) controller.enabled = false;
        if (moveScript != null) moveScript.enabled = false;

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

        if (!isDead && controller != null) controller.enabled = true;
        if (!isDead && moveScript != null) moveScript.enabled = true;
    }

    // --- HỆ THỐNG CHẾT MỚI (RƠI XUỐNG ĐẤT, QUÁI NGỪNG ĐÁNH, CHỜ 3 GIÂY) ---
    // --- HỆ THỐNG CHẾT MỚI (RƠI XUỐNG ĐẤT, QUÁI NGỪNG ĐÁNH, CHỜ 3 GIÂY) ---
    private IEnumerator DieRoutine()
    {
        isDead = true;

        // 1. Kích hoạt hoạt ảnh ngã xuống
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        // 2. Tắt điều khiển để người chơi không bấm chạy được nữa
        PlayerMovement moveScript = GetComponent<PlayerMovement>();
        if (moveScript != null) moveScript.enabled = false;

        CharacterController controller = GetComponent<CharacterController>();

        // 3. Ép trọng lực kéo xác rớt xuống mặt đất trong 1 giây
        float fallTime = 1f;
        while (fallTime > 0)
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(new Vector3(0, -9.81f * Time.deltaTime, 0));
            }
            fallTime -= Time.deltaTime;
            yield return null;
        }

        // Tắt luôn khung va chạm để xác nằm im
        if (controller != null) controller.enabled = false;

        // =================================================================
        // 4. LÀM QUÁI VẬT NGỪNG ĐÁNH VÀ ĐỨNG IM
        EnemyAI[] tatCaQuai = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI quai in tatCaQuai)
        {
            quai.enabled = false; // Tắt não AI (không tìm người nữa)

            // Phanh gấp hệ thống di chuyển (NavMesh)
            UnityEngine.AI.NavMeshAgent agent = quai.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // Ép Animator quái chuyển về trạng thái tốc độ bằng 0 (Đứng yên)
            Animator quaiAnim = quai.GetComponent<Animator>();
            if (quaiAnim != null) quaiAnim.SetFloat("Speed", 0);
        }
        // =================================================================

        // 5. CHỜ ĐÚNG 7 GIÂY
        yield return new WaitForSeconds(7f);

        // 6. Sau 7 giây mới hiện bảng Game Over và mở khóa chuột
        if (bangGameOver != null) bangGameOver.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ChoiLaiTuDau()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}