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
        // 1. KIỂM TRA TRẠNG THÁI TẤN CÔNG
        bool isAttacking = false;
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("chém") || stateInfo.IsName("đá"))
            {
                isAttacking = true;
            }
        }

        // 2. NHẬN NÚT DI CHUYỂN (Mở lại để có thể đuổi theo quái vật)
        float moveX = 0f;
        float moveZ = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed) moveX = 1f;
            if (Keyboard.current.sKey.isPressed) moveZ = -1f;
            if (Keyboard.current.wKey.isPressed) moveZ = 1f;
        }

        // 3. TÍNH HƯỚNG DI CHUYỂN
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

        // 4. TRỌNG LỰC VÀ NHẢY (Vẫn khóa nhảy khi đang đánh)
        if (controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;
            if (!isAttacking && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                velocity.y = jumpForce;
                if (anim != null) anim.SetTrigger("JumpTrigger");
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // 5. ĐIỀU CHỈNH TỐC ĐỘ Combat (Chìa khóa nằm ở đây)
        float currentSpeed = moveSpeed;
        if (isAttacking)
        {
            // Cho phép lướt bám theo quái với 35% tốc độ (Bạn có thể tự tăng giảm số 0.35f này)
            currentSpeed = moveSpeed * 0.35f;
        }

        // Vẫn cho phép bẻ lái (xoay mặt) để chém trúng quái vật đang chạy vòng quanh
        if (move.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 6. ÁP DỤNG DI CHUYỂN
        Vector3 finalMovement = move * currentSpeed;
        finalMovement.y = velocity.y;
        controller.Move(finalMovement * Time.deltaTime);

        // 7. CẬP NHẬT ANIMATOR CHẠY
        if (anim != null)
        {
            if (!isAttacking)
            {
                anim.SetFloat("Speed", move.magnitude); // Chạy bình thường
            }
            else
            {
                anim.SetFloat("Speed", 0f); // Mẹo: Ép Animator hiểu là đang đứng im để không bị lỗi trượt hình ảnh
            }
        }
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
            // 1. Chém trúng Thú
            if (enemy.CompareTag("Enemy"))
            {
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null)
                {
                    animalHealth.TakeDamage(attackDamage);
                }
            }
            // 2. CHÉM TRÚNG CÂY
            else if (enemy.CompareTag("Tree"))
            {
                TreeHealth tree = enemy.GetComponent<TreeHealth>();
                if (tree != null)
                {
                    tree.TakeDamage(attackDamage);
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
    public int kickDamage = 25; // Lực đá (sát thương thấp hơn chém)

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