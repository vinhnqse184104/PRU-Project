#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public class SetupChapter4Lighting
{
    private const string SCENE_PATH = "Assets/Scenes/Chapter4_LythongCuopCong.unity";

    private const string LIGHTING_SESSION_KEY = "PRU_Ch4Lighting_Done_v1";

    static SetupChapter4Lighting()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(LIGHTING_SESSION_KEY, false))
            {
                SessionState.SetBool(LIGHTING_SESSION_KEY, true);
                // Only run automatically if Chapter 4 is already the active scene
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.name.Equals("Chapter4_LythongCuopCong", System.StringComparison.OrdinalIgnoreCase))
                {
                    ApplyLightingSetup(false);
                }
            }
        };
    }

    [MenuItem("Tools/PRU/Thiết lập ánh sáng cung điện Chapter 4")]
    public static void ApplyLightingSetupMenu()
    {
        ApplyLightingSetup(true);
    }

    public static void ApplyLightingSetup(bool showDialog)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Equals("Chapter4_LythongCuopCong", System.StringComparison.OrdinalIgnoreCase))
        {
            activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        Debug.Log("[SetupChapter4Lighting] Bắt đầu thiết lập ánh sáng hoàn chỉnh cho Chapter 4...");

        // 1. TÌM VÀ CẤU HÌNH DIRECTIONAL LIGHT
        GameObject dirLightObj = GameObject.Find("LightComponent0") ?? GameObject.Find("DirectionalLight1") ?? GameObject.Find("Directional Light");
        Light dirLight = null;
        if (dirLightObj != null)
        {
            dirLight = dirLightObj.GetComponent<Light>();
        }
        if (dirLight == null)
        {
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l.type == LightType.Directional) { dirLight = l; break; }
            }
        }

        if (dirLight != null)
        {
            Undo.RecordObject(dirLight, "Cấu hình Directional Light Chapter 4");
            // Giảm độ gắt của bóng đổ từ 1.0 (đen kịt) xuống 0.68 (bóng râm mềm mại tự nhiên)
            // Giúp các mái ngói tầng dưới, tường, cột và cửa lộ rõ chi tiết vân gỗ và ngói đá
            dirLight.shadowStrength = 0.68f;
            dirLight.shadowBias = 0.06f;
            dirLight.shadowNormalBias = 0.55f;
            dirLight.intensity = 2.8f;
            dirLight.color = new Color(1.0f, 0.98f, 0.94f); // Trắng ấm nhẹ tự nhiên

            // Gán Sun vào RenderSettings
            RenderSettings.sun = dirLight;
            Debug.Log($"[SetupChapter4Lighting] Đã cấu hình Directional Light: shadowStrength={dirLight.shadowStrength}, bias={dirLight.shadowBias}, normalBias={dirLight.shadowNormalBias}");
        }

        // 2. CẤU HÌNH RENDER SETTINGS (AMBIENT LIGHT)
        // Tăng Ambient Intensity từ 0.5 lên 0.85 để chiếu sáng tán xạ dịu nhẹ các mặt khuất của mái nhà
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.ambientSkyColor = new Color(0.26f, 0.28f, 0.32f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.17f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.07f);
        Debug.Log("[SetupChapter4Lighting] Đã cập nhật RenderSettings Ambient Light (intensity = 0.85).");

        // 3. TẠO / CẬP NHẬT GAMEOBJECT Chapter4_HouseLights
        GameObject lightsRoot = GameObject.Find("Chapter4_HouseLights");
        if (lightsRoot == null)
        {
            lightsRoot = new GameObject("Chapter4_HouseLights");
            Undo.RegisterCreatedObjectUndo(lightsRoot, "Tạo Chapter4_HouseLights");
        }

        // Danh sách các điểm đặt đèn vàng ấm bên trong nhà và dưới hiên cung điện
        var lightConfigs = new (string name, Vector3 pos, float range, float intensity, Color color)[]
        {
            // Sảnh chính Cung Điện (King Court - nơi Nhà vua ngự)
            ("Light_KingCourt_MainCenter", new Vector3(-134.5f, -6.2f, 4.0f), 16.0f, 2.5f, new Color(1.0f, 0.88f, 0.70f)),
            ("Light_KingCourt_LeftPillars", new Vector3(-140.0f, -6.8f, 4.0f), 12.0f, 1.8f, new Color(1.0f, 0.84f, 0.62f)),
            ("Light_KingCourt_RightPillars", new Vector3(-129.0f, -6.8f, 4.0f), 12.0f, 1.8f, new Color(1.0f, 0.84f, 0.62f)),
            ("Light_KingCourt_ThroneBack", new Vector3(-134.5f, -6.5f, 13.0f), 13.0f, 2.0f, new Color(1.0f, 0.85f, 0.60f)),
            ("Light_KingCourt_EntrancePorch", new Vector3(-134.5f, -6.8f, -7.0f), 14.0f, 2.0f, new Color(1.0f, 0.86f, 0.66f)),

            // Dãy nhà cánh Tây (Tây điện)
            ("Light_WestPalace_InnerHall", new Vector3(-155.0f, -6.8f, 0.0f), 15.0f, 1.8f, new Color(1.0f, 0.85f, 0.65f)),
            ("Light_WestPalace_Pavilion", new Vector3(-155.0f, -6.8f, 16.0f), 14.0f, 1.7f, new Color(1.0f, 0.85f, 0.65f)),

            // Dãy nhà cánh Đông (Đông điện)
            ("Light_EastPalace_InnerHall", new Vector3(-113.0f, -6.8f, 0.0f), 15.0f, 1.8f, new Color(1.0f, 0.85f, 0.65f)),
            ("Light_EastPalace_Pavilion", new Vector3(-113.0f, -6.8f, 16.0f), 14.0f, 1.7f, new Color(1.0f, 0.85f, 0.65f)),

            // Cổng chính cung điện (Main Gate)
            ("Light_MainGate_ArchInner", new Vector3(-134.5f, -6.5f, -28.0f), 13.0f, 1.8f, new Color(1.0f, 0.86f, 0.65f)),
            ("Light_MainGate_RoofTopWarmth", new Vector3(-134.5f, -2.5f, -28.0f), 11.0f, 1.5f, new Color(1.0f, 0.90f, 0.75f)),

            // Gian nhà phía Nam nhìn sang sân (gần lối vào của Lý Thông)
            ("Light_SouthCourt_HouseA", new Vector3(-76.0f, -6.8f, -32.0f), 14.0f, 1.8f, new Color(1.0f, 0.85f, 0.65f)),
            ("Light_SouthCourt_HouseB", new Vector3(-76.0f, -6.8f, -14.0f), 14.0f, 1.8f, new Color(1.0f, 0.85f, 0.65f))
        };

        foreach (var cfg in lightConfigs)
        {
            Transform existing = lightsRoot.transform.Find(cfg.name);
            GameObject lightObj;
            if (existing == null)
            {
                lightObj = new GameObject(cfg.name);
                lightObj.transform.parent = lightsRoot.transform;
            }
            else
            {
                lightObj = existing.gameObject;
            }

            lightObj.transform.position = cfg.pos;

            Light l = lightObj.GetComponent<Light>();
            if (l == null) l = lightObj.AddComponent<Light>();

            Undo.RecordObject(l, "Cấu hình đèn nhà Chapter 4");
            l.type = LightType.Point;
            l.range = cfg.range;
            l.intensity = cfg.intensity;
            l.color = cfg.color;
            l.shadows = LightShadows.None; // Tối ưu FPS tuyệt đối cho URP
            l.renderMode = LightRenderMode.Auto;

            // Đảm bảo UniversalAdditionalLightData tương thích URP
            var addData = lightObj.GetComponent<UniversalAdditionalLightData>();
            if (addData == null) addData = lightObj.AddComponent<UniversalAdditionalLightData>();
        }

        Debug.Log($"[SetupChapter4Lighting] Đã bố trí {lightConfigs.Length} đèn Point Light vàng ấm vào 'Chapter4_HouseLights'.");

        // 4. LƯU SCENE
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("[SetupChapter4Lighting] HOÀN TẤT THIẾT LẬP ÁNH SÁNG CHAPTER 4 THÀNH CÔNG!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công",
                "Đã hoàn tất cấu hình ánh sáng Chapter 4:\n" +
                "- Directional Light: Giảm bóng đổ đen kịt từ 1.0 xuống 0.68 (làm nổi rõ vân ngói, cột gỗ).\n" +
                "- Tăng Ambient Intensity lên 0.85 để chiếu sáng tán xạ tự nhiên các mặt khuất của mái nhà.\n" +
                "- Bổ sung 13 đèn Point Light vàng ấm (ấm nhẹ cổ trang) trong 'Chapter4_HouseLights'.\n" +
                "- Sảnh ngai vàng (King Court) sáng bừng ấm cúng, nhìn rõ nhà vua và Lý Thông.\n" +
                "- Không làm sáng quá mức toàn bản đồ, tối ưu FPS tối đa.", "OK");
        }
    }
}
#endif
