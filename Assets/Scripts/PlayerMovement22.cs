using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement22 : MonoBehaviour
{
    public Transform cam;              // kéo Main Camera vào
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float turnSpeed = 12f;

    CharacterController controller;
    Vector3 velocity;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Hướng di chuyển tính theo camera
        Vector3 camF = cam.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cam.right; camR.y = 0; camR.Normalize();
        Vector3 move = camF * z + camR * x;

        if (move.sqrMagnitude > 0.01f)
        {
            // Player quay mặt về hướng đang đi
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move.normalized * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}