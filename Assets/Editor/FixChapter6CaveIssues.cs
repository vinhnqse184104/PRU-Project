#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class FixChapter6CaveIssues
{
    private const string FIX_SESSION_KEY = "PRU_FixChapter6Cave_v1";
    private const string SCENE_PATH = "Assets/Scenes/Chapter6_HangDaiBang.unity";
    private const string CLIFF_VISUAL_MESH_PATH = "Assets/Free_Rocks/_models/cliff.fbx";

    static FixChapter6CaveIssues()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(FIX_SESSION_KEY, false))
            {
                SessionState.SetBool(FIX_SESSION_KEY, true);
                FixAllIssues();
            }
        };
    }

    [MenuItem("Tools/PRU/Fix Chapter 6 Eagle Cave Issues")]
    public static void FixAllIssues()
    {
        Debug.Log("[FixChapter6CaveIssues] Bắt đầu sửa lỗi Thạch Sanh bị đẩy lên nóc hang trong Chapter 6...");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != SCENE_PATH)
        {
            activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        Mesh cliffVisualMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CLIFF_VISUAL_MESH_PATH);
        if (cliffVisualMesh == null)
        {
            // Thử load sub-asset mesh
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CLIFF_VISUAL_MESH_PATH);
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == "cliff")
                {
                    cliffVisualMesh = m;
                    break;
                }
            }
        }

        // ==========================================
        // 1. SỬA DiemDen_Hang: GỠ BỎ BOXCOLLIDER 1000M
        // ==========================================
        GameObject diemDenHang = GameObject.Find("DiemDen_Hang");
        if (diemDenHang != null)
        {
            BoxCollider bc = diemDenHang.GetComponent<BoxCollider>();
            if (bc != null)
            {
                Undo.DestroyObjectImmediate(bc);
                Debug.Log("[FixChapter6CaveIssues] Đã gỡ bỏ BoxCollider 1000m khổng lồ trên DiemDen_Hang.");
            }
            Undo.RecordObject(diemDenHang.transform, "Reset DiemDen_Hang scale");
            diemDenHang.transform.localScale = Vector3.one;
        }

        // ==========================================
        // 2. SỬA Ground_EntranceTunnel_-14_-46 (1): CẮT BỎ ĐOẠN CHÈO QUA DỐC
        // ==========================================
        // Ban đầu: Pos=(-490.4, 14.3, -138.9), Scale=(60, 1.2, 100) -> vươn tới X = -520.4 che kín đường dốc
        // Sửa thành: Pos=(-478.5, 14.3, -138.9), Scale=(37, 1.2, 100) -> kết thúc tại X = -497 (mép dốc)
        GameObject groundEntrance = GameObject.Find("Ground_EntranceTunnel_-14_-46 (1)");
        if (groundEntrance != null)
        {
            Undo.RecordObject(groundEntrance.transform, "Adjust Ground_EntranceTunnel (1) bounds");
            groundEntrance.transform.localPosition = new Vector3(-478.5f, 14.3f, -138.9f);
            groundEntrance.transform.localScale = new Vector3(37f, 1.2f, 100f);
            Debug.Log("[FixChapter6CaveIssues] Đã điều chỉnh Ground_EntranceTunnel_-14_-46 (1) dừng tại mép dốc X=-497, giải phóng hoàn toàn khoảng không trên dốc.");
        }

        // ==========================================
        // 3. SỬA MESHCOLLIDER CỦA CÁC KHỐI ĐÁ RightCliff_Upper_2
        // ==========================================
        // Thay cliff_collider.fbx (dạng lồi đơn giản hóa gây phình chặn hầm) bằng cliff.fbx (khớp 100% hình dạng thực)
        if (cliffVisualMesh != null)
        {
            var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var go in allObjs)
            {
                if (go.name.Contains("RightCliff_Upper_2"))
                {
                    MeshCollider mc = go.GetComponent<MeshCollider>();
                    if (mc != null && mc.sharedMesh != cliffVisualMesh)
                    {
                        Undo.RecordObject(mc, "Use accurate cliff mesh for collider");
                        mc.sharedMesh = cliffVisualMesh;
                        count++;
                    }
                }
            }
            Debug.Log($"[FixChapter6CaveIssues] Đã cập nhật MeshCollider cho {count} đối tượng RightCliff_Upper_2 sang mesh cliff.fbx chính xác.");
        }

        // ==========================================
        // 4. TẠO RAMP DỐC THÔNG SUỐT Slope_Tunnel_Walkway
        // ==========================================
        // Nối từ mép trên cửa hang (X = -497, Y = 14.9) xuống sàn đáy hang (X = -518, Y = 0.5)
        GameObject walkway = GameObject.Find("Slope_Tunnel_Walkway");
        if (walkway == null)
        {
            walkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkway.name = "Slope_Tunnel_Walkway";
            Undo.RegisterCreatedObjectUndo(walkway, "Create Slope_Tunnel_Walkway");
        }

        // Vị trí tâm: X = (-497 - 518) / 2 = -507.5, Y = (14.9 + 0.5) / 2 = 7.7, Z = -139.2
        // Độ dài theo phương X-Y: sqrt(21^2 + 14.4^2) = sqrt(441 + 207.36) = sqrt(648.36) = ~25.46m
        // Góc nghiêng: atan2(14.4, 21) = 34.4 độ
        Undo.RecordObject(walkway.transform, "Configure Slope_Tunnel_Walkway");
        walkway.transform.position = new Vector3(-507.5f, 7.3f, -139.2f);
        walkway.transform.rotation = Quaternion.Euler(0f, 0f, 34.4f);
        walkway.transform.localScale = new Vector3(25.6f, 0.6f, 16f);

        // Tắt Renderer để tàng hình tự nhiên trên bề mặt đá
        Renderer rend = walkway.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        BoxCollider rampCol = walkway.GetComponent<BoxCollider>();
        if (rampCol != null)
        {
            rampCol.isTrigger = false;
        }
        Debug.Log("[FixChapter6CaveIssues] Đã tạo dốc đi bộ mượt mà Slope_Tunnel_Walkway nghiêng 34.4 độ nối liền cửa hang xuống lòng hang.");

        // ==========================================
        // 5. GỠ BỎ NavMeshAgent TRÊN THẠCH SANH NẾU CÓ
        // ==========================================
        GameObject player = GameObject.Find("thachsanh") ?? GameObject.FindWithTag("Player");
        if (player != null)
        {
            var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                Undo.DestroyObjectImmediate(agent);
                Debug.Log("[FixChapter6CaveIssues] Đã gỡ bỏ NavMeshAgent trên Thạch Sanh để tránh xung đột với CharacterController.");
            }
        }

        // ==========================================
        // 6. LƯU SCENE
        // ==========================================
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("[FixChapter6CaveIssues] HOÀN TẤT SỬA LỖI CHAPTER 6 HANG ĐẠI BÀNG!");
    }
}
#endif
