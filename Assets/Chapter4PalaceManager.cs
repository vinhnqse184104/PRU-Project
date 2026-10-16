using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý tổng thể Chapter 4 (Scene: Chapter4_Lythongcuopcong):
/// 1. Tự động kiểm tra và thêm Collider cho toàn bộ sàn và công trình cung điện để nhân vật không rơi xuyên nền.
/// 2. Khởi tạo / kích hoạt nhân vật Lý Thông làm nhân vật người chơi (điều khiển WASD).
/// 3. Loại bỏ NavMeshAgent khỏi nhân vật Lý Thông để khắc phục triệt để lỗi "Failed to create agent because there is no valid NavMesh".
/// 4. Đảm bảo Camera bám theo Lý Thông (không bám Thạch Sanh).
/// 5. Đặt SpawnPoint và định vị Lý Thông đứng vững chắc trên mặt sàn cung điện.
/// </summary>
public class Chapter4PalaceManager : MonoBehaviour
{
    public static Chapter4PalaceManager Instance { get; private set; }

    [Header("Cấu hình nhân vật & Camera")]
    public GameObject lyThongObject;
    public Transform chapter4SpawnPoint;
    public CameraFollow cameraFollow;

    private bool isInitialized = false;

    void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Chapter4_Lythongcuopcong" && !sceneName.ToLower().Contains("chapter4"))
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
    }

    void Start()
    {
        StartCoroutine(InitializeChapter4Routine());
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
            if (mf == null || mf.sharedMesh == null) continue;

            string objName = mf.gameObject.name.ToLower();
            // Nếu là sàn, đất, đường đi, đá, hoặc các bộ phận kiến trúc cung điện
            if (objName.Contains("floor") || objName.Contains("ground") || objName.Contains("stone") ||
                objName.Contains("stairs") || objName.Contains("bridge") || objName.Contains("ssg"))
            {
                Collider col = mf.GetComponent<Collider>();
                if (col == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                }
            }
        }

        // Tạo lưới bảo vệ sàn phụ (Safety Net) ngay dưới mặt sàn cung điện đề phòng người chơi rơi ra ngoài
        GameObject safetyFloor = GameObject.Find("PalaceSafetyFloorCollider");
        if (safetyFloor == null)
        {
            safetyFloor = new GameObject("PalaceSafetyFloorCollider");
            BoxCollider box = safetyFloor.AddComponent<BoxCollider>();
            box.size = new Vector3(400f, 2f, 400f);
            safetyFloor.transform.position = new Vector3(-64f, -11.5f, 0f);
        }
    }

    private IEnumerator InitializeChapter4Routine()
    {
        yield return null; // Chờ 1 frame để các đối tượng trong Scene sẵn sàng

        // 2. TÌM HOẶC TẠO SPAWNPOINT CỦA LÝ THÔNG TẠI CHAPTER 4
        if (chapter4SpawnPoint == null)
        {
            GameObject spObj = GameObject.Find("Chapter4_StartPoint");
            if (spObj == null)
            {
                spObj = new GameObject("Chapter4_StartPoint");
                // Tọa độ ngoài cung điện nhìn thẳng về cổng chính (-134.5f, -10.5f, -70.0f)
                Vector3 spawnPos = new Vector3(-134.5f, -10.5f, -70.0f);
                if (Physics.Raycast(new Vector3(spawnPos.x, spawnPos.y + 10f, spawnPos.z), Vector3.down, out RaycastHit hit, 30f))
                {
                    spawnPos.y = hit.point.y + 0.05f;
                }
                spObj.transform.position = spawnPos;
                spObj.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f));
            }
            chapter4SpawnPoint = spObj.transform;
        }

        // 3. TÌM VÀ CẤU HÌNH NHÂN VẬT LÝ THÔNG
        SetupLyThongCharacter();

        // 4. CẤU HÌNH CAMERA THEO LÝ THÔNG
        SetupCameraTarget();

        isInitialized = true;
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

            // Đặt vị trí Lý Thông tại SpawnPoint an toàn trên mặt đất
            if (chapter4SpawnPoint != null)
            {
                cc.enabled = false;
                Vector3 targetPos = chapter4SpawnPoint.position;
                if (Physics.Raycast(new Vector3(targetPos.x, targetPos.y + 10f, targetPos.z), Vector3.down, out RaycastHit hit, 30f))
                {
                    targetPos.y = hit.point.y + 0.05f;
                }
                lyThongObject.transform.position = targetPos;
                lyThongObject.transform.rotation = chapter4SpawnPoint.rotation;
                Physics.SyncTransforms();
                cc.enabled = true;
            }

            Debug.Log("[Chapter4PalaceManager] Đã cấu hình thành công nhân vật Lý Thông với bộ điều khiển WASD.");
        }
    }

    private void SetupCameraTarget()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

        if (cam != null)
        {
            cameraFollow = cam.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
            }

            if (lyThongObject != null)
            {
                cameraFollow.target = lyThongObject.transform;
                cameraFollow.distance = 4.5f;
                cameraFollow.heightOffset = 1.6f;
                cameraFollow.rotationSpeed = 1.5f;
            }
        }
    }

    // OnGUI đã được loại bỏ hoàn toàn theo yêu cầu để màn hình Chapter 4 không hiển thị các dòng chữ tiêu đề và hướng dẫn
}
