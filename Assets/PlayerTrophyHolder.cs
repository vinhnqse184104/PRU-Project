using UnityEngine;

public class PlayerTrophyHolder : MonoBehaviour
{
    [Header("Cài đặt vật phẩm chiến lợi phẩm")]
    [Tooltip("Kéo Prefab 3D Đầu Chằn Tinh vào đây nếu có")]
    public GameObject trophyPrefab;

    [Tooltip("Vị trí xương hoặc socket để gắn vật phẩm (Để trống sẽ tự động tìm tay trái)")]
    public Transform customSocket;

    [Header("Offset tinh chỉnh vị trí khi cầm trên tay")]
    public Vector3 localPositionOffset = new Vector3(0.08f, 0.05f, 0.12f);
    public Vector3 localRotationOffset = new Vector3(15f, -90f, 0f);
    public Vector3 localScale = new Vector3(0.25f, 0.25f, 0.25f);

    private GameObject currentTrophyInstance;
    private Transform resolvedSocket;

    void Start()
    {
        CheckAndApplyTrophy();
    }

    void OnEnable()
    {
        CheckAndApplyTrophy();
    }

    public void CheckAndApplyTrophy()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Ở Chapter 1 (RungThachSanh) và Chapter 4 (Lý Thông đã cướp công lấy đầu Chằn Tinh)
        // Thạch Sanh không mang đầu Chằn Tinh
        if (currentScene == "RungThachSanh" || currentScene.ToLower().Contains("chapter4"))
        {
            RemoveTrophy();
            return;
        }

        if (StoryStateManager.HasChanTinhHead)
        {
            AttachTrophy();
        }
        else
        {
            RemoveTrophy();
        }
    }

    public void AttachTrophy()
    {
        if (currentTrophyInstance != null) return; // Tránh nhân đôi vật phẩm

        // 1. Tìm hoặc tạo Socket trên tay Thạch Sanh
        resolvedSocket = FindHandSocket();
        if (resolvedSocket == null)
        {
            resolvedSocket = transform; // Fallback vào chính player nếu không tìm thấy xương
        }

        // Kiểm tra xem socket đã có object trophy trước đó chưa
        Transform existing = resolvedSocket.Find("ChanTinhHead_Visual");
        if (existing != null)
        {
            currentTrophyInstance = existing.gameObject;
            currentTrophyInstance.SetActive(true);
            return;
        }

        // 2. Tạo đối tượng hiển thị
        if (trophyPrefab != null)
        {
            currentTrophyInstance = Instantiate(trophyPrefab, resolvedSocket);
            currentTrophyInstance.name = "ChanTinhHead_Visual";
        }
        else
        {
            // Tạo anchor đại diện nếu người dùng chưa gán Prefab 3D chuyên biệt
            currentTrophyInstance = CreateDefaultTrophyVisual(resolvedSocket);
            currentTrophyInstance.name = "ChanTinhHead_Visual";
            Debug.Log("[PlayerTrophyHolder] Đã kích hoạt vị trí mang Đầu Chằn Tinh trên tay Thạch Sanh. Bạn có thể kéo Prefab 3D vào trường Trophy Prefab trên Inspector để thay thế model.");
        }

        // 3. Thiết lập Transform trong socket
        currentTrophyInstance.transform.localPosition = localPositionOffset;
        currentTrophyInstance.transform.localRotation = Quaternion.Euler(localRotationOffset);
        currentTrophyInstance.transform.localScale = localScale;

        // 4. Đảm bảo AN TOÀN VẬT LÝ:
        // - Xóa Rigidbody để vật phẩm không rơi xuống đất
        // - Đặt Collider thành Trigger hoặc vô hiệu hóa để không đẩy Thạch Sanh rơi xuyên đất
        Rigidbody[] rbs = currentTrophyInstance.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            Destroy(rb);
        }

        Collider[] cols = currentTrophyInstance.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
        {
            col.isTrigger = true; // Không cản trở va chạm nhân vật
        }

        currentTrophyInstance.SetActive(true);
    }

    public void RemoveHead() => RemoveTrophy();

    public void RemoveTrophy()
    {
        if (currentTrophyInstance != null)
        {
            Destroy(currentTrophyInstance);
            currentTrophyInstance = null;
        }

        if (resolvedSocket != null)
        {
            Transform existing = resolvedSocket.Find("ChanTinhHead_Visual");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }
    }

    private Transform FindHandSocket()
    {
        if (customSocket != null) return customSocket;

        // Kiểm tra xem đã có socket sẵn chưa
        Transform existingSocket = transform.Find("ChanTinhHead_Socket");
        if (existingSocket != null) return existingSocket;

        // Thử lấy xương tay trái thông qua Animator Humanoid
        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (anim != null && anim.isHuman)
        {
            Transform leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                GameObject socketObj = new GameObject("ChanTinhHead_Socket");
                socketObj.transform.SetParent(leftHand, false);
                socketObj.transform.localPosition = Vector3.zero;
                socketObj.transform.localRotation = Quaternion.identity;
                return socketObj.transform;
            }
        }

        // Duyệt tìm xương qua tên nếu không lấy được qua Humanoid Bone
        Transform[] allBones = GetComponentsInChildren<Transform>();
        foreach (Transform bone in allBones)
        {
            string lowerName = bone.name.ToLower();
            if (lowerName.Contains("lefthand") || lowerName.Contains("hand_l") || lowerName.Contains("hand.l") || lowerName.Contains("wrist_l"))
            {
                GameObject socketObj = new GameObject("ChanTinhHead_Socket");
                socketObj.transform.SetParent(bone, false);
                socketObj.transform.localPosition = Vector3.zero;
                socketObj.transform.localRotation = Quaternion.identity;
                return socketObj.transform;
            }
        }

        // Fallback: Tạo socket treo bên hông trái của Thạch Sanh
        GameObject fallbackSocket = new GameObject("ChanTinhHead_Socket");
        fallbackSocket.transform.SetParent(transform, false);
        fallbackSocket.transform.localPosition = new Vector3(-0.35f, 0.85f, 0.1f);
        fallbackSocket.transform.localRotation = Quaternion.identity;
        return fallbackSocket.transform;
    }

    private GameObject CreateDefaultTrophyVisual(Transform parent)
    {
        // Tạo visual tượng trưng cho túi chiến lợi phẩm đầu Chằn Tinh
        GameObject trophyObj = new GameObject("ChanTinhHead_Placeholder");
        trophyObj.transform.SetParent(parent, false);

        GameObject headModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headModel.name = "Trophy_HeadMesh";
        headModel.transform.SetParent(trophyObj.transform, false);
        headModel.transform.localScale = new Vector3(0.35f, 0.42f, 0.35f);

        Collider sphereCol = headModel.GetComponent<Collider>();
        if (sphereCol != null) sphereCol.isTrigger = true;

        // Gán màu sẫm của Chằn Tinh cho quả cầu chiến lợi phẩm
        Renderer r = headModel.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.38f, 0.22f); // Màu xanh quái vật Chằn Tinh
            r.material = mat;
        }

        return trophyObj;
    }
}
