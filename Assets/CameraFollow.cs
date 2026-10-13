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

    void Awake()
    {
        if (GetComponent<Camera>() == null || CompareTag("Player") || GetComponent<PlayerMovement>() != null)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (GetComponent<Camera>() == null || CompareTag("Player") || GetComponent<PlayerMovement>() != null)
        {
            enabled = false;
            return;
        }

        // Khóa chuột vào giữa màn hình và ẩn con trỏ đi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResolveTarget();
    }

    public void ResolveTarget()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Chapter4_Lythongcuopcong" || sceneName.ToLower().Contains("chapter4"))
        {
            // Trong Chapter 4, camera bám theo Lý Thông, tuyệt đối không bám Thạch Sanh
            GameObject lt = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
            if (lt == null)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                foreach (var p in players)
                {
                    if (p.name.ToLower().Contains("lythong") || p.name.ToLower().Contains("ly_thong"))
                    {
                        lt = p;
                        break;
                    }
                }
            }
            if (lt != null && lt.transform != transform && !lt.transform.IsChildOf(transform))
            {
                target = lt.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (GetComponent<Camera>() == null || CompareTag("Player") || GetComponent<PlayerMovement>() != null)
        {
            enabled = false;
            return;
        }

        // Trong Chapter 4, nếu chưa có mục tiêu thì cập nhật mục tiêu sang Lý Thông
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("chapter4"))
        {
            if (target == null || !target.name.ToLower().Contains("lythong"))
            {
                ResolveTarget();
            }
        }

        if (target == null || target == transform || target.IsChildOf(transform)) return;

        // 1. Nhận tín hiệu rê chuột (Tương thích New Input System)
        float mouseX = 0;
        float mouseY = 0;

        if (Mouse.current != null)
        {
            mouseX = Mouse.current.delta.x.ReadValue() * 0.1f * rotationSpeed;
            mouseY = Mouse.current.delta.y.ReadValue() * 0.1f * rotationSpeed;
        }
        else
        {
            try
            {
                mouseX = Input.GetAxis("Mouse X") * 2.0f * rotationSpeed;
                mouseY = Input.GetAxis("Mouse Y") * 2.0f * rotationSpeed;
            }
            catch { }
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