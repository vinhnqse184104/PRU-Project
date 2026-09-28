using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f; // Tốc độ xoay mặt nhân vật mượt hơn
    public float gravity = -9.81f;
    public float jumpForce = 5f;

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

        // KHÓA VÀ ẨN CON TRỎ CHUỘT (Giống PUBG)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Nhận input di chuyển chuẩn bằng WASD (Dùng New Input System)
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
            camForward.y = 0f; // Bỏ qua trục Y để không bay lên trời
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Hướng di chuyển cuối cùng
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

        // 4. Xoay mặt nhân vật về hướng đang chạy
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

        
    }
}