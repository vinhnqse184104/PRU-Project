#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class ThachSanhBedBuilder : EditorWindow
{
    [MenuItem("Tools/Thach Sanh/Create Bed In House")]
    public static void CreateBedInHouse()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Chapter2_LyThong")
        {
            bool proceed = EditorUtility.DisplayDialog("Thông báo Scene",
                "Scene hiện tại không phải là 'Chapter2_LyThong'. Bạn có muốn tiếp tục tạo giường trong Scene hiện tại không?",
                "Tiếp tục", "Hủy");
            if (!proceed) return;
        }

        // 1. Kiểm tra tránh tạo giường trùng lặp
        GameObject oldBed = GameObject.Find("ThachSanhBed");
        if (oldBed != null)
        {
            Undo.DestroyObjectImmediate(oldBed);
        }

        // 2. Tạo Materials cần thiết trong Assets/Materials/Bed/
        string matFolderPath = "Assets/Materials/Bed";
        if (!Directory.Exists(matFolderPath))
        {
            Directory.CreateDirectory(matFolderPath);
            AssetDatabase.Refresh();
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");

        Material darkWoodMat = CreateOrLoadMaterial(matFolderPath + "/BedDarkWood.mat", litShader, new Color(0.24f, 0.15f, 0.08f));
        Material strawMat = CreateOrLoadMaterial(matFolderPath + "/BedStrawMattress.mat", litShader, new Color(0.74f, 0.62f, 0.38f));
        Material pillowMat = CreateOrLoadMaterial(matFolderPath + "/BedPillow.mat", litShader, new Color(0.82f, 0.76f, 0.65f));
        Material blanketMat = CreateOrLoadMaterial(matFolderPath + "/BedBlanket.mat", litShader, new Color(0.30f, 0.18f, 0.10f));

        // 3. Tọa độ đặt giường (Xác định nhà medieval_house_lite_v2 (1) hoặc BedPlacementPoint)
        Vector3 bedPos = new Vector3(86.37f, 0.40f, 26.68f); // Vị trí chuẩn sát tường bên phải trong nhà Lý Thông
        Quaternion bedRot = Quaternion.Euler(0, 90f, 0);

        GameObject placementPoint = GameObject.Find("BedPlacementPoint");
        if (placementPoint != null)
        {
            bedPos = placementPoint.transform.position;
            bedRot = placementPoint.transform.rotation;
        }
        else
        {
            GameObject houseObj = GameObject.Find("medieval_house_lite_v2 (1)") ?? GameObject.Find("medieval_house_lite_v2") ?? GameObject.Find("House");
            if (houseObj != null)
            {
                // Lấy vị trí sát tường bên phải căn phòng, tránh cửa và cầu thang
                bedPos = houseObj.transform.position + new Vector3(1.8f, 0.15f, 1.2f);
                bedRot = houseObj.transform.rotation * Quaternion.Euler(0, 90f, 0);
            }
        }

        // 4. Tạo Root GameObject "ThachSanhBed"
        GameObject bedRoot = new GameObject("ThachSanhBed");
        bedRoot.transform.position = bedPos;
        bedRoot.transform.rotation = bedRot;
        Undo.RegisterCreatedObjectUndo(bedRoot, "Create ThachSanhBed");

        // 5. Dựng mô hình 3D Giường Gỗ (Chuẩn Yêu cầu 1 & ảnh tham khảo)
        // A. Khung sàn giường (Bed Base Frame)
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "BedFrame";
        frame.transform.SetParent(bedRoot.transform, false);
        frame.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        frame.transform.localScale = new Vector3(1.1f, 0.22f, 2.1f);
        frame.GetComponent<Renderer>().sharedMaterial = darkWoodMat;

        // B. 4 Chân giường gỗ (Bed Legs)
        Vector3[] legPositions = new Vector3[]
        {
            new Vector3(-0.48f, 0.02f, -0.95f),
            new Vector3(0.48f, 0.02f, -0.95f),
            new Vector3(-0.48f, 0.02f, 0.95f),
            new Vector3(0.48f, 0.02f, 0.95f)
        };

        for (int i = 0; i < legPositions.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"BedLeg_{i + 1}";
            leg.transform.SetParent(bedRoot.transform, false);
            leg.transform.localPosition = legPositions[i];
            leg.transform.localScale = new Vector3(0.12f, 0.44f, 0.12f);
            leg.GetComponent<Renderer>().sharedMaterial = darkWoodMat;
        }

        // C. Ván đầu giường (Headboard)
        GameObject headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headboard.name = "Headboard";
        headboard.transform.SetParent(bedRoot.transform, false);
        headboard.transform.localPosition = new Vector3(0f, 0.55f, 1.0f);
        headboard.transform.localScale = new Vector3(1.15f, 0.82f, 0.08f);
        headboard.GetComponent<Renderer>().sharedMaterial = darkWoodMat;

        // D. Ván đuôi giường (Footboard)
        GameObject footboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        footboard.name = "Footboard";
        footboard.transform.SetParent(bedRoot.transform, false);
        footboard.transform.localPosition = new Vector3(0f, 0.38f, -1.0f);
        footboard.transform.localScale = new Vector3(1.15f, 0.48f, 0.08f);
        footboard.GetComponent<Renderer>().sharedMaterial = darkWoodMat;

        // E. Nệm rơm (Straw Mattress)
        GameObject mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mattress.name = "StrawMattress";
        mattress.transform.SetParent(bedRoot.transform, false);
        mattress.transform.localPosition = new Vector3(0f, 0.36f, -0.02f);
        mattress.transform.localScale = new Vector3(0.98f, 0.16f, 1.92f);
        mattress.GetComponent<Renderer>().sharedMaterial = strawMat;

        // F. Gối nhỏ (Pillow)
        GameObject pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillow.name = "Pillow";
        pillow.transform.SetParent(bedRoot.transform, false);
        pillow.transform.localPosition = new Vector3(0f, 0.48f, 0.72f);
        pillow.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        pillow.transform.localScale = new Vector3(0.48f, 0.12f, 0.34f);
        pillow.GetComponent<Renderer>().sharedMaterial = pillowMat;

        // G. Tấm chăn màu nâu sẫm (Dark Blanket)
        GameObject blanket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blanket.name = "DarkBlanket";
        blanket.transform.SetParent(bedRoot.transform, false);
        blanket.transform.localPosition = new Vector3(0f, 0.45f, -0.25f);
        blanket.transform.localScale = new Vector3(1.0f, 0.06f, 1.35f);
        blanket.GetComponent<Renderer>().sharedMaterial = blanketMat;

        // 6. Thêm Box Collider vật lý cho giường để Thạch Sanh không đi xuyên qua
        BoxCollider bedCollider = bedRoot.AddComponent<BoxCollider>();
        bedCollider.center = new Vector3(0f, 0.45f, 0f);
        bedCollider.size = new Vector3(1.2f, 0.9f, 2.2f);

        // 7. Tạo SleepTrigger gắn bên cạnh giường với Box Collider Is Trigger
        GameObject triggerObj = new GameObject("SleepTrigger");
        triggerObj.transform.SetParent(bedRoot.transform, false);
        triggerObj.transform.localPosition = Vector3.zero;

        BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.center = new Vector3(0f, 0.5f, 0f);
        triggerCol.size = new Vector3(2.6f, 2.2f, 3.2f);

        BedSleepTrigger sleepTrigger = triggerObj.AddComponent<BedSleepTrigger>();
        sleepTrigger.fadeDuration = 2.0f;
        sleepTrigger.nextSceneName = "Chapter3_MieuChanTinh";

        // 8. Lưu Giường thành Prefab trong Assets/Prefabs/ThachSanhBed.prefab
        string prefabFolderPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabFolderPath))
        {
            Directory.CreateDirectory(prefabFolderPath);
            AssetDatabase.Refresh();
        }

        string prefabPath = prefabFolderPath + "/ThachSanhBed.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(bedRoot, prefabPath, InteractionMode.UserAction);

        // 9. Lưu Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"[ThachSanhBedBuilder] Đã tạo thành công Giường Gỗ 3D trong nhà Lý Thông và lưu Prefab tại {prefabPath}!");
        EditorUtility.DisplayDialog("Thành công", $"Đã tạo thành công giường 3D Thạch Sanh trong nhà Lý Thông!\n\nPrefab đã lưu tại: {prefabPath}", "OK");
    }

    private static Material CreateOrLoadMaterial(string path, Shader shader, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
        }
        return mat;
    }
}
#endif
