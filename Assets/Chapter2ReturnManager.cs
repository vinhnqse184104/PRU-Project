using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;

public class Chapter2ReturnManager : MonoBehaviour
{
    public static Chapter2ReturnManager Instance { get; private set; }

    [Header("Điểm xuất hiện ngoài nhà khi quay về")]
    [Tooltip("SpawnPoint riêng cho trường hợp quay về sau khi diệt Chằn Tinh")]
    public Transform returnSpawnPoint;

    [Header("Vị trí trong nhà cho 2 mẹ con Lý Thông")]
    public Vector3 lyThongInsidePosition = new Vector3(81.2f, 1.05f, 25.0f);
    public Vector3 motherInsidePosition = new Vector3(82.5f, 1.05f, 25.0f);

    [Header("Khoảng cách tương tác hội thoại")]
    public float interactionDistance = 3.5f;

    [Header("UI Text (Tùy chọn)")]
    public TextMeshProUGUI dialogueText;

    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private CharacterController playerController;
    private Transform lyThongTransform;
    private Transform motherTransform;

    private bool isDialogueActive = false;
    private int currentDialogueStep = 0;
    private bool isInInteractionRange = false;
    private bool isTransitioningToChapter4 = false;

    // 12 câu thoại theo đúng kịch bản truyện Thạch Sanh
    private struct DialogueLine
    {
        public string speaker;
        public string content;
        public Color speakerColor;

        public DialogueLine(string speaker, string content, Color speakerColor)
        {
            this.speaker = speaker;
            this.content = content;
            this.speakerColor = speakerColor;
        }
    }


    private readonly DialogueLine[] returnDialogues = new DialogueLine[]
    {
    new DialogueLine("THẠCH SANH",
        "Anh Lý Thông ơi! Em đã trở về!",
        new Color(0.3f, 0.8f, 1.0f)),

    new DialogueLine("LÝ THÔNG",
        "Thạch Sanh! Em vẫn còn sống trở về ư?",
        new Color(1.0f, 0.85f, 0.2f)),

    new DialogueLine("THẠCH SANH",
        "Đêm qua, em đến miếu thờ thay anh. Nửa đêm, một con Chằn Tinh hiện ra định ăn thịt em.",
        new Color(0.3f, 0.8f, 1.0f)),

    new DialogueLine("THẠCH SANH",
        "Em đã giao chiến với nó và chém được đầu nó. Anh xem, đầu Chằn Tinh em mang về đây!",
        new Color(0.3f, 0.8f, 1.0f)),

    new DialogueLine("MẸ LÝ THÔNG",
        "Trời ơi! Con Chằn Tinh ấy đã bị giết thật rồi sao?",
        new Color(1.0f, 0.5f, 0.5f)),

    new DialogueLine("LÝ THÔNG",
        "Chết rồi! Em gây ra chuyện lớn rồi, Thạch Sanh ơi!",
        new Color(1.0f, 0.85f, 0.2f)),

    new DialogueLine("THẠCH SANH",
        "Thưa anh, em giết được Chằn Tinh trừ hại cho dân, sao anh lại lo lắng?",
        new Color(0.3f, 0.8f, 1.0f)),

    new DialogueLine("LÝ THÔNG",
        "Em có biết không? Con Chằn Tinh ấy là vật nuôi của nhà vua. Nay em giết nó, ắt sẽ bị tội chết!",
        new Color(1.0f, 0.85f, 0.2f)),

    new DialogueLine("THẠCH SANH",
        "Em nào có biết! Em chỉ nghĩ giết được yêu quái là cứu dân làng khỏi tai họa.",
        new Color(0.3f, 0.8f, 1.0f)),

    new DialogueLine("MẸ LÝ THÔNG",
        "Con ơi, mau trốn đi! Nếu để quan quân bắt được thì khó giữ nổi tính mạng!",
        new Color(1.0f, 0.5f, 0.5f)),

    new DialogueLine("LÝ THÔNG",
        "Em hãy mau trở về túp lều dưới gốc đa mà ẩn náu. Mọi chuyện ở đây để anh lo liệu!",
        new Color(1.0f, 0.85f, 0.2f)),

    new DialogueLine("THẠCH SANH",
        "Vâng, em xin nghe lời anh. Em về đây!",
        new Color(0.3f, 0.8f, 1.0f))
    };


    void Awake()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Chapter2_LyThong")
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Chapter2_LyThong") return;

        // Kiểm tra xem có phải đang ở trạng thái quay về từ Chapter 3 sau khi diệt Chằn Tinh không
        if (StoryStateManager.IsChanTinhDefeated && StoryStateManager.IsReturningToChapter2)
        {
            SetupReturnScene();
            // Đảm bảo chạy thêm 1 nhịp ở frame tiếp theo để chắc chắn không bị ghi đè bởi Start của các script khác
            StartCoroutine(EnsureNPCsPositionedNextFrame());
        }
    }

    private IEnumerator EnsureNPCsPositionedNextFrame()
    {
        yield return null; // Chờ 1 frame
        SetupNPCsInsideHouse();
    }

    /// <summary>
    /// Thiết lập Scene Chapter 2 khi Thạch Sanh mang đầu Chằn Tinh trở về.
    /// </summary>
    public void SetupReturnScene()
    {
        Debug.Log("[Chapter2ReturnManager] Khởi tạo trạng thái trở về nhà Lý Thông sau khi giết Chằn Tinh.");

        // 1. Vô hiệu hóa script hướng dẫn lần 1 (LyThongGuide) để không chạy cutscene kết nghĩa ngoài rừng
        LyThongGuide guide = Object.FindAnyObjectByType<LyThongGuide>(FindObjectsInactive.Include);
        if (guide != null)
        {
            guide.enabled = false;
        }

        // 2. Tìm hoặc tạo SpawnPoint riêng ngoài nhà Lý Thông
        if (returnSpawnPoint == null)
        {
            GameObject spObj = GameObject.Find("Chapter2_ReturnSpawnPoint");
            if (spObj == null)
            {
                spObj = new GameObject("Chapter2_ReturnSpawnPoint");
                // Tận dụng vị trí cửa nhà để xác định sân trước nhà
                Vector3 doorPos = new Vector3(81.98f, 0.23f, 31.29f);
                if (guide != null && guide.homePoint != null) doorPos = guide.homePoint.position;

                Vector3 outdoorPos = doorPos + new Vector3(0f, 0.1f, 4.5f); // Khoảng sân trước cửa
                // Bắn raycast tầm thấp từ trên mặt đất 2 mét (không bắn từ nóc trời để tránh mái nhà)
                if (Physics.Raycast(new Vector3(outdoorPos.x, doorPos.y + 2f, outdoorPos.z), Vector3.down, out RaycastHit hit, 5f))
                {
                    outdoorPos.y = hit.point.y + 0.05f;
                }
                spObj.transform.position = outdoorPos;
                spObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Nhìn vào hướng cửa nhà
            }
            returnSpawnPoint = spObj.transform;
        }

        // 3. Đưa Thạch Sanh ra vị trí phía ngoài nhà Lý Thông
        GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
            playerController = playerObj.GetComponent<CharacterController>();

            if (playerController != null) playerController.enabled = false;

            playerTransform.position = returnSpawnPoint.position;
            playerTransform.rotation = returnSpawnPoint.rotation;
            Physics.SyncTransforms();

            if (playerController != null) playerController.enabled = true;
            if (playerMovement != null) playerMovement.enabled = true;

            // Đảm bảo Thạch Sanh đang mang đầu Chằn Tinh trên tay
            PlayerTrophyHolder trophy = playerObj.GetComponent<PlayerTrophyHolder>();
            if (trophy == null) trophy = playerObj.AddComponent<PlayerTrophyHolder>();
            trophy.CheckAndApplyTrophy();
        }

        // 4. Bố trí Lý Thông và Mẹ Lý Thông ở trong nhà
        SetupNPCsInsideHouse();

        // 5. Mở sẵn cửa nhà để Thạch Sanh đi vào
        DoorInteraction door = Object.FindAnyObjectByType<DoorInteraction>();
        if (door != null)
        {
            door.OpenDoor();
        }

        // 6. Cập nhật mục tiêu nhiệm vụ ban đầu khi mới về
        UpdateQuestBanner("NHIỆM VỤ: Đi vào nhà gặp mẹ con Lý Thông để báo tin!");
    }

    private void SetupNPCsInsideHouse()
    {
        LyThongGuide guide = Object.FindAnyObjectByType<LyThongGuide>(FindObjectsInactive.Include);

        // A. TÌM VÀ ĐỊNH VỊ LÝ THÔNG
        GameObject ltObj = FindLyThongGameObject(guide);
        if (ltObj != null)
        {
            ltObj.SetActive(true);
            lyThongTransform = ltObj.transform;

            NavMeshAgent ltAgent = ltObj.GetComponent<NavMeshAgent>();
            if (ltAgent != null) ltAgent.enabled = false;

            // Tận dụng Transform mốc trong nhà đã có trong Scene
            Vector3 targetLtPos = GetIndoorReferencePosition(true);

            lyThongTransform.position = targetLtPos;
            lyThongTransform.rotation = Quaternion.Euler(0f, 0f, 0f); // Quay mặt ra cửa chính

            // Đảm bảo MeshRenderer/SkinnedMeshRenderer bật để nhìn thấy NPC
            Renderer[] renderers = ltObj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers) r.enabled = true;

            // Đảm bảo Collider bật để Thạch Sanh có thể tương tác
            Collider[] colliders = ltObj.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders) c.enabled = true;

            Animator anim = ltObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetFloat("Speed", 0f);
            }

            Debug.Log($"[Chapter2ReturnManager] Đã đặt Lý Thông vào trong nhà tại: {lyThongTransform.position}");
        }
        else
        {
            Debug.LogWarning("[Chapter2ReturnManager] Không tìm thấy GameObject của Lý Thông trong Scene!");
        }

        // B. TÌM VÀ ĐỊNH VỊ MẸ LÝ THÔNG
        GameObject mObj = FindMotherGameObject(guide);
        if (mObj != null)
        {
            mObj.SetActive(true);
            motherTransform = mObj.transform;

            NavMeshAgent mAgent = mObj.GetComponent<NavMeshAgent>();
            if (mAgent != null) mAgent.enabled = false;

            Vector3 targetMPos = GetIndoorReferencePosition(false);

            motherTransform.position = targetMPos;
            motherTransform.rotation = Quaternion.Euler(0f, 0f, 0f); // Quay mặt ra cửa chính

            Renderer[] renderers = mObj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers) r.enabled = true;

            Collider[] colliders = mObj.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders) c.enabled = true;

            Animator anim = mObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetFloat("Speed", 0f);
            }

            Debug.Log($"[Chapter2ReturnManager] Đã đặt Mẹ Lý Thông vào trong nhà tại: {motherTransform.position}");
        }
        else
        {
            Debug.LogWarning("[Chapter2ReturnManager] Không tìm thấy GameObject của Mẹ Lý Thông trong Scene!");
        }
    }

    private Vector3 GetIndoorReferencePosition(bool isLyThong)
    {
        // Ưu tiên 1: Lấy mốc Transform 'DiemThachSanhVaoNha' đã được đặt sẵn trong Scene
        Transform marker = GameObject.Find("DiemThachSanhVaoNha")?.transform;
        if (marker != null)
        {
            // Điểm trong nhà có tọa độ chuẩn: Y = marker.position.y (không bị mái nhà cản trở)
            Vector3 basePos = marker.position;
            if (isLyThong)
                return basePos + new Vector3(-0.75f, 0f, -0.6f); // Lý Thông đứng bên trái
            else
                return basePos + new Vector3(0.75f, 0f, -0.6f);  // Mẹ Lý Thông đứng bên phải
        }

        // Fallback: Sử dụng tọa độ trong nhà chuẩn xác (Y = 1.03m là độ cao sàn nhà thực tế)
        if (isLyThong)
            return new Vector3(81.0f, 1.03f, 24.8f);
        else
            return new Vector3(82.4f, 1.03f, 24.8f);
    }

    private GameObject FindLyThongGameObject(LyThongGuide guide)
    {
        if (guide != null) return guide.gameObject;

        // Tìm tất cả GameObject kể cả inactive
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string lower = t.name.ToLower();
                if ((lower.Contains("sage") || lower.Contains("wayfarer") || lower.Contains("lythong") || lower.Contains("ly_thong"))
                    && !lower.Contains("home") && !lower.Contains("point") && !lower.Contains("bed") && !lower.Contains("camera"))
                {
                    if (t.GetComponent<Animator>() != null || t.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    {
                        return t.gameObject;
                    }
                }
            }
        }
        return null;
    }

    private GameObject FindMotherGameObject(LyThongGuide guide)
    {
        if (guide != null)
        {
            if (guide.motherNPC != null) return guide.motherNPC.gameObject;
            if (guide.homePoint != null && guide.homePoint.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                return guide.homePoint.gameObject;
        }

        // Tìm tất cả GameObject kể cả inactive
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string lower = t.name.ToLower();
                if ((lower.Contains("elder") || lower.Contains("mother") || lower.Contains("melythong") || lower.Contains("me_lythong"))
                    && !lower.Contains("point") && !lower.Contains("light") && !lower.Contains("bed"))
                {
                    if (t.GetComponent<Animator>() != null || t.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    {
                        return t.gameObject;
                    }
                }
            }
        }
        return null;
    }

    void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Chapter2_LyThong") return;
        if (!StoryStateManager.IsChanTinhDefeated || !StoryStateManager.IsReturningToChapter2) return;
        if (isTransitioningToChapter4) return;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh");
            if (p != null) playerTransform = p.transform;
            return;
        }

        // Kiểm tra khoảng cách tới Lý Thông hoặc Mẹ Lý Thông
        float distToLyThong = lyThongTransform != null ? Vector3.Distance(playerTransform.position, lyThongTransform.position) : 999f;
        float distToMother = motherTransform != null ? Vector3.Distance(playerTransform.position, motherTransform.position) : 999f;
        float minDist = Mathf.Min(distToLyThong, distToMother);

        isInInteractionRange = (minDist <= interactionDistance);

        // Nhận phím tương tác
        bool interactPressed = false;
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            interactPressed = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            interactPressed = true;
        }
        if (!interactPressed)
        {
            try { interactPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); } catch { }
        }

        if (isDialogueActive)
        {
            if (interactPressed)
            {
                AdvanceDialogue();
            }
        }
        else
        {
            // Nếu chưa hoàn thành hội thoại và đang đứng gần NPC -> Bấm phím để bắt đầu
            if (isInInteractionRange && !StoryStateManager.IsReturnDialogueCompleted && !isTransitioningToChapter4 && interactPressed)
            {
                StartReturnDialogue();
            }
        }
    }

    private void StartReturnDialogue()
    {
        isDialogueActive = true;
        currentDialogueStep = 0;

        // Tạm khóa di chuyển của Thạch Sanh khi hội thoại
        if (playerMovement != null) playerMovement.enabled = false;

        DisplayCurrentDialogue();
    }

    private void AdvanceDialogue()
    {
        currentDialogueStep++;
        if (currentDialogueStep < returnDialogues.Length)
        {
            DisplayCurrentDialogue();
        }
        else
        {
            EndReturnDialogue();
        }
    }

    private void DisplayCurrentDialogue()
    {
        DialogueLine line = returnDialogues[currentDialogueStep];
        string text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(line.speakerColor)}>{line.speaker}:</color></b>\n\"{line.content}\"";

        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void EndReturnDialogue()
    {
        if (isTransitioningToChapter4) return;

        isDialogueActive = false;
        StoryStateManager.IsReturnDialogueCompleted = true;

        if (dialogueText != null) dialogueText.text = "";

        // Cập nhật thông báo hoàn thành Chương 2 và bắt đầu chuyển cảnh
        UpdateQuestBanner("<color=#00FF00>HOÀN THÀNH CHƯƠNG 2!</color>\nĐang chuyển sang Chương 4...");
        Debug.Log("[Chapter2ReturnManager] Đã xem hết tất cả câu thoại và đóng hộp thoại cuối cùng. Chờ 2 giây rồi chuyển sang Chapter 4...");

        StartCoroutine(TransitionToChapter4Routine());
    }

    private IEnumerator TransitionToChapter4Routine()
    {
        isTransitioningToChapter4 = true;

        // Tạm khóa di chuyển để tránh việc nhân vật di chuyển hoặc kích hoạt lại hội thoại trong 2 giây chờ chuyển cảnh
        if (playerMovement != null) playerMovement.enabled = false;

        // Chờ đúng 2 giây sau khi đóng hộp thoại cuối cùng theo yêu cầu
        yield return new WaitForSeconds(2.0f);

        // Lưu trạng thái trước khi chuyển sang Chapter 4
        StoryStateManager.IsChapter4Reached = true;
        StoryStateManager.HasChanTinhHead = false; // Lý Thông đã lấy đầu Chằn Tinh đem nộp triều đình cướp công
        StoryStateManager.IsReturningToChapter2 = false;

        Debug.Log("[Chapter2ReturnManager] Đang chuyển sang Scene Chapter4_Lythongcuopcong...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Chapter4_Lythongcuopcong");
    }

    private void UpdateQuestBanner(string text)
    {
        QuestManager qm = Object.FindAnyObjectByType<QuestManager>();
        if (qm != null && qm.questText != null)
        {
            qm.questText.text = text;
            qm.questText.color = Color.yellow;
            qm.questText.gameObject.SetActive(true);
        }
        else if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.gameObject.SetActive(true);
        }
    }

    void OnGUI()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Chapter2_LyThong") return;
        if (!StoryStateManager.IsChanTinhDefeated || !StoryStateManager.IsReturningToChapter2) return;

        // 0. Hiển thị thông báo khi đang trong 2 giây chờ chuyển cảnh sang Chapter 4
        if (isTransitioningToChapter4)
        {
            GUIStyle transStyle = new GUIStyle(GUI.skin.box);
            transStyle.fontSize = 20;
            transStyle.alignment = TextAnchor.MiddleCenter;
            transStyle.normal.textColor = Color.green;
            transStyle.richText = true;

            float width = 460f;
            float height = 60f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - 140f;

            GUI.Box(new Rect(left, top, width, height), "<b><color=#00FF00>HOÀN THÀNH CHƯƠNG 2!</color></b>\n<size=16>Đang chuyển sang Chương 4...</size>", transStyle);
            return;
        }

        // 1. Hiển thị hộp thoại khi đang đối thoại
        if (isDialogueActive && currentDialogueStep < returnDialogues.Length)
        {
            DialogueLine line = returnDialogues[currentDialogueStep];

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 20;
            boxStyle.wordWrap = true;
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(30, 30, 20, 20);
            boxStyle.normal.textColor = Color.white;
            boxStyle.richText = true;

            float width = Screen.width * 0.8f;
            float height = 170f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - height - 40f;

            string colorHex = ColorUtility.ToHtmlStringRGB(line.speakerColor);
            string fullMsg = $"<size=22><b><color=#{colorHex}>{line.speaker}</color></b></size>\n\n<size=19>\"{line.content}\"</size>\n\n<color=#FFD700><size=14>(Bấm E / Space / Chuột trái để tiếp tục)</size></color>";

            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
        // 2. Hiển thị gợi ý phím bấm khi đứng gần hai mẹ con
        else if (isInInteractionRange)
        {
            GUIStyle hintStyle = new GUIStyle(GUI.skin.box);
            hintStyle.fontSize = 18;
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.normal.textColor = Color.yellow;
            hintStyle.richText = true;

            float width = 360f;
            float height = 50f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - 130f;

            string prompt = StoryStateManager.IsReturnDialogueCompleted
                ? "<color=#FFFFFF>Lý Thông:</color> \"Em mau về gốc đa trốn đi!\""
                : "<b>[E] Nói chuyện với Lý Thông</b>";

            GUI.Box(new Rect(left, top, width, height), prompt, hintStyle);
        }
    }
}
