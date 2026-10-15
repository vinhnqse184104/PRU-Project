using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera góc nhìn thứ ba chuyên dụng cho Chapter 6 - Hang Đại Bàng:
/// - Tự động bám theo Thạch Sanh (Tag: Player hoặc tên thachsanh).
/// - Di chuyển mượt mà (SmoothDamp/Lerp), không giật hình khi chạy/nhảy.
/// - Chống xuyên tường/vách đá bằng SphereCast: tự động thu ngắn khoảng cách khi áp sát vách đá.
/// - Điều khiển xoay tự do bằng chuột theo chuẩn New Input System & Legacy Input.
/// </summary>
public class Chapter6CameraFollow : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    [Tooltip("Kéo Transform của thachsanh vào đây, hoặc để trống script sẽ tự động tìm")]
    public Transform target;
    public bool autoFindPlayer = true;

    [Header("Khoảng cách & Độ cao")]
    [Tooltip("Khoảng cách từ Camera đến nhân vật")]
    [Range(2f, 10f)]
    public float distance = 4.5f;

    [Tooltip("Độ cao điểm ngắm trên thân nhân vật")]
    [Range(0.5f, 4f)]
    public float heightOffset = 2.0f;

    [Header("Độ mượt & Xoay chuột")]
    [Tooltip("Tốc độ bám theo mượt mà (5 - 8 là tối ưu)")]
    [Range(2f, 15f)]
    public float smoothSpeed = 7.0f;

    [Tooltip("Độ nhạy xoay chuột")]
    [Range(0.5f, 5f)]
    public float rotationSpeed = 1.5f;

    [Header("Chống xuyên vách đá")]
    public bool preventWallClipping = true;
    public float minDistance = 1.2f;
    public float cameraCollisionRadius = 0.25f;
    public LayerMask obstacleLayers = ~0; // Mặc định va chạm mọi layer trừ Player

    private float currentX = 0f;
    private float currentY = 12f;
    private Vector3 currentVelocity;
    private bool isInitialized = false;

    void Awake()
    {
        // Loại bỏ layer Player khỏi mặt nạ va chạm để camera không bị nhân vật cản trở
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            obstacleLayers &= ~(1 << playerLayer);
        }
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer != -1)
        {
            obstacleLayers &= ~(1 << ignoreRaycastLayer);
        }
    }

    void Start()
    {
        // Khóa chuột vào giữa màn hình khi chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResolveTarget();

        if (target != null)
        {
            // Khởi tạo góc nhìn hướng về phía trước của nhân vật
            currentX = target.eulerAngles.y;
            SnapToTarget();
        }
    }

    public void ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;

        // 1. Tìm theo tag Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // 2. Tìm theo tên GameObject
            player = GameObject.Find("thachsanh") ?? GameObject.Find("ThachSanh");
        }

        if (player != null && player.transform != transform && !player.transform.IsChildOf(transform))
        {
            // Kiểm tra nếu có target_character bên trong
            Transform tc = player.transform.Find("target_character");
            target = tc != null ? tc : player.transform;
            Debug.Log($"[Chapter6CameraFollow] Đã tự động kết nối mục tiêu: {target.name} tại {target.position}");
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 targetFocus = target.position + Vector3.up * heightOffset;
        Vector3 desiredPosition = targetFocus - (rotation * Vector3.forward * distance);

        transform.position = desiredPosition;
        transform.rotation = rotation;
        isInitialized = true;
    }

    void LateUpdate()
    {
        // 1. Đảm bảo luôn có mục tiêu
        if (target == null && autoFindPlayer)
        {
            ResolveTarget();
        }

        if (target == null || target == transform || target.IsChildOf(transform))
        {
            return;
        }

        // 2. Nhận tín hiệu chuột xoay góc nhìn
        float mouseX = 0f;
        float mouseY = 0f;

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

        // Giới hạn góc nhìn lên / xuống: -10 độ (không nhìn xuống lòng đất) đến 55 độ (nhìn bao quát vòm đá)
        currentY = Mathf.Clamp(currentY, -10f, 55f);

        // 3. Tính toán vị trí và góc xoay
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 targetFocus = target.position + Vector3.up * heightOffset;
        Vector3 camDirection = rotation * Vector3.back;

        float targetDist = distance;

        // 4. Chống xuyên vách đá bằng SphereCast
        if (preventWallClipping)
        {
            RaycastHit hit;
            if (Physics.SphereCast(targetFocus, cameraCollisionRadius, camDirection, out hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                // Nếu chạm vách đá, tự động thu ngắn khoảng cách để camera không bị chìm vào trong đá
                targetDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, distance);
            }
        }

        Vector3 desiredPosition = targetFocus + camDirection * targetDist;

        // 5. Áp dụng chuyển động mượt mà
        if (!isInitialized)
        {
            transform.position = desiredPosition;
            transform.rotation = rotation;
            isInitialized = true;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = rotation;
        }
    }
}
