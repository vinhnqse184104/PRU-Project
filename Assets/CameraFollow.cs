using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Kéo nhân vật thachsanh vào đây")]
    public Transform target;

    [Header("Cài đặt Camera")]
    public float distance = 4.5f; // Khoảng cách từ cam đến lưng nhân vật
    public float heightOffset = 1.5f; // Nâng cam cao lên ngang vai
    public float rotationSpeed = 1.5f; // Tốc độ xoay chuột

    private float currentX = 0f;
    private float currentY = 15f;

    void Start()
    {
        // Khóa chuột vào giữa màn hình và ẩn con trỏ đi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Nếu chưa gán mục tiêu thì không làm gì cả để tránh lỗi
        if (target == null) return;

        // 1. Nhận tín hiệu rê chuột (Tương thích New Input System)
        float mouseX = 0;
        float mouseY = 0;

        if (Mouse.current != null)
        {
            mouseX = Mouse.current.delta.x.ReadValue() * 0.1f * rotationSpeed;
            mouseY = Mouse.current.delta.y.ReadValue() * 0.1f * rotationSpeed;
        }

        currentX += mouseX;
        currentY -= mouseY;

        // 2. Khóa góc nhìn lên/xuống để Camera không bị lật ngược lộn cổ
        currentY = Mathf.Clamp(currentY, -15f, 60f);

        // 3. Tính toán vòng quỹ đạo xoay quanh nhân vật
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * distance) + (Vector3.up * heightOffset);

        // 4. Áp dụng vị trí và góc quay cho Camera
        transform.position = position;
        transform.rotation = rotation;
    }
}