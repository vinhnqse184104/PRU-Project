using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Quản lý phân cảnh Chapter 4 -> Chapter 2: Lý Thông về nhà gọi mẹ và khoe chức Quận công.
/// Kích hoạt khi StoryStateManager.LyThongReturnedAsOfficial == true.
/// </summary>
public class Chapter2OfficialReturnCutscene : MonoBehaviour
{
    public static Chapter2OfficialReturnCutscene Instance { get; private set; }

    [Header("Cấu hình kiểm tra")]
    [Tooltip("Bật tùy chọn này để test phân cảnh trực tiếp trong Editor mà không cần qua Chapter 4")]
    public bool forceRunInEditor = false;

    [Header("Nhân vật")]
    public GameObject lyThongOfficialInstance;
    public GameObject motherInstance;
    public GameObject oldLyThongNPC;
    public GameObject thachSanhPlayer;

    [Header("Các điểm mốc (Tự động tìm hoặc tạo nếu trống)")]
    public Transform lyThongOfficialReturnPoint;
    public Transform motherInsidePoint;
    public Transform motherDoorPoint;
    public Transform cutsceneCameraPoint;

    [Header("Camera")]
    public Camera mainCamera;
    public Camera cutsceneCamera;

    [Header("Cửa nhà")]
    public DoorInteraction doorInteraction;

    [Header("UI Hội thoại Canvas uGUI (Chapter 4)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    private bool isCutsceneRunning = false;
    private bool isCutsceneCompleted = false;

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

    private readonly DialogueLine[] boastingDialogues = new DialogueLine[]
    {
        new DialogueLine("LÝ THÔNG", "Mẹ ơi! Con đã được nhà vua phong làm Quận công rồi!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueLine("MẸ LÝ THÔNG", "Thật sao con? Sao hôm nay con ăn mặc sang trọng thế này?", new Color(1.0f, 0.5f, 0.5f)),
        new DialogueLine("LÝ THÔNG", "Từ nay mẹ con ta được hưởng vinh hoa phú quý, không còn phải sống cảnh nghèo khó nữa!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueLine("MẸ LÝ THÔNG", "Ôi! Con trai của mẹ thật giỏi! Mẹ vui quá!", new Color(1.0f, 0.5f, 0.5f)),
        new DialogueLine("LÝ THÔNG", "Mẹ cứ yên tâm! Từ nay không ai dám coi thường mẹ con ta nữa!", new Color(1.0f, 0.72f, 0.2f))
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Chỉ chạy khi trạng thái LyThongReturnedAsOfficial được kích hoạt hoặc bật forceRunInEditor
        bool shouldRun = StoryStateManager.LyThongReturnedAsOfficial;
#if UNITY_EDITOR
        if (forceRunInEditor) shouldRun = true;
#endif
        if (!shouldRun)
        {
            enabled = false;
            return;
        }

        Debug.Log("[Chapter2OfficialReturnCutscene] Bắt đầu phân cảnh: Lý Thông về nhà gọi mẹ sau khi được phong Quận công.");

        SetupEnvironmentAndHideOldEntities();
        SetupMarkers();
        SetupLyThongOfficial();
        SetupMother();
        SetupCutsceneCamera();
        EnsureDialogueUI();

        StartCoroutine(RunOfficialReturnSequence());
    }

    /// <summary>
    /// Vô hiệu hoá Thạch Sanh, NPC Lý Thông cũ và các quest cũ của Chapter 2
    /// </summary>
    private void SetupEnvironmentAndHideOldEntities()
    {
        // 1. Ẩn Thạch Sanh để không spawn Thạch Sanh trong phân cảnh này
        if (thachSanhPlayer == null)
        {
            thachSanhPlayer = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh") ?? GameObject.Find("ThachSanh");
        }
        if (thachSanhPlayer != null)
        {
            thachSanhPlayer.SetActive(false);
            Debug.Log("[Chapter2OfficialReturnCutscene] Đã ẩn nhân vật Thạch Sanh.");
        }

        // 2. Ẩn mô hình Lý Thông dân làng cũ
        if (oldLyThongNPC == null)
        {
            oldLyThongNPC = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
        }
        if (oldLyThongNPC != null)
        {
            oldLyThongNPC.SetActive(false);
        }

        // 3. Tắt các script Chapter 2 cũ nếu còn hoạt động
        LyThongGuide guide = Object.FindAnyObjectByType<LyThongGuide>();
        if (guide != null) guide.enabled = false;

        Chapter2ReturnManager returnMgr = Object.FindAnyObjectByType<Chapter2ReturnManager>();
        if (returnMgr != null) returnMgr.enabled = false;

        QuestManager qm = Object.FindAnyObjectByType<QuestManager>();
        if (qm != null)
        {
            if (qm.questText != null) qm.questText.gameObject.SetActive(false);
            if (qm.lyThongNPC != null) qm.lyThongNPC.SetActive(false);
            qm.enabled = false;
        }

        // 4. Tìm cửa nhà
        if (doorInteraction == null)
        {
            doorInteraction = Object.FindAnyObjectByType<DoorInteraction>();
        }
    }

    /// <summary>
    /// Tạo hoặc liên kết 3 điểm mốc theo đúng toạ độ yêu cầu
    /// </summary>
    private void SetupMarkers()
    {
        // 1. Điểm xuất hiện của Lý Thông: (81.98, 0.2318305, 31.29)
        if (lyThongOfficialReturnPoint == null)
        {
            GameObject pt = GameObject.Find("LyThong_OfficialReturnPoint");
            if (pt == null)
            {
                pt = new GameObject("LyThong_OfficialReturnPoint");
                pt.transform.position = new Vector3(81.98f, 0.2318305f, 31.29f);
                pt.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Quay mặt về phía cửa nhà (-Z)
            }
            lyThongOfficialReturnPoint = pt.transform;
        }

        // 2. Điểm Mẹ bên trong nhà: (82.0, 0.25, 25.0)
        if (motherInsidePoint == null)
        {
            GameObject pt = GameObject.Find("Mother_InsidePoint");
            if (pt == null)
            {
                pt = new GameObject("Mother_InsidePoint");
                pt.transform.position = new Vector3(82.0f, 0.25f, 25.0f);
                pt.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Hướng ra cửa (+Z)
            }
            motherInsidePoint = pt.transform;
        }

        // 3. Điểm Mẹ trước cửa nhà: (81.98, 0.2318305, 29.5) đối diện Lý Thông
        if (motherDoorPoint == null)
        {
            GameObject pt = GameObject.Find("Mother_DoorPoint");
            if (pt == null)
            {
                pt = new GameObject("Mother_DoorPoint");
                pt.transform.position = new Vector3(81.98f, 0.2318305f, 29.5f);
                pt.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Hướng về Lý Thông (+Z)
            }
            motherDoorPoint = pt.transform;
        }
    }

    /// <summary>
    /// Khởi tạo hoặc định vị nhân vật Lý Thông Quận công
    /// </summary>
    private void SetupLyThongOfficial()
    {
        if (lyThongOfficialInstance == null)
        {
            lyThongOfficialInstance = GameObject.Find("LyThong_Official");
        }

        if (lyThongOfficialInstance == null)
        {
            // Tải model quan phục từ đường dẫn
            string fbxPath = "Assets/nhanvat/lý thông được phong chức/đứng yên/Meshy_AI_Imperial_Dragon_Warlo_biped_Animation_01a0d464-3ba4-7709-b908-7ce4a2ae12f4_withSkin.fbx";
            GameObject fbxPrefab = null;
#if UNITY_EDITOR
            fbxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
#endif
            if (fbxPrefab != null)
            {
                lyThongOfficialInstance = Instantiate(fbxPrefab);
                lyThongOfficialInstance.name = "LyThong_Official";
            }
        }

        if (lyThongOfficialInstance != null)
        {
            lyThongOfficialInstance.SetActive(true);

            // Gán đúng toạ độ yêu cầu: X = 81.98, Y = 0.2318305, Z = 31.29
            Vector3 targetPos = lyThongOfficialReturnPoint != null
                ? lyThongOfficialReturnPoint.position
                : new Vector3(81.98f, 0.2318305f, 31.29f);

            // Kiểm tra mặt đất giữ nguyên X và Z
            if (Physics.Raycast(new Vector3(targetPos.x, targetPos.y + 2f, targetPos.z), Vector3.down, out RaycastHit hit, 5f))
            {
                targetPos.y = hit.point.y + 0.02f;
            }

            lyThongOfficialInstance.transform.position = targetPos;
            lyThongOfficialInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Quay mặt vào cửa nhà
            lyThongOfficialInstance.transform.localScale = Vector3.one;

            // Vô hiệu hoá điều khiển người chơi (WASD)
            MonoBehaviour playerCtrl = lyThongOfficialInstance.GetComponent("LyThongPlayerController") as MonoBehaviour;
            if (playerCtrl != null) playerCtrl.enabled = false;

            CharacterController cc = lyThongOfficialInstance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Bật Animator ở trạng thái Idle
            Animator anim = lyThongOfficialInstance.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }

            Debug.Log($"[Chapter2OfficialReturnCutscene] Đã định vị Lý Thông Quận công tại {lyThongOfficialInstance.transform.position}");
        }
    }

    /// <summary>
    /// Đưa Mẹ Lý Thông vào bên trong nhà chuẩn bị bước ra
    /// </summary>
    private void SetupMother()
    {
        if (motherInstance == null)
        {
            // Tìm theo tên GameObject trong Scene
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjs)
            {
                string lower = go.name.ToLower();
                if ((lower.Contains("elder") || lower.Contains("melythong") || lower.Contains("mother")) &&
                    !lower.Contains("point") && !lower.Contains("camera"))
                {
                    motherInstance = go;
                    break;
                }
            }
        }

        if (motherInstance != null)
        {
            motherInstance.SetActive(true);

            // Đặt Mẹ ở bên trong nhà
            Vector3 insidePos = motherInsidePoint != null ? motherInsidePoint.position : new Vector3(82.0f, 0.25f, 25.0f);
            motherInstance.transform.position = insidePos;
            motherInstance.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Hướng mặt ra cửa nhà

            // Tắt NavMeshAgent để tránh bị cản trở bởi ngưỡng cửa
            UnityEngine.AI.NavMeshAgent agent = motherInstance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            Collider col = motherInstance.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Animator anim = motherInstance.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }

            Debug.Log($"[Chapter2OfficialReturnCutscene] Đã đặt Mẹ Lý Thông ban đầu ở trong nhà tại {motherInstance.transform.position}");
        }
    }

    /// <summary>
    /// Cấu hình góc máy quay bao trọn cửa nhà, Lý Thông và Mẹ
    /// </summary>
    private void SetupCutsceneCamera()
    {
        if (cutsceneCameraPoint == null)
        {
            GameObject pt = GameObject.Find("Chapter2_OfficialReturnCamera");
            if (pt == null)
            {
                pt = new GameObject("Chapter2_OfficialReturnCamera");
                // Góc chếch ngang nhìn rõ cửa nhà bên trái, Lý Thông bên phải
                pt.transform.position = new Vector3(78.0f, 1.45f, 31.8f);
                pt.transform.rotation = Quaternion.Euler(6.0f, 112.0f, 0f);
            }
            cutsceneCameraPoint = pt.transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && cutsceneCameraPoint != null)
        {
            mainCamera.transform.position = cutsceneCameraPoint.position;
            mainCamera.transform.rotation = cutsceneCameraPoint.rotation;

            CameraFollow cf = mainCamera.GetComponent<CameraFollow>();
            if (cf != null) cf.enabled = false;

            Debug.Log("[Chapter2OfficialReturnCutscene] Đã thiết lập góc Camera điện ảnh bao trọn bối cảnh trước nhà.");
        }
    }

    private void EnsureDialogueUI()
    {
        if (dialoguePanel == null || speakerText == null || contentText == null || hintText == null)
        {
            dialoguePanel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter2_DialogueCanvas", out speakerText, out contentText, out hintText);
        }
    }

    private void ShowDialogue(string speaker, string message, Color speakerColor)
    {
        EnsureDialogueUI();

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null)
            {
                speakerText.text = speaker + ":";
                speakerText.color = speakerColor;
            }
            if (contentText != null)
            {
                contentText.text = $"\"{message}\"";
            }
            if (hintText != null)
            {
                hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";
            }
        }
    }

    private void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private IEnumerator WaitForKeyPress()
    {
        yield return new WaitForSeconds(0.15f);
        while (true)
        {
            bool pressed = false;
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                             Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                pressed = true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressed = true;
            }
            if (!pressed)
            {
                try
                {
                    pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                              Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
                }
                catch { }
            }
            if (pressed) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// Tiến trình phân cảnh hoàn chỉnh
    /// </summary>
    private IEnumerator RunOfficialReturnSequence()
    {
        isCutsceneRunning = true;
        yield return new WaitForSeconds(0.6f);

        // BƯỚC 1: Lý Thông đứng trước nhà gọi Mẹ
        ShowDialogue("LÝ THÔNG", "Mẹ ơi! Mẹ mau ra đây! Con có tin vui muốn báo!", new Color(1.0f, 0.72f, 0.2f));
        yield return StartCoroutine(WaitForKeyPress());
        HideDialogue();

        // BƯỚC 2: Cửa tự động mở ra
        if (doorInteraction != null)
        {
            doorInteraction.OpenDoor();
        }
        yield return new WaitForSeconds(0.4f);

        // BƯỚC 3: Mẹ từ trong nhà đi ra cửa đối diện Lý Thông
        yield return StartCoroutine(MoveMotherToDoorRoutine());

        // BƯỚC 4: Hai nhân vật quay mặt về phía nhau
        AlignFacing();
        yield return new WaitForSeconds(0.4f);

        // BƯỚC 5: Hội thoại khoe chức (5 câu)
        for (int i = 0; i < boastingDialogues.Length; i++)
        {
            var line = boastingDialogues[i];
            ShowDialogue(line.speaker, line.content, line.speakerColor);
            yield return StartCoroutine(WaitForKeyPress());
        }

        HideDialogue();
        isCutsceneCompleted = true;
        isCutsceneRunning = false;

        Debug.Log("[Chapter2OfficialReturnCutscene] Phân cảnh Lý Thông khoe chức Quận công đã hoàn tất thành công!");
    }

    /// <summary>
    /// Di chuyển Mẹ từ trong nhà ra cửa một cách mượt mà và an toàn
    /// </summary>
    private IEnumerator MoveMotherToDoorRoutine()
    {
        if (motherInstance == null) yield break;

        Vector3 startPos = motherInstance.transform.position;
        Vector3 targetPos = motherDoorPoint != null ? motherDoorPoint.position : new Vector3(81.98f, 0.2318305f, 29.5f);

        // Hướng mặt về phía cửa trong lúc đi
        Vector3 walkDir = (targetPos - startPos);
        walkDir.y = 0f;
        if (walkDir.sqrMagnitude > 0.01f)
        {
            motherInstance.transform.rotation = Quaternion.LookRotation(walkDir.normalized);
        }

        // Bật cờ đi bộ nếu Animator có
        Animator anim = motherInstance.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetFloat("Speed", 1.0f);
        }

        float walkDuration = 2.4f;
        float elapsed = 0f;

        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / walkDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothT);
            motherInstance.transform.position = currentPos;
            yield return null;
        }

        motherInstance.transform.position = targetPos;

        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
        }
    }

    /// <summary>
    /// Xoay hai nhân vật hướng mặt nhìn nhau thẳng hàng
    /// </summary>
    private void AlignFacing()
    {
        if (lyThongOfficialInstance != null && motherInstance != null)
        {
            Vector3 toMother = motherInstance.transform.position - lyThongOfficialInstance.transform.position;
            toMother.y = 0f;
            if (toMother.sqrMagnitude > 0.01f)
            {
                lyThongOfficialInstance.transform.rotation = Quaternion.LookRotation(toMother.normalized);
            }

            Vector3 toLyThong = lyThongOfficialInstance.transform.position - motherInstance.transform.position;
            toLyThong.y = 0f;
            if (toLyThong.sqrMagnitude > 0.01f)
            {
                motherInstance.transform.rotation = Quaternion.LookRotation(toLyThong.normalized);
            }
        }
    }

    void OnGUI()
    {
        // Khi Canvas DialoguePanel đang hiển thị thì không vẽ OnGUI
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy) return;

        if (isCutsceneCompleted)
        {
            GUIStyle compStyle = new GUIStyle(GUI.skin.box);
            compStyle.fontSize = 18;
            compStyle.alignment = TextAnchor.MiddleCenter;
            compStyle.normal.textColor = Color.yellow;
            compStyle.richText = true;

            float width = 540f;
            float height = 65f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - 100f;

            GUI.Box(new Rect(left, top, width, height),
                "<b><color=#00FF00>LÝ THÔNG ĐÃ HOÀN TẤT BÁO CÔNG VỚI MẸ!</color></b>\n<size=14>Vinh hoa phú quý - Rạng rỡ tổ tông</size>",
                compStyle);
        }
    }
}
