using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

public class Chapter2OfficialReturnCutscene : MonoBehaviour
{
    public static Chapter2OfficialReturnCutscene Instance { get; private set; }

    [Header("Cấu hình kiểm tra")]
    public bool forceRunInEditor = false;

    [Header("Nhân vật")]
    public GameObject lyThongOfficialInstance;
    public GameObject motherInstance;
    public GameObject oldLyThongNPC;
    public GameObject thachSanhPlayer;

    [Header("Các điểm mốc")]
    public Transform lyThongOfficialReturnPoint;
    public Transform motherInsidePoint;
    public Transform motherDoorPoint;
    public Transform cutsceneCameraPoint;

    [Header("Camera")]
    public Camera mainCamera;
    public Camera cutsceneCamera;

    [Header("Cửa nhà")]
    public DoorInteraction doorInteraction;

    [Header("UI Hội thoại Canvas uGUI")]
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
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        bool shouldRun = StoryStateManager.LyThongReturnedAsOfficial;
#if UNITY_EDITOR
        if (forceRunInEditor) shouldRun = true;
#endif
        if (!shouldRun)
        {
            enabled = false;
            return;
        }

        SetupEnvironmentAndHideOldEntities();
        SetupMarkers();
        SetupLyThongOfficial();
        SetupMother();
        SetupCutsceneCamera();
        EnsureDialogueUI();

        StartCoroutine(RunOfficialReturnSequence());
    }

    private void SetupEnvironmentAndHideOldEntities()
    {
        if (thachSanhPlayer == null) thachSanhPlayer = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh") ?? GameObject.Find("ThachSanh");
        if (thachSanhPlayer != null) thachSanhPlayer.SetActive(false);

        if (oldLyThongNPC == null) oldLyThongNPC = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
        if (oldLyThongNPC != null) oldLyThongNPC.SetActive(false);

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

        if (doorInteraction == null) doorInteraction = Object.FindAnyObjectByType<DoorInteraction>();
    }

    private void SetupMarkers()
    {
        if (lyThongOfficialReturnPoint == null)
        {
            GameObject pt = GameObject.Find("LyThong_OfficialReturnPoint");
            if (pt == null)
            {
                pt = new GameObject("LyThong_OfficialReturnPoint");
                pt.transform.position = new Vector3(81.98f, 0.2318305f, 31.29f);
                pt.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            lyThongOfficialReturnPoint = pt.transform;
        }

        if (motherInsidePoint == null)
        {
            GameObject pt = GameObject.Find("Mother_InsidePoint");
            if (pt == null)
            {
                pt = new GameObject("Mother_InsidePoint");
                pt.transform.position = new Vector3(82.0f, 0.25f, 25.0f);
                pt.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            motherInsidePoint = pt.transform;
        }

        if (motherDoorPoint == null)
        {
            GameObject pt = GameObject.Find("Mother_DoorPoint");
            if (pt == null)
            {
                pt = new GameObject("Mother_DoorPoint");
                pt.transform.position = new Vector3(81.98f, 0.2318305f, 29.5f);
                pt.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            motherDoorPoint = pt.transform;
        }
    }

    private void SetupLyThongOfficial()
    {
        if (lyThongOfficialInstance == null) lyThongOfficialInstance = GameObject.Find("LyThong_Official");

        if (lyThongOfficialInstance != null)
        {
            lyThongOfficialInstance.SetActive(true);
            Vector3 targetPos = lyThongOfficialReturnPoint != null ? lyThongOfficialReturnPoint.position : new Vector3(81.98f, 0.2318305f, 31.29f);

            if (Physics.Raycast(new Vector3(targetPos.x, targetPos.y + 2f, targetPos.z), Vector3.down, out RaycastHit hit, 5f))
            {
                targetPos.y = hit.point.y + 0.02f;
            }

            lyThongOfficialInstance.transform.position = targetPos;
            lyThongOfficialInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            lyThongOfficialInstance.transform.localScale = Vector3.one;

            MonoBehaviour playerCtrl = lyThongOfficialInstance.GetComponent("LyThongPlayerController") as MonoBehaviour;
            if (playerCtrl != null) playerCtrl.enabled = false;

            CharacterController cc = lyThongOfficialInstance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Animator anim = lyThongOfficialInstance.GetComponent<Animator>();
            if (anim == null) anim = lyThongOfficialInstance.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }
        }
    }

    private void SetupMother()
    {
        if (motherInstance == null)
        {
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
            Vector3 insidePos = motherInsidePoint != null ? motherInsidePoint.position : new Vector3(82.0f, 0.25f, 25.0f);
            motherInstance.transform.position = insidePos;
            motherInstance.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            UnityEngine.AI.NavMeshAgent agent = motherInstance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            Collider col = motherInstance.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // ĐÃ NÂNG CẤP TÌM ANIMATOR BÊN TRONG LỚP CON
            Animator anim = motherInstance.GetComponent<Animator>();
            if (anim == null) anim = motherInstance.GetComponentInChildren<Animator>();

            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }
        }
    }

    private void SetupCutsceneCamera()
    {
        if (cutsceneCameraPoint == null)
        {
            GameObject pt = GameObject.Find("Chapter2_OfficialReturnCamera");
            if (pt == null)
            {
                pt = new GameObject("Chapter2_OfficialReturnCamera");
                pt.transform.position = new Vector3(78.0f, 1.45f, 31.8f);
                pt.transform.rotation = Quaternion.Euler(6.0f, 112.0f, 0f);
            }
            cutsceneCameraPoint = pt.transform;
        }

        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null && cutsceneCameraPoint != null)
        {
            mainCamera.transform.position = cutsceneCameraPoint.position;
            mainCamera.transform.rotation = cutsceneCameraPoint.rotation;

            CameraFollow cf = mainCamera.GetComponent<CameraFollow>();
            if (cf != null) cf.enabled = false;
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
            if (speakerText != null) { speakerText.text = speaker + ":"; speakerText.color = speakerColor; }
            if (contentText != null) contentText.text = $"\"{message}\"";
            if (hintText != null) hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";
        }
    }

    private void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private IEnumerator WaitForKeyPress()
    {
        yield return new WaitForSeconds(0.15f);
        while (true)
        {
            bool pressed = false;
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) pressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;
            if (!pressed)
            {
                try { pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0); }
                catch { }
            }
            if (pressed) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator RunOfficialReturnSequence()
    {
        isCutsceneRunning = true;
        yield return new WaitForSeconds(0.6f);

        ShowDialogue("LÝ THÔNG", "Mẹ ơi! Mẹ mau ra đây! Con có tin vui muốn báo!", new Color(1.0f, 0.72f, 0.2f));
        yield return StartCoroutine(WaitForKeyPress());
        HideDialogue();

        if (doorInteraction != null) doorInteraction.OpenDoor();
        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(MoveMotherToDoorRoutine());

        AlignFacing();
        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < boastingDialogues.Length; i++)
        {
            var line = boastingDialogues[i];
            ShowDialogue(line.speaker, line.content, line.speakerColor);
            yield return StartCoroutine(WaitForKeyPress());
        }

        HideDialogue();
        isCutsceneCompleted = true;
        isCutsceneRunning = false;
    }

    private IEnumerator MoveMotherToDoorRoutine()
    {
        if (motherInstance == null) yield break;

        Vector3 startPos = motherInstance.transform.position;
        Vector3 targetPos = motherDoorPoint != null ? motherDoorPoint.position : new Vector3(81.98f, 0.2318305f, 29.5f);

        Vector3 walkDir = (targetPos - startPos);
        walkDir.y = 0f;
        if (walkDir.sqrMagnitude > 0.01f)
        {
            motherInstance.transform.rotation = Quaternion.LookRotation(walkDir.normalized);
        }

        // ĐÃ NÂNG CẤP TÌM ANIMATOR BÊN TRONG LỚP CON
        Animator anim = motherInstance.GetComponent<Animator>();
        if (anim == null) anim = motherInstance.GetComponentInChildren<Animator>();
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

    private void AlignFacing()
    {
        if (lyThongOfficialInstance != null && motherInstance != null)
        {
            Vector3 toMother = motherInstance.transform.position - lyThongOfficialInstance.transform.position;
            toMother.y = 0f;
            if (toMother.sqrMagnitude > 0.01f) lyThongOfficialInstance.transform.rotation = Quaternion.LookRotation(toMother.normalized);

            Vector3 toLyThong = lyThongOfficialInstance.transform.position - motherInstance.transform.position;
            toLyThong.y = 0f;
            if (toLyThong.sqrMagnitude > 0.01f) motherInstance.transform.rotation = Quaternion.LookRotation(toLyThong.normalized);
        }
    }

    void OnGUI()
    {
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy) return;

        if (isCutsceneCompleted)
        {
            GUIStyle compStyle = new GUIStyle(GUI.skin.box);
            compStyle.fontSize = 18;
            compStyle.alignment = TextAnchor.MiddleCenter;
            compStyle.normal.textColor = Color.yellow;
            compStyle.richText = true;

            float width = 540f, height = 65f;
            float left = (Screen.width - width) / 2f, top = Screen.height - 100f;

            GUI.Box(new Rect(left, top, width, height),
                "<b><color=#00FF00>LÝ THÔNG ĐÃ HOÀN TẤT BÁO CÔNG VỚI MẸ!</color></b>\n<size=14>Vinh hoa phú quý - Rạng rỡ tổ tông</size>",
                compStyle);
        }
    }
}