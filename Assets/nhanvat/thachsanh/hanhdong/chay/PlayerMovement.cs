using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // Tốc độ xoay người mượt mà
    public float gravity = -9.81f;    // Trọng lực để giữ nhân vật bám sát mặt đất

    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;
    private Camera mainCam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        mainCam = Camera.main;

        // Tắt Root Motion để tránh animation đẩy nhân vật bay khỏi mặt đất
        // Việc di chuyển sẽ do code (CharacterController) kiểm soát hoàn toàn
        if (anim != null)
            anim.applyRootMotion = false;
    }

    void Update()
    {
        // 1. Nhận input di chuyển từ bàn phím (A/D hoặc mũi tên trái/phải, W/S)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // 2. Tính hướng di chuyển theo góc nhìn của Camera
        Vector3 move;
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
            move = new Vector3(moveX, 0f, moveZ).normalized;
        }

        // 3. Xử lý trọng lực bám mặt đất
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Ép nhân vật bám sát mặt đất khi đang ở trên mặt đất
        }
        else
        {
            velocity.y += gravity * Time.deltaTime; // Rơi tự do nếu đang ở trên không
        }

        // 4. Tính toán hướng và di chuyển
        Vector3 finalMovement = Vector3.zero;

        if (move.magnitude >= 0.1f)
        {
            // Xoay mặt nhân vật mượt mà theo hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            finalMovement = move * moveSpeed;
        }

        finalMovement.y = velocity.y;
        controller.Move(finalMovement * Time.deltaTime);

        // 5. Cập nhật Animator
        if (anim != null)
        {
            anim.SetFloat("Speed", move.magnitude);
        }
    }
}