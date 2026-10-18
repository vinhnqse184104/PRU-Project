using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Bộ điều khiển dành riêng cho nhân vật Lý Thông tại Chapter 4.
/// - Nhận phím WASD / phím mũi tên để di chuyển.
/// - Xoay theo hướng di chuyển mượt mà.
/// - Đồng bộ animation đứng / chạy qua parameter 'Speed' trong LyThongAnim.controller.
/// - Loại bỏ NavMeshAgent để tránh lỗi "Failed to create agent because there is no valid NavMesh".
/// - Không tự động di chuyển nếu người chơi chưa nhập phím.
/// </summary>
public class LyThongPlayerController : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = -9.81f;
    public float jumpForce = 4.5f;

    [Header("Camera tham chiếu")]
    public Camera mainCam;

    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;

    void Awake()
    {
        // YÊU CẦU 3: SỬA LỖI NAVMESH
        // Loại bỏ hoặc vô hiệu hóa NavMeshAgent khỏi nhân vật Lý Thông do người chơi điều khiển
        // để tránh lỗi "Failed to create agent because there is no valid NavMesh"
        // và không gây xung đột giữa NavMeshAgent với Transform/CharacterController.
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            Destroy(agent);
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (mainCam == null) mainCam = Camera.main;

        if (anim != null)
        {
            anim.applyRootMotion = false;
            // Đảm bảo Lý Thông đứng dậy ngay lập tức nếu Animator có trigger StandUp
            try { anim.SetTrigger("StandUp"); } catch { }
        }

        // Khóa con trỏ chuột vào giữa màn hình cho camera góc nhìn thứ 3
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. NHẬN PHÍM DI CHUYỂN (WASD / MŨI TÊN)
        float moveX = 0f;
        float moveZ = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
        }
#endif

        if (moveX == 0f && moveZ == 0f)
        {
            try
            {
                moveX = Input.GetAxisRaw("Horizontal");
                moveZ = Input.GetAxisRaw("Vertical");
            }
            catch { }
        }

        // 2. TÍNH TOÁN HƯỚNG DI CHUYỂN THEO CAMERA
        if (mainCam == null) mainCam = Camera.main;

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
        else
        {
            Vector3 fwd = transform.forward;
            Vector3 rgt = transform.right;
            fwd.y = 0f;
            rgt.y = 0f;
            fwd.Normalize();
            rgt.Normalize();
            move = (fwd * moveZ + rgt * moveX).normalized;
        }

        // 3. TRỌNG LỰC & NHẢY
        bool spacePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
#endif
        if (!spacePressed)
        {
            try { spacePressed = Input.GetKeyDown(KeyCode.Space); } catch { }
        }

        if (controller != null && controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;
            if (spacePressed)
            {
                velocity.y = jumpForce;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // 4. XOAY NHÂN VẬT THEO HƯỚNG DI CHUYỂN
        // Chỉ xoay khi có tín hiệu điều khiển (không tự xoay khi đứng yên)
        if (move.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 5. THỰC HIỆN DI CHUYỂN (CHARACTER CONTROLLER)
        // Không tự động di chuyển nếu người chơi chưa nhập phím (move.magnitude == 0)
        if (controller != null && controller.enabled)
        {
            Vector3 finalMovement = move * moveSpeed;
            finalMovement.y = velocity.y;
            controller.Move(finalMovement * Time.deltaTime);
        }

        // 6. CẬP NHẬT ANIMATOR SPEED
        // Trong LyThongAnim.controller:
        // Speed <= 0.1 -> Trạng thái 'đứng ' (Idle)
        // Speed > 0.1 -> Trạng thái 'chạy' (Run)
        if (anim != null)
        {
            anim.SetFloat("Speed", move.magnitude);
        }
    }
}
