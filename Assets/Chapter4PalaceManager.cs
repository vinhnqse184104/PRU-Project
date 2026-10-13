using UnityEngine;

/// <summary>
/// Quản lý tổng thể Chapter 4 (Scene: Chapter4_Lythongcuopcong):
/// 1. Tự động kiểm tra và thêm Collider cho toàn bộ sàn và công trình cung điện để nhân vật không rơi xuyên nền.
/// 2. Khởi tạo / kích hoạt nhân vật Lý Thông làm nhân vật người chơi (điều khiển WASD).
/// 3. Loại bỏ NavMeshAgent khỏi nhân vật Lý Thông để khắc phục triệt để lỗi "Failed to create agent because there is no valid NavMesh".
/// 4. Đảm bảo Camera bám theo Lý Thông (không bám Thạch Sanh).
/// 5. Giữ vị trí Lý Thông được đặt trong Scene; chỉ dùng SpawnPoint khi bật tùy chọn rõ ràng.
/// </summary>
public class Chapter4PalaceManager : MonoBehaviour
{
    public static Chapter4PalaceManager Instance { get; private set; }

    [Header("Cấu hình nhân vật & Camera")]
    public GameObject lyThongObject;
    public Transform chapter4SpawnPoint;
    public CameraFollow cameraFollow;
    [Tooltip("Mặc định giữ nguyên Transform trong Scene. Chỉ bật khi muốn dùng Chapter4_StartPoint.")]
    public bool useSpawnPointOnStart = false;

    void Awake()
    {
        if (!string.Equals(gameObject.scene.name, "Chapter4_LythongCuopCong", System.StringComparison.OrdinalIgnoreCase))
        {
            Destroy(gameObject);
            return;
        }
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupPalaceEnvironment();
        // Configure before the first movement frame, without a delayed teleport.
        if (chapter4SpawnPoint == null)
        {
            GameObject startPoint = GameObject.Find("Chapter4_StartPoint");
            if (startPoint != null) chapter4SpawnPoint = startPoint.transform;
        }
        SetupLyThongCharacter();
        SetupCameraTarget();
    }

    /// <summary>
    /// Thiết lập Collider cho toàn bộ sàn cung điện và loại bỏ NavMeshAgent gây xung đột
    /// </summary>
    public void SetupPalaceEnvironment()
    {
        // 1. KIỂM TRA VÀ THÊM COLLIDER CHO TOÀN BỘ SÀN CUNG ĐIỆN
        // Duyệt qua tất cả MeshFilter trong Scene để bảo đảm mọi mặt sàn, bậc thang đều có MeshCollider
        MeshFilter[] allMeshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter mf in allMeshFilters)
        {
            if (mf == null || mf.sharedMesh == null || mf.gameObject.scene != gameObject.scene) continue;

            string objName = mf.gameObject.name.ToLower();
            // Nếu là sàn, đất, đường đi, đá, hoặc các bộ phận kiến trúc cung điện
            if (objName.Contains("floor") || objName.Contains("ground") || objName.Contains("stone") ||
                objName.Contains("stairs") || objName.Contains("bridge") || objName.Contains("ssg"))
            {
                Collider col = mf.GetComponent<Collider>();
                if (col == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }
        }
    }

    private void SetupLyThongCharacter()
    {
        if (lyThongObject == null)
        {
            lyThongObject = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
        }

        if (lyThongObject == null)
        {
            // Tìm theo model hoặc tag Player
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in players)
            {
                if (p.name.ToLower().Contains("lythong") || p.name.ToLower().Contains("sage"))
                {
                    lyThongObject = p;
                    break;
                }
            }
        }

        // Nếu trong Scene chưa có GameObject Lý Thông, tự động tìm model mẫu và khởi tạo
        if (lyThongObject == null)
        {
            // Thử load từ Resources nếu có
            GameObject prefab = Resources.Load<GameObject>("LyThong_Player");
            if (prefab != null)
            {
                lyThongObject = Instantiate(prefab);
                lyThongObject.name = "LyThong";
            }
        }

        if (lyThongObject != null)
        {
            lyThongObject.tag = "Player";

            // YÊU CẦU 3: SỬA LỖI NAVMESH
            // Xóa bỏ hoàn toàn NavMeshAgent trên Lý Thông để loại bỏ lỗi "Failed to create agent because there is no valid NavMesh"
            UnityEngine.AI.NavMeshAgent agent = lyThongObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
                Destroy(agent);
            }

            // Gắn CharacterController nếu chưa có
            CharacterController cc = lyThongObject.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = lyThongObject.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.minMoveDistance = 0.001f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.3f;
            }

            // Gắn LyThongPlayerController để điều khiển WASD
            LyThongPlayerController controller = lyThongObject.GetComponent<LyThongPlayerController>();
            if (controller == null)
            {
                controller = lyThongObject.AddComponent<LyThongPlayerController>();
            }

            // Only explicit opt-in may replace the authored Transform; no ground raycast.
            if (useSpawnPointOnStart && chapter4SpawnPoint != null)
            {
                bool wasEnabled = cc.enabled;
                cc.enabled = false;
                lyThongObject.transform.position = chapter4SpawnPoint.position;
                lyThongObject.transform.rotation = chapter4SpawnPoint.rotation;
                Physics.SyncTransforms();
                cc.enabled = wasEnabled;
            }

            Debug.Log($"[Chapter4PalaceManager] Lý Thông bắt đầu tại {lyThongObject.transform.position:F3}; dùng SpawnPoint: {useSpawnPointOnStart}.");
        }
    }

    private void SetupCameraTarget()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

        if (cam != null)
        {
            cameraFollow = cam.GetComponent<CameraFollow>();
            bool addedFollow = cameraFollow == null;
            if (cameraFollow == null)
            {
                cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
            }

            if (lyThongObject != null)
            {
                cameraFollow.target = lyThongObject.transform;
                cameraFollow.enabled = true;
                if (addedFollow)
                {
                    cameraFollow.distance = 4.5f;
                    cameraFollow.heightOffset = 1.6f;
                    cameraFollow.rotationSpeed = 1.5f;
                }
            }
        }
    }

    // OnGUI đã được loại bỏ hoàn toàn theo yêu cầu để màn hình Chapter 4 không hiển thị các dòng chữ tiêu đề và hướng dẫn
}
