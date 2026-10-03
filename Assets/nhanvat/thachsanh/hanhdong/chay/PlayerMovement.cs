using UnityEngine;
using UnityEngine.InputSystem; // Đang dùng New Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;

    [Header("Combat Settings")]
    public Transform attackPoint; // Kéo thả AttackPoint vào đây
    public float attackRange = 1.5f; // Bán kính tầm đánh
    public int attackDamage = 35; // Sát thương mỗi nhát chém

    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;
    private Camera mainCam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        mainCam = Camera.main;

        if (anim != null)
            anim.applyRootMotion = false;

        // KHÓA VÀ ẨN CON TRỎ CHUỘT
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Nhận input di chuyển
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed) moveX = 1f;
            if (Keyboard.current.sKey.isPressed) moveZ = -1f;
            if (Keyboard.current.wKey.isPressed) moveZ = 1f;
        }

        // 2. Tính hướng di chuyển theo góc nhìn Camera
        Vector3 move = Vector3.zero;
        if (mainCam != null)
        {
            Vector3 camForward = mainCam.transform.forward;
            Vector3 camRight = mainCam.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            move = (camForward * moveZ + camRight * moveX).normalized;
        }

        // 3. Xử lý nhảy và trọng lực
        if (controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                velocity.y = jumpForce;
                if (anim != null) anim.SetTrigger("JumpTrigger");
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // 4. Xoay mặt nhân vật
        if (move.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 5. Áp dụng di chuyển
        Vector3 finalMovement = move * moveSpeed;
        finalMovement.y = velocity.y;
        controller.Move(finalMovement * Time.deltaTime);

        // 6. Cập nhật Animator tốc độ chạy
        if (anim != null)
        {
            anim.SetFloat("Speed", move.magnitude);
        }

        // (Đã xóa code nhận nút đánh ở đây để nhường lại cho file PlayerCut/PlayerKick và Animation Event)
    }

    // 7. HÀM TẤN CÔNG (Phải là public để Animation Event gọi được)
    public void Attack()
    {
        // Nếu chưa gán Attack Point thì bỏ qua để tránh lỗi
        if (attackPoint == null) return;

        // Tạo vòng tròn phát hiện va chạm quét kẻ thù
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null)
                {
                    animalHealth.TakeDamage(attackDamage);
                }
            }
        }
    }

    // (Hỗ trợ) Vẽ vòng tròn tầm đánh màu đỏ trong màn hình Scene để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    [Header("Kick Settings")]
    public int kickDamage = 25
        ; // Lực đá (sát thương thấp hơn chém)

    // HÀM TẤN CÔNG BẰNG CHÂN (Gắn vào Animation Event của hoạt ảnh Đá)
    public void KickAttack()
    {
        if (attackPoint == null) return;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null)
                {
                    animalHealth.TakeDamage(kickDamage); // Trừ máu bằng lực đá
                }
            }
        }
    }
}