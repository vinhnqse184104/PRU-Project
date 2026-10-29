using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

public class Chapter4SoldierEscort : MonoBehaviour
{
    public static Chapter4SoldierEscort Instance { get; private set; }

    public enum EscortState
    {
        Initializing, Escorting, ArrivedAtKing, InDialogue, SteppingForward, Completed
    }

    [Header("Trạng thái hiện tại")]
    public EscortState currentState = EscortState.Initializing;

    [Header("Nhân vật tham chiếu")]
    public Transform lyThong;
    public Transform escortSoldier;
    public Transform king;
    public Transform princess;

    [Header("GÁN THỦ CÔNG ANIMATOR")]
    public Animator lyThongAnim;
    public Animator soldierAnim;

    [Header("Đường đi (Waypoints)")]
    public Transform[] waypoints;
    public int currentWaypointIndex = 0;

    [Header("Thông số di chuyển")]
    public float soldierSpeed = 2.8f;

    [Tooltip("Khoảng cách Lý Thông theo sau lưng lính")]
    [Range(1.5f, 2.5f)]
    public float followDistance = 2.0f;

    [Tooltip("Khoảng cách Lý Thông đi lệch sang bên PHẢI lính để không bị vướng")]
    [Range(0f, 2.0f)]
    public float rightOffset = 1.2f; // <--- Thông số mới giúp Lý Thông dạt sang phải

    public float rotationSpeed = 6.0f;
    public float waypointTolerance = 1.0f;

    [Header("Camera")]
    public Camera mainCamera;
    public Transform dialogueCameraPoint;
    public float cameraTransitionSpeed = 2.0f;

    [Header("UI Hội thoại Canvas (uGUI)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI dialogueText;

    [Header("Âm thanh Lồng tiếng Hội thoại")]
    public AudioSource loaHoiThoai;
    public AudioClip[] mangAmThanhHoiThoai; // Chứa 7 file ghi âm

    [Header("Âm thanh Bước chân Cắt cảnh")]
    public AudioSource loaBuocChanLyThong;
    public AudioSource loaBuocChanLinh;

    // Component nội bộ
    private CharacterController lyThongCC;
    private CharacterController soldierCC;
    private LyThongPlayerController lyThongInputController;
    private CameraFollow camFollow;

    private string soldierSpeedParam = null;
    private string soldierWalkingBoolParam = null;
    private string lyThongSpeedParam = null;
    private string lyThongWalkingBoolParam = null;
    private bool animParamsCached = false;
    private bool hasTriggeredArrival = false;

    public struct DialogueEntry
    {
        public string speaker;
        public string message;
        public Color speakerColor;

        public DialogueEntry(string speaker, string message, Color speakerColor)
        {
            this.speaker = speaker; this.message = message; this.speakerColor = speakerColor;
        }
    }

    private readonly DialogueEntry[] scriptLines = new DialogueEntry[]
    {
        new DialogueEntry("LÍNH", "Bẩm bệ hạ! Người có công tiêu diệt Chằn Tinh đã đến!", new Color(0.4f, 0.8f, 1.0f)),
        new DialogueEntry("NHÀ VUA", "Cho hắn tiến lên!", new Color(1.0f, 0.84f, 0.0f)),
        new DialogueEntry("LÝ THÔNG", "Muôn tâu bệ hạ! Chính thần đã tiêu diệt Chằn Tinh, trừ hại cho dân!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueEntry("NHÀ VUA", "Ngươi quả là người tài giỏi! Trẫm phong cho ngươi làm Quận công!", new Color(1.0f, 0.84f, 0.0f)),
        new DialogueEntry("LÝ THÔNG", "Thần xin đa tạ bệ hạ!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueEntry("CÔNG CHÚA", "Phụ vương, mong rằng từ nay dân chúng sẽ được bình yên.", new Color(1.0f, 0.6f, 0.75f)),
        new DialogueEntry("NHÀ VUA", "Truyền ban thưởng cho Lý Thông!", new Color(1.0f, 0.84f, 0.0f))
    };

    private int currentDialogueStep = 0;
    private bool isTransitioningCamera = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }
        FindReferences();
        LockPlayerInput();
        EnsureCharacterControllers();
        EnsureAnimators();
    }

    void Start()
    {
        FindReferences();
        LockPlayerInput();
        EnsureCharacterControllers();
        EnsureAnimators();
        StartCoroutine(StartEscortSequence());
    }

    public void FindReferences()
    {
        if (lyThong == null)
        {
            GameObject ltObj = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
            if (ltObj != null) lyThong = ltObj.transform;
        }

        if (escortSoldier == null && lyThong != null)
        {
            GameObject leaderNamed = GameObject.Find("Linh_EscortLeader");
            if (leaderNamed != null) { escortSoldier = leaderNamed.transform; }
            else
            {
                float minDistance = float.MaxValue; GameObject closestSoldier = null;
                foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (go.name == "Linh" || go.name == "Linh_EscortLeader")
                    {
                        float dist = Vector3.Distance(go.transform.position, lyThong.position);
                        if (dist < minDistance && dist < 12.0f) { minDistance = dist; closestSoldier = go; }
                    }
                }
                if (closestSoldier != null) escortSoldier = closestSoldier.transform;
            }
        }

        if (king == null)
        {
            GameObject kObj = GameObject.Find("Meshy_AI_Dragon_Emperor_Rising_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin") ?? GameObject.Find("vua") ?? GameObject.Find("King");
            if (kObj != null) king = kObj.transform;
        }

        if (princess == null)
        {
            GameObject pObj = GameObject.Find("Congchua") ?? GameObject.Find("congchua") ?? GameObject.Find("Princess");
            if (pObj != null) { princess = (pObj.name == "Congchua_Model" && pObj.transform.parent != null) ? pObj.transform.parent : pObj.transform; }
        }

        if (mainCamera == null) mainCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (mainCamera != null) camFollow = mainCamera.GetComponent<CameraFollow>();

        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject wpGroup = GameObject.Find("Chapter4_EscortWaypoints");
            if (wpGroup != null && wpGroup.transform.childCount > 0)
            {
                waypoints = new Transform[wpGroup.transform.childCount];
                for (int i = 0; i < wpGroup.transform.childCount; i++) { waypoints[i] = wpGroup.transform.GetChild(i); }
            }
        }

        if (dialogueCameraPoint == null)
        {
            GameObject dcp = GameObject.Find("Chapter4_DialogueCameraPoint");
            if (dcp != null) dialogueCameraPoint = dcp.transform;
        }

        if (dialoguePanel == null)
        {
            GameObject panelObj = GameObject.Find("DialoguePanel");
            if (panelObj != null) dialoguePanel = panelObj;
            else
            {
                GameObject canvasObj = GameObject.Find("Chapter4_DialogueCanvas");
                if (canvasObj != null)
                {
                    Transform tPanel = canvasObj.transform.Find("DialoguePanel");
                    if (tPanel != null) dialoguePanel = tPanel.gameObject;
                }
            }
        }

        if (dialoguePanel != null)
        {
            if (speakerText == null) { Transform sObj = dialoguePanel.transform.Find("SpeakerText"); if (sObj != null) speakerText = sObj.GetComponent<TextMeshProUGUI>(); }
            if (contentText == null) { Transform cObj = dialoguePanel.transform.Find("ContentText"); if (cObj != null) contentText = cObj.GetComponent<TextMeshProUGUI>(); }
            if (hintText == null) { Transform hObj = dialoguePanel.transform.Find("HintText"); if (hObj != null) hintText = hObj.GetComponent<TextMeshProUGUI>(); }
        }
    }

    private void LockPlayerInput()
    {
        if (lyThong != null)
        {
            lyThongInputController = lyThong.GetComponent<LyThongPlayerController>();
            if (lyThongInputController != null && lyThongInputController.enabled) { lyThongInputController.enabled = false; }
        }
    }

    private void EnsureCharacterControllers()
    {
        if (lyThong != null)
        {
            lyThongCC = lyThong.GetComponent<CharacterController>();
            if (lyThongCC == null)
            {
                lyThongCC = lyThong.gameObject.AddComponent<CharacterController>();
                lyThongCC.height = 1.8f; lyThongCC.radius = 0.35f; lyThongCC.center = new Vector3(0f, 0.9f, 0f);
            }
        }

        if (escortSoldier != null)
        {
            soldierCC = escortSoldier.GetComponent<CharacterController>();
            if (soldierCC == null)
            {
                soldierCC = escortSoldier.gameObject.AddComponent<CharacterController>();
                soldierCC.height = 1.8f; soldierCC.radius = 0.35f; soldierCC.center = new Vector3(0f, 0.9f, 0f); soldierCC.stepOffset = 0.3f; soldierCC.slopeLimit = 45f;
            }
        }
    }

    private static readonly string[] SPEED_FLOAT_CANDIDATES = new string[] { "Speed", "MoveSpeed", "moveSpeed", "speed" };
    private static readonly string[] WALKING_BOOL_CANDIDATES = new string[] { "IsWalking", "isWalking", "Walking", "walking", "Walk", "walk" };

    private static string FindParameterName(Animator anim, string[] candidates, AnimatorControllerParameterType type)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return null;
        try
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == type)
                {
                    foreach (var candidate in candidates) { if (string.Equals(p.name, candidate, System.StringComparison.OrdinalIgnoreCase)) return p.name; }
                }
            }
        }
        catch { }
        return null;
    }

    public void CacheAnimatorParameters()
    {
        if (soldierAnim != null && soldierAnim.runtimeAnimatorController != null)
        {
            soldierSpeedParam = FindParameterName(soldierAnim, SPEED_FLOAT_CANDIDATES, AnimatorControllerParameterType.Float);
            soldierWalkingBoolParam = FindParameterName(soldierAnim, WALKING_BOOL_CANDIDATES, AnimatorControllerParameterType.Bool);
        }

        if (lyThongAnim != null && lyThongAnim.runtimeAnimatorController != null)
        {
            lyThongSpeedParam = FindParameterName(lyThongAnim, SPEED_FLOAT_CANDIDATES, AnimatorControllerParameterType.Float);
            lyThongWalkingBoolParam = FindParameterName(lyThongAnim, WALKING_BOOL_CANDIDATES, AnimatorControllerParameterType.Bool);
        }
        animParamsCached = (soldierAnim != null && soldierAnim.runtimeAnimatorController != null) && (lyThongAnim != null && lyThongAnim.runtimeAnimatorController != null);
    }

    private void EnsureAnimators()
    {
        if (lyThongAnim == null && lyThong != null)
        {
            lyThongAnim = lyThong.GetComponent<Animator>();
            if (lyThongAnim == null) lyThongAnim = lyThong.GetComponentInChildren<Animator>();
        }
        if (lyThongAnim != null) lyThongAnim.applyRootMotion = false;

        if (soldierAnim == null && escortSoldier != null)
        {
            soldierAnim = escortSoldier.GetComponent<Animator>();
            if (soldierAnim == null) soldierAnim = escortSoldier.GetComponentInChildren<Animator>();
        }
        if (soldierAnim != null) soldierAnim.applyRootMotion = false;
        CacheAnimatorParameters();
    }

    private IEnumerator StartEscortSequence()
    {
        yield return new WaitForSeconds(0.5f);
        LockPlayerInput();

        if (camFollow != null && lyThong != null)
        {
            camFollow.target = lyThong; camFollow.distance = 4.5f; camFollow.heightOffset = 1.8f; camFollow.enabled = true;
        }

        currentWaypointIndex = 0;
        if (waypoints != null && waypoints.Length > 1 && escortSoldier != null)
        {
            float distToStart = Vector3.Distance(escortSoldier.position, waypoints[0].position);
            if (distToStart < 2.0f) currentWaypointIndex = 1;
        }

        hasTriggeredArrival = false; currentState = EscortState.Escorting;
    }

    void Update()
    {
        if (lyThongInputController != null && lyThongInputController.enabled) lyThongInputController.enabled = false;

        switch (currentState)
        {
            case EscortState.Escorting: UpdateEscortMovement(); break;
            case EscortState.ArrivedAtKing: UpdateArrivedFacing(); break;
            case EscortState.InDialogue: UpdateDialogueInput(); MaintainDialogueFacing(); break;
            case EscortState.SteppingForward: MaintainDialogueFacing(); break;
        }
    }

    private void UpdateEscortMovement()
    {
        if (escortSoldier == null || lyThong == null || waypoints == null || waypoints.Length == 0) return;

        bool soldierReachedDest = (currentWaypointIndex >= waypoints.Length);

        if (!soldierReachedDest)
        {
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            Vector3 soldierPos = escortSoldier.position;
            Vector3 targetPos = targetWaypoint.position;

            Vector3 soldierMoveDir = (targetPos - soldierPos);
            soldierMoveDir.y = 0f;
            float distToWaypoint = soldierMoveDir.magnitude;

            if (distToWaypoint > waypointTolerance)
            {
                soldierMoveDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(soldierMoveDir);
                escortSoldier.rotation = Quaternion.Slerp(escortSoldier.rotation, targetRot, rotationSpeed * Time.deltaTime);

                Vector3 moveVelocity = escortSoldier.forward * soldierSpeed;
                moveVelocity.y = -9.81f;

                if (soldierCC != null && soldierCC.enabled) soldierCC.Move(moveVelocity * Time.deltaTime);
                else escortSoldier.position += moveVelocity * Time.deltaTime;

                SetSoldierWalking(true);
            }
            else
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length) { soldierReachedDest = true; SetSoldierWalking(false); }
            }
        }
        else { SetSoldierWalking(false); }

        // ==========================================
        // CẬP NHẬT: Thêm thông số rightOffset để dạt sang phải
        Vector3 lyThongPos = lyThong.position;
        Vector3 behindSoldierPos = escortSoldier.position - (escortSoldier.forward * followDistance) + (escortSoldier.right * rightOffset);
        behindSoldierPos.y = lyThongPos.y;
        // ==========================================

        Vector3 toSoldier = (escortSoldier.position - lyThongPos); toSoldier.y = 0f;
        float actualDistToSoldier = toSoldier.magnitude;

        Vector3 lyThongMoveDir = (behindSoldierPos - lyThongPos); lyThongMoveDir.y = 0f;
        float distToFollowSpot = lyThongMoveDir.magnitude;

        bool lyThongIsMoving = false;
        bool lyThongArrived = soldierReachedDest && (actualDistToSoldier <= (followDistance + 0.45f) || distToFollowSpot <= 0.4f);

        if (!lyThongArrived && (actualDistToSoldier > (followDistance * 0.9f) || distToFollowSpot > 0.3f))
        {
            if (lyThongMoveDir.sqrMagnitude > 0.001f)
            {
                lyThongMoveDir.Normalize();
                Quaternion ltTargetRot = Quaternion.LookRotation(lyThongMoveDir);
                lyThong.rotation = Quaternion.Slerp(lyThong.rotation, ltTargetRot, rotationSpeed * Time.deltaTime);

                float speedMultiplier = 1.0f;
                if (actualDistToSoldier > followDistance * 1.3f) speedMultiplier = 1.25f;
                else if (actualDistToSoldier < followDistance) speedMultiplier = 0.85f;

                Vector3 ltVelocity = lyThong.forward * (soldierSpeed * speedMultiplier); ltVelocity.y = -9.81f;

                if (lyThongCC != null && lyThongCC.enabled) lyThongCC.Move(ltVelocity * Time.deltaTime);
                else lyThong.position += ltVelocity * Time.deltaTime;

                lyThongIsMoving = true;
            }
        }

        SetLyThongWalking(lyThongIsMoving);

        if (soldierReachedDest && (!lyThongIsMoving || lyThongArrived) && !hasTriggeredArrival)
        {
            hasTriggeredArrival = true; OnBothArrivedAtDestination();
        }
    }

    private void SetSoldierWalking(bool walking)
    {
        if (soldierAnim != null)
        {
            if (!animParamsCached) CacheAnimatorParameters();
            soldierAnim.speed = 1.0f;
            if (!string.IsNullOrEmpty(soldierSpeedParam)) soldierAnim.SetFloat(soldierSpeedParam, walking ? 1.0f : 0.0f);
            if (!string.IsNullOrEmpty(soldierWalkingBoolParam)) soldierAnim.SetBool(soldierWalkingBoolParam, walking);
        }

        // ==========================================
        // BẬT/TẮT TIẾNG BƯỚC CHÂN LÍNH
        // ==========================================
        if (walking)
        {
            if (loaBuocChanLinh != null && !loaBuocChanLinh.isPlaying) loaBuocChanLinh.Play();
        }
        else
        {
            if (loaBuocChanLinh != null && loaBuocChanLinh.isPlaying) loaBuocChanLinh.Stop();
        }
    }

    private void SetLyThongWalking(bool walking)
    {
        if (lyThongAnim != null)
        {
            if (!animParamsCached) CacheAnimatorParameters();
            if (!string.IsNullOrEmpty(lyThongSpeedParam)) lyThongAnim.SetFloat(lyThongSpeedParam, walking ? 1.0f : 0.0f);
            if (!string.IsNullOrEmpty(lyThongWalkingBoolParam)) lyThongAnim.SetBool(lyThongWalkingBoolParam, walking);
        }

        // ==========================================
        // BẬT/TẮT TIẾNG BƯỚC CHÂN LÝ THÔNG
        // ==========================================
        if (walking)
        {
            if (loaBuocChanLyThong != null && !loaBuocChanLyThong.isPlaying) loaBuocChanLyThong.Play();
        }
        else
        {
            if (loaBuocChanLyThong != null && loaBuocChanLyThong.isPlaying) loaBuocChanLyThong.Stop();
        }
    }

    private void OnBothArrivedAtDestination()
    {
        SetSoldierWalking(false); SetLyThongWalking(false);
        currentState = EscortState.ArrivedAtKing;
        StartCoroutine(ArrivedAndPrepareDialogueRoutine());
    }

    private void UpdateArrivedFacing()
    {
        RotateCharactersFacing();
        if (isTransitioningCamera && mainCamera != null && dialogueCameraPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, dialogueCameraPoint.position, cameraTransitionSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, dialogueCameraPoint.rotation, cameraTransitionSpeed * Time.deltaTime);
        }
    }

    private void MaintainDialogueFacing() { RotateCharactersFacing(); }

    private void RotateCharactersFacing()
    {
        if (lyThong == null) return;
        Vector3 lyThongPos = lyThong.position;

        if (king != null)
        {
            Vector3 dirK = (lyThongPos - king.position); dirK.y = 0f;
            if (dirK.sqrMagnitude > 0.01f) { king.rotation = Quaternion.Slerp(king.rotation, Quaternion.LookRotation(dirK.normalized), rotationSpeed * Time.deltaTime); }
        }

        if (princess != null)
        {
            Vector3 dirP = (lyThongPos - princess.position); dirP.y = 0f;
            if (dirP.sqrMagnitude > 0.01f)
            {
                Quaternion yawRot = Quaternion.LookRotation(dirP.normalized);
                Transform modelChild = princess.Find("Congchua_Model");
                if (modelChild != null)
                {
                    princess.rotation = Quaternion.Slerp(princess.rotation, yawRot, rotationSpeed * Time.deltaTime);
                    modelChild.localPosition = Vector3.zero; modelChild.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }
                else
                {
                    princess.rotation = Quaternion.Slerp(princess.rotation, yawRot * Quaternion.Euler(-90f, 0f, 0f), rotationSpeed * Time.deltaTime);
                }
            }
        }

        if (king != null)
        {
            Vector3 dirL = (king.position - lyThong.position); dirL.y = 0f;
            if (dirL.sqrMagnitude > 0.01f) { lyThong.rotation = Quaternion.Slerp(lyThong.rotation, Quaternion.LookRotation(dirL.normalized), rotationSpeed * Time.deltaTime); }
        }

        if (escortSoldier != null && king != null)
        {
            Vector3 dirS = (king.position - escortSoldier.position); dirS.y = 0f;
            if (dirS.sqrMagnitude > 0.01f) { escortSoldier.rotation = Quaternion.Slerp(escortSoldier.rotation, Quaternion.LookRotation(dirS.normalized), rotationSpeed * Time.deltaTime); }
        }
    }

    private IEnumerator ArrivedAndPrepareDialogueRoutine()
    {
        if (camFollow != null) camFollow.enabled = false;
        if (dialogueCameraPoint == null && king != null && lyThong != null)
        {
            GameObject dcp = GameObject.Find("Chapter4_DialogueCameraPoint");
            if (dcp == null)
            {
                dcp = new GameObject("Chapter4_DialogueCameraPoint");
                dcp.transform.position = new Vector3(-51.5f, -8.2f, 17.5f); dcp.transform.rotation = Quaternion.Euler(10f, 32f, 0f);
            }
            dialogueCameraPoint = dcp.transform;
        }

        isTransitioningCamera = true;
        yield return new WaitForSeconds(1.2f);
        isTransitioningCamera = false;

        if (mainCamera != null && dialogueCameraPoint != null) { mainCamera.transform.position = dialogueCameraPoint.position; mainCamera.transform.rotation = dialogueCameraPoint.rotation; }
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

        SetSoldierWalking(false); SetLyThongWalking(false);
        currentDialogueStep = 0; currentState = EscortState.InDialogue; UpdateDialogueUI();
    }

    private void UpdateDialogueInput()
    {
        bool nextPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) nextPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) nextPressed = true;
#endif
        if (!nextPressed) { try { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) nextPressed = true; } catch { } }

        if (nextPressed) AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        currentDialogueStep++;

        if (currentDialogueStep == 2)
        {
            StartCoroutine(LyThongStepForwardRoutine());
        }
        else if (currentDialogueStep < scriptLines.Length)
        {
            UpdateDialogueUI();
        }
        else
        {
            EndDialogue();
        }
    }

    private IEnumerator LyThongStepForwardRoutine()
    {
        currentState = EscortState.SteppingForward;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";

        SetLyThongWalking(true);

        float timeToWalk = 1.5f;
        float timer = 0f;

        while (timer < timeToWalk)
        {
            timer += Time.deltaTime;
            if (king != null && Vector3.Distance(lyThong.position, king.position) < 2.0f)
            {
                break;
            }

            Vector3 moveVelocity = lyThong.forward * (soldierSpeed * 0.8f);
            moveVelocity.y = -9.81f;

            if (lyThongCC != null && lyThongCC.enabled) lyThongCC.Move(moveVelocity * Time.deltaTime);
            else lyThong.position += moveVelocity * Time.deltaTime;

            yield return null;
        }

        SetLyThongWalking(false);
        yield return new WaitForSeconds(0.5f);
        currentState = EscortState.InDialogue;
        UpdateDialogueUI();
    }

    private void UpdateDialogueUI()
    {
        if (currentDialogueStep < 0 || currentDialogueStep >= scriptLines.Length) return;

        // ========================================================
        // TỰ ĐỘNG PHÁT ÂM THANH THEO CÂU THOẠI
        // ========================================================
        if (loaHoiThoai != null)
        {
            loaHoiThoai.Stop(); // Cắt tiếng cũ nếu người chơi bấm lướt nhanh

            if (mangAmThanhHoiThoai != null && currentDialogueStep < mangAmThanhHoiThoai.Length)
            {
                AudioClip clipDePhat = mangAmThanhHoiThoai[currentDialogueStep];
                if (clipDePhat != null)
                {
                    loaHoiThoai.PlayOneShot(clipDePhat);
                }
            }
        }
        // ========================================================

        DialogueEntry entry = scriptLines[currentDialogueStep];
        string hexColor = ColorUtility.ToHtmlStringRGB(entry.speakerColor);
        string formatted = $"<b><color=#{hexColor}>{entry.speaker}:</color></b>\n\"{entry.message}\"";

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null) { speakerText.text = entry.speaker + ":"; speakerText.color = entry.speakerColor; }
            if (contentText != null) contentText.text = $"\"{entry.message}\"";
            if (hintText != null) hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";
        }
        if (dialogueText != null) { dialogueText.text = formatted; dialogueText.gameObject.SetActive(true); }
    }

    private void EndDialogue()
    {
        // ========================================================
        // TẮT TIẾNG KHI KẾT THÚC HỘI THOẠI
        // ========================================================
        if (loaHoiThoai != null) loaHoiThoai.Stop();

        currentState = EscortState.Completed;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        StartCoroutine(TransitionToChapter2OfficialRoutine());
    }

    private float officialTransitionTimer = 5.0f;
    private IEnumerator TransitionToChapter2OfficialRoutine()
    {
        officialTransitionTimer = 5.0f;

        while (officialTransitionTimer > 0f)
        {
            officialTransitionTimer -= Time.deltaTime;
            yield return null;
        }

        // Lưu trạng thái trước khi tải lại scene nhà Lý Thông.
        StoryStateManager.LyThongReturnedAsOfficial = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            "Chapter2_LyThong"
        );
    }

    private Texture2D boxBgTexture;
    private Texture2D GetBoxBackground()
    {
        if (boxBgTexture == null)
        {
            boxBgTexture = new Texture2D(1, 1); boxBgTexture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.65f)); boxBgTexture.Apply();
        }
        return boxBgTexture;
    }

    void OnGUI()
    {
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy) return;
        GUI.depth = -100;

        if (currentState == EscortState.InDialogue && currentDialogueStep < scriptLines.Length)
        {
            DialogueEntry line = scriptLines[currentDialogueStep];
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box); boxStyle.normal.background = GetBoxBackground(); boxStyle.wordWrap = true; boxStyle.alignment = TextAnchor.UpperLeft; boxStyle.padding = new RectOffset(35, 35, 18, 18); boxStyle.normal.textColor = Color.white; boxStyle.richText = true;
            float width = Mathf.Min(Screen.width * 0.78f, 1100f), height = 215f, left = (Screen.width - width) / 2f, top = Screen.height - height - 30f;
            string colorHex = ColorUtility.ToHtmlStringRGB(line.speakerColor);
            string fullMsg = $"<size=34><b><color=#{colorHex}>{line.speaker}:</color></b></size>\n<size=28>\"{line.message}\"</size>\n\n<color=#FFD700><size=20><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";
            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
        else if (currentState == EscortState.Completed)
        {
            GUIStyle completeStyle = new GUIStyle(GUI.skin.box); completeStyle.normal.background = GetBoxBackground(); completeStyle.fontSize = 17; completeStyle.alignment = TextAnchor.MiddleCenter; completeStyle.normal.textColor = Color.yellow; completeStyle.richText = true;
            float width = 520f, height = 70f, left = (Screen.width - width) / 2f, top = Screen.height - 110f;
            int remainingSeconds = Mathf.CeilToInt(Mathf.Max(0f, officialTransitionTimer));
            GUI.Box(new Rect(left, top, width, height), $"<b><color=#00FF00>LÝ THÔNG ĐÃ ĐƯỢC PHONG LÀM QUẬN CÔNG!</color></b>\n<size=13><color=#FFD700>Đang trở về nhà khoe với mẹ ({remainingSeconds}s)...</color></size>", completeStyle);
        }
    }
}