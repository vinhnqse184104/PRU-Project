using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Nhân vật cần đi theo")]
    public Transform target;

    [Header("Khoảng cách: X(trái/phải), Y(cao), Z(xa sau lưng)")]
    // Z là số âm (ví dụ: -4.5f) nghĩa là camera ở sau lưng nhân vật
    public Vector3 offset = new Vector3(0f, 2.5f, -4.5f);

    [Header("Độ mượt mà")]
    public float followSpeed = 6f;      // Tốc độ bám theo vị trí
    public float rotationSpeed = 6f;    // Tốc độ xoay theo lưng nhân vật

    [Header("Điểm nhìn trên nhân vật")]
    public float targetHeight = 1.3f;   // Nhìn vào ngang ngực/vai thay vì gan bàn chân

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Tính vị trí chính xác ở phía SAU LƯNG nhân vật dựa vào góc quay của nhân vật
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // 2. Di chuyển camera mượt mà đến vị trí sau lưng đó
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // 3. Xoay camera mượt mà để luôn hướng thẳng vào lưng/vai nhân vật
        Vector3 lookTarget = target.position + Vector3.up * targetHeight;
        Vector3 directionToTarget = lookTarget - transform.position;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }
}