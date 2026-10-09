#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class Chapter2BedSetup : EditorWindow
{
    static Chapter2BedSetup()
    {
        EditorApplication.delayCall += () =>
        {
            SetupBedInCurrentScene();
        };
    }

    [MenuItem("Thach Sanh/Setup Bed in Chapter 2")]
    public static void SetupBedInCurrentScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Chapter2_LyThong")
        {
            return; // Chỉ chạy khi Scene Chapter2_LyThong đang mở
        }

        // 1. Tìm hoặc tạo Root Bed Object "ThachSanhBed"
        GameObject bedObj = GameObject.Find("ThachSanhBed");
        if (bedObj == null)
        {
            bedObj = new GameObject("ThachSanhBed");
        }

        // Tìm vị trí đặt giường góc phòng trống (bên phải cửa, không vướng cầu thang bên trái)
        Transform houseTr = GameObject.Find("House")?.transform ?? GameObject.Find("House_Lite")?.transform;
        GameObject thachSanhPoint = GameObject.Find("thachSanhInsidePoint");

        Vector3 targetBedPos;
        Quaternion targetBedRot = Quaternion.Euler(0, 90f, 0);

        if (thachSanhPoint != null)
        {
            // Đặt ở góc phòng gần thachSanhInsidePoint (lệch sang bên phải)
            targetBedPos = thachSanhPoint.transform.position + new Vector3(1.8f, 0f, 0.5f);
        }
        else if (houseTr != null)
        {
            targetBedPos = houseTr.position + new Vector3(2.5f, 0.1f, 1.2f);
        }
        else
        {
            targetBedPos = new Vector3(2.2f, 0.1f, 1.5f);
        }

        bedObj.transform.position = targetBedPos;
        bedObj.transform.rotation = targetBedRot;

        // 2. Load Materials sẵn có từ Medieval_house_lite
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval_house_lite/wood1.mat");
        if (woodMat == null)
        {
            woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval_house_lite/wood2.mat");
        }

        // Tự động tạo Straw Material cho đệm rơm nếu chưa có
        Material strawMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval_house_lite/StrawMattress.mat");
        if (strawMat == null)
        {
            strawMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            strawMat.name = "StrawMattress";
            strawMat.color = new Color(0.85f, 0.68f, 0.32f); // Màu rơm vàng nhạt
            AssetDatabase.CreateAsset(strawMat, "Assets/Medieval_house_lite/StrawMattress.mat");
        }

        // 3. Xây dựng mô hình giường gỗ 3D (Trung cổ)
        // A. Khung giường gỗ (BedFrame)
        Transform frameTr = bedObj.transform.Find("BedFrame");
        GameObject frameObj;
        if (frameTr == null)
        {
            frameObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameObj.name = "BedFrame";
            frameObj.transform.SetParent(bedObj.transform, false);
        }
        else
        {
            frameObj = frameTr.gameObject;
        }
        frameObj.transform.localPosition = new Vector3(0, 0.2f, 0);
        frameObj.transform.localScale = new Vector3(1.3f, 0.25f, 2.2f);
        if (woodMat != null) frameObj.GetComponent<Renderer>().sharedMaterial = woodMat;

        // B. Thành đầu giường (Headboard)
        Transform headboardTr = bedObj.transform.Find("Headboard");
        GameObject headboardObj;
        if (headboardTr == null)
        {
            headboardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboardObj.name = "Headboard";
            headboardObj.transform.SetParent(bedObj.transform, false);
        }
        else
        {
            headboardObj = headboardTr.gameObject;
        }
        headboardObj.transform.localPosition = new Vector3(0, 0.55f, 1.05f);
        headboardObj.transform.localScale = new Vector3(1.35f, 0.75f, 0.12f);
        if (woodMat != null) headboardObj.GetComponent<Renderer>().sharedMaterial = woodMat;

        // C. 4 Chân giường (BedLegs)
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-0.58f, 0.05f, -0.98f),
            new Vector3(0.58f, 0.05f, -0.98f),
            new Vector3(-0.58f, 0.05f, 0.98f),
            new Vector3(0.58f, 0.05f, 0.98f)
        };

        for (int i = 0; i < legOffsets.Length; i++)
        {
            Transform legTr = bedObj.transform.Find($"BedLeg_{i + 1}");
            GameObject legObj;
            if (legTr == null)
            {
                legObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                legObj.name = $"BedLeg_{i + 1}";
                legObj.transform.SetParent(bedObj.transform, false);
            }
            else
            {
                legObj = legTr.gameObject;
            }
            legObj.transform.localPosition = legOffsets[i];
            legObj.transform.localScale = new Vector3(0.14f, 0.4f, 0.14f);
            if (woodMat != null) legObj.GetComponent<Renderer>().sharedMaterial = woodMat;
        }

        // D. Đệm rơm (StrawMattress)
        Transform mattressTr = bedObj.transform.Find("StrawMattress");
        GameObject mattressObj;
        if (mattressTr == null)
        {
            mattressObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattressObj.name = "StrawMattress";
            mattressObj.transform.SetParent(bedObj.transform, false);
        }
        else
        {
            mattressObj = mattressTr.gameObject;
        }
        mattressObj.transform.localPosition = new Vector3(0, 0.38f, -0.05f);
        mattressObj.transform.localScale = new Vector3(1.22f, 0.18f, 2.05f);
        if (strawMat != null) mattressObj.GetComponent<Renderer>().sharedMaterial = strawMat;

        // 4. Tạo SleepTrigger gắn trực tiếp vào giường
        Transform triggerTr = bedObj.transform.Find("BedSleepTrigger");
        GameObject triggerObj;
        if (triggerTr == null)
        {
            triggerObj = new GameObject("BedSleepTrigger");
            triggerObj.transform.SetParent(bedObj.transform, false);
        }
        else
        {
            triggerObj = triggerTr.gameObject;
        }
        triggerObj.transform.localPosition = Vector3.zero;

        BoxCollider boxCol = triggerObj.GetComponent<BoxCollider>();
        if (boxCol == null) boxCol = triggerObj.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.size = new Vector3(2.5f, 2.0f, 3.0f);

        BedSleepTrigger sleepComponent = triggerObj.GetComponent<BedSleepTrigger>();
        if (sleepComponent == null) sleepComponent = triggerObj.AddComponent<BedSleepTrigger>();
        sleepComponent.fadeDuration = 2.0f;
        sleepComponent.nextSceneName = "Chapter3_MieuChanTinh";

        // 5. Lưu Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[Chapter2BedSetup] Giường gỗ rơm 3D và BedSleepTrigger đã được tạo & thiết lập thành công trong Chapter2_LyThong!");
    }
}
#endif
