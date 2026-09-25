using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Biến này để khai báo xem Camera sẽ đi theo ai
    public Transform target;

    // Khoảng cách từ Camera đến nhân vật (x, y, z)
    // Y = 3 (Cao hơn đầu), Z = -5 (Lùi về sau lưng)
    public Vector3 offset = new Vector3(0, 3f, -5f);

    // Độ mượt mà khi Camera di chuyển
    public float smoothSpeed = 5f;

    // Dùng LateUpdate thay vì Update để Camera không bị giật lag khi đi theo
    void LateUpdate()
    {
        if (target != null)
        {
            // Tính toán vị trí Camera cần bay tới
            Vector3 desiredPosition = target.position + offset;

            // Di chuyển mượt mà từ vị trí hiện tại tới vị trí mới
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Ép Camera luôn luôn nhìn chằm chằm vào lưng/đầu nhân vật (cộng thêm 1.5 mét để không nhìn vào chân)
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
}