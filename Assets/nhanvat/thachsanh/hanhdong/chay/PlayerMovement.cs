using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f; // Trọng lực để giữ nhân vật bám sát mặt đất

    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        // Tắt Root Motion để tránh animation đẩy nhân vật bay khỏi mặt đất
        // Việc di chuyển sẽ do code (CharacterController) kiểm soát hoàn toàn
        if (anim != null)
            anim.applyRootMotion = false;
    }

    void Update()
    {
        // 1. Nhận input di chuyển
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;

        // 2. Xử lý trọng lực bám mặt đất
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Ép nhân vật bám sát mặt đất khi đang ở trên mặt đất
        }
        else
        {
            velocity.y += gravity * Time.deltaTime; // Rơi tự do nếu đang ở trên không
        }

        // 3. Tính toán hướng và di chuyển (gộp cả ngang và dọc vào 1 lần Move)
        Vector3 finalMovement = Vector3.zero;

        if (move.magnitude >= 0.1f)
        {
            // Xoay mặt nhân vật theo hướng di chuyển
            transform.forward = move;
            finalMovement = move * moveSpeed;
        }

        finalMovement.y = velocity.y;
        controller.Move(finalMovement * Time.deltaTime);

        // 4. Cập nhật Animator
        if (anim != null)
        {
            anim.SetFloat("Speed", move.magnitude);
        }
    }
}