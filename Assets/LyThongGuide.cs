using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LyThongGuide : MonoBehaviour
{
    [Header("KÉO MÔ HÌNH LÝ THÔNG VÀO ĐÂY:")]
    public Animator lyThongAnimator; // <-- BIẾN MỚI QUAN TRỌNG NHẤT

    [Header("Các Object chính")]
    public Transform homePoint;
    public Transform thachSanh;
    public Transform motherNPC;

    [Header("3 Điểm đứng trong nhà")]
    public Transform motherInsidePoint;
    public Transform lyThongInsidePoint;
    public Transform thachSanhInsidePoint;

    [Header("Cấu hình Cửa & UI")]
    public DoorInteraction doorInteraction;
    public TextMeshProUGUI dialogueText;
    public Light directionalLight;

    [Header("UI Hội thoại Canvas uGUI (Chuẩn Chapter 4)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    [Header("Âm thanh chạy Cắt cảnh")]
    public AudioSource loaChayLyThong;
    public AudioSource loaChayThachSanh;

    [Header("Âm thanh Lồng tiếng Hội thoại")]
    public AudioSource loaHoiThoai;           // Loa độc lập để phát giọng nói
    public AudioClip[] motherDialogueAudio;   // Mảng 8 file tiếng cho đoạn gặp Mẹ
    public AudioClip[] secretDialogueAudio;   // Mảng 5 file tiếng cho đoạn Bí mật

    public void EnsureDialogueUI()
    {
        if (dialoguePanel == null || speakerText == null || contentText == null || hintText == null)
        {
            dialoguePanel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter2_DialogueCanvas", out speakerText, out contentText, out hintText);
        }
    }

    [Header("Cài đặt")]
    public float triggerDistance = 6.5f;
    public float guideSpeed = 2.5f;

    public enum CutsceneState { WaitingForPlayer, GuideHome, MotherDialogue, ThachSanhGoesInside, SecretDialogue, Finished }

    [Header("Trạng thái hiện tại")]
    public CutsceneState currentState = CutsceneState.WaitingForPlayer;

    private NavMeshAgent agent;
    private PlayerMovement playerMovement;
    private CharacterController thachSanhController;

    private int currentDialogueIndex = 0;
    private string currentSpeech = "";

    private readonly string[] motherDialogueLines = new string[]
    {
        "LÝ THÔNG:\n\"Mẹ ơi, con đi bán rượu về rồi đây! Mẹ xem con dẫn ai về này.\"",
        "MẸ LÝ THÔNG:\n\"Ai mà tướng tá to lớn, mặt mũi lạ hoắc thế này? Sao con lại dẫn về nhà?\"",
        "LÝ THÔNG:\n\"Đây là Thạch Sanh, tiều phu mồ côi con vừa kết nghĩa anh em dưới gốc đa. Từ nay em ấy sẽ sống cùng mẹ con ta. Thạch Sanh, mau gọi mẹ đi em!\"",
        "THẠCH SANH:\n\"Dạ... Con chào mẹ ạ. Con tứ cố vô thân, nay được anh Thông thương tình kết giao, xin mẹ cho con nương tựa.\"",
        "MẸ LÝ THÔNG:\n\"Ối dào ôi... tội nghiệp thằng bé! Đã kết nghĩa anh em thì từ nay con cứ xem ta như mẹ ruột, xem đây như nhà của mình nhé.\"",
        "LÝ THÔNG:\n\"Đúng đấy em trai, nhà ta có rau ăn rau, có cháo ăn cháo, em đừng ngại ngùng gì cả.\"",
        "THẠCH SANH:\n\"Ân tình của anh và mẹ, Thạch Sanh này xin khắc cốt ghi tâm.\"",
        "MẸ LÝ THÔNG:\n\"Đường xa chắc con cũng mệt rồi. Thạch Sanh, con cứ vào buồng trong nghỉ ngơi trước đi.\""
    };

    private readonly string[] secretDialogueLines = new string[]
    {
        "MẸ LÝ THÔNG:\n\"Trời đất ơi, mày rước cái thằng to lù lù này về làm gì cho tốn cơm tốn gạo hả con?\"",
        "LÝ THÔNG:\n\"Mẹ bé cái mồm thôi! Mẹ quên là nay mai đến phiên con phải ra miếu nộp mạng cho Chằn Tinh rồi à?\"",
        "MẸ LÝ THÔNG:\n\"Chết cha... mẹ quên đi mất! Thế ý mày rước nó về, bắt nó gọi tao bằng mẹ là để...\"",
        "LÝ THÔNG:\n\"Đúng rồi đấy! Nó thật thà ngốc nghếch, lại đang mang ơn mẹ con ta . Đêm mai con sẽ lừa nó đi canh miếu, cho nó đi thế mạng thay con!\"",
        "MẸ LÝ THÔNG:\n\"Ôi trời ơi, con trai mẹ thông minh tuyệt đỉnh! Khà khà... Mưu kế hay lắm, mẹ con ta yên tâm vào ngủ thôi!\""
    };

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Chapter4_Lythongcuopcong" || sceneName.ToLower().Contains("chapter4") ||
            (StoryStateManager.IsChanTinhDefeated && StoryStateManager.IsReturningToChapter2) ||
            StoryStateManager.LyThongReturnedAsOfficial)
        {
            enabled = false; return;
        }

        agent = GetComponent<NavMeshAgent>();

        if (thachSanh == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh");
            if (pObj != null) thachSanh = pObj.transform;
        }

        if (thachSanh != null)
        {
            playerMovement = thachSanh.GetComponent<PlayerMovement>();
            thachSanhController = thachSanh.GetComponent<CharacterController>();
        }

        if (motherNPC == null)
        {
            GameObject mObj = GameObject.Find("Meshy_AI_Elder_Master_Stance_biped_Animation_01a0d44b-3352-70aa-af88-28f245645f42_withSkin") ?? GameObject.Find("Mother") ?? GameObject.Find("MotherNPC") ?? GameObject.Find("MeLyThong") ?? GameObject.Find("Me_LyThong");
            if (mObj != null) motherNPC = mObj.transform;
        }

        if (doorInteraction == null) doorInteraction = FindAnyObjectByType<DoorInteraction>();
        EnsureDialogueUI();
        SetupSunsetLighting();
    }

    void SetupSunsetLighting()
    {
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights) { if (l.type == LightType.Directional) { directionalLight = l; break; } }
        }
        if (directionalLight != null) { directionalLight.color = new Color(1.0f, 0.75f, 0.45f); directionalLight.intensity = Mathf.Min(directionalLight.intensity, 0.65f); }
    }

    void Update()
    {
        if (lyThongAnimator != null && lyThongAnimator.runtimeAnimatorController != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            lyThongAnimator.SetFloat("Speed", currentSpeed);
        }

        switch (currentState)
        {
            case CutsceneState.WaitingForPlayer: UpdateWaiting(); break;
            case CutsceneState.GuideHome: UpdateGuideHome(); break;
            case CutsceneState.MotherDialogue: UpdateMotherDialogue(); break;
            case CutsceneState.ThachSanhGoesInside: break;
            case CutsceneState.SecretDialogue: UpdateSecretDialogue(); break;
        }
    }

    void UpdateWaiting()
    {
        if (thachSanh == null) return;

        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(thachSanh.position.x, thachSanh.position.z));

        if (dist <= triggerDistance)
        {
            currentState = CutsceneState.GuideHome;
            ClearUI();

            if (playerMovement != null) playerMovement.enabled = false;
            if (thachSanhController != null) thachSanhController.enabled = false;

            Vector3 frontPos = transform.position + transform.forward * 1.5f;
            if (Terrain.activeTerrain != null)
            {
                float groundY = Terrain.activeTerrain.SampleHeight(frontPos) + Terrain.activeTerrain.transform.position.y;
                thachSanh.position = new Vector3(frontPos.x, groundY, frontPos.z);
            }
            else { thachSanh.position = new Vector3(frontPos.x, thachSanh.position.y, frontPos.z); }

            thachSanh.LookAt(new Vector3(transform.position.x, thachSanh.position.y, transform.position.z));

            // Gọi Animator đã được gán bằng tay
            if (lyThongAnimator != null)
            {
                lyThongAnimator.SetTrigger("StandUp");
                Debug.Log("<color=green>ĐÃ GỬI LỆNH STANDUP THÀNH CÔNG TỚI: " + lyThongAnimator.gameObject.name + "</color>");
            }
            else
            {
                Debug.LogError("<color=red>LỖI: BẠN CHƯA KÉO MÔ HÌNH LÝ THÔNG VÀO Ô LY THONG ANIMATOR TRONG INSPECTOR!</color>");
            }

            if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; }
            StartCoroutine(WaitAndWalkRoutine());
        }
    }

    IEnumerator WaitAndWalkRoutine()
    {
        yield return new WaitForSeconds(4.0f);
        if (homePoint != null && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false; agent.speed = guideSpeed; agent.SetDestination(homePoint.position);
        }
    }

    void UpdateGuideHome()
    {
        bool isMoving = agent != null && agent.velocity.magnitude > 0.1f && !agent.isStopped;
        SetWalking(isMoving);

        // ==========================================
        // XỬ LÝ BẬT/TẮT ÂM THANH BƯỚC CHÂN
        // ==========================================
        if (isMoving)
        {
            if (loaChayLyThong != null && !loaChayLyThong.isPlaying) loaChayLyThong.Play();
            if (loaChayThachSanh != null && !loaChayThachSanh.isPlaying) loaChayThachSanh.Play();
        }
        else
        {
            if (loaChayLyThong != null && loaChayLyThong.isPlaying) loaChayLyThong.Stop();
            if (loaChayThachSanh != null && loaChayThachSanh.isPlaying) loaChayThachSanh.Stop();
        }
        // ==========================================

        if (thachSanh != null)
        {
            if (isMoving)
            {
                if (thachSanhController != null && !thachSanhController.enabled) thachSanhController.enabled = true;
                Vector3 followTarget = transform.position - transform.forward * 2.0f + transform.right * 1.5f;
                followTarget.y = thachSanh.position.y;
                float distToTarget = Vector2.Distance(new Vector2(thachSanh.position.x, thachSanh.position.z), new Vector2(followTarget.x, followTarget.z));

                if (distToTarget > 0.8f)
                {
                    Vector3 moveDir = (new Vector3(followTarget.x, 0, followTarget.z) - new Vector3(thachSanh.position.x, 0, thachSanh.position.z)).normalized;
                    if (thachSanhController != null) { thachSanhController.Move((moveDir * guideSpeed + Vector3.down * 15f) * Time.deltaTime); }
                    else { thachSanh.position = Vector3.MoveTowards(thachSanh.position, followTarget, guideSpeed * Time.deltaTime); }
                    if (moveDir != Vector3.zero) { Quaternion targetRot = Quaternion.LookRotation(moveDir); thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, targetRot, 8f * Time.deltaTime); }
                    SetCharacterWalking(thachSanh.gameObject, true);
                }
                else { SetCharacterWalking(thachSanh.gameObject, false); }
            }
            else
            {
                SetCharacterWalking(thachSanh.gameObject, false);
                Vector3 lookPos = new Vector3(transform.position.x, thachSanh.position.y, transform.position.z); thachSanh.LookAt(lookPos);
            }
        }

        float distToHome = homePoint != null ? Vector3.Distance(transform.position, homePoint.position) : 999f;
        if (homePoint != null && distToHome <= 2.0f)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            SetWalking(false);
            if (thachSanh != null) SetCharacterWalking(thachSanh.gameObject, false);

            // Dừng tiếng chạy khi đã về tới nhà
            if (loaChayLyThong != null) loaChayLyThong.Stop();
            if (loaChayThachSanh != null) loaChayThachSanh.Stop();

            if (doorInteraction != null) doorInteraction.OpenDoor();
            if (motherNPC != null)
            {
                transform.LookAt(new Vector3(motherNPC.position.x, transform.position.y, motherNPC.position.z));
                motherNPC.LookAt(new Vector3(transform.position.x, motherNPC.position.y, transform.position.z));
                if (thachSanh != null) thachSanh.LookAt(new Vector3(motherNPC.position.x, thachSanh.position.y, motherNPC.position.z));
            }
            currentState = CutsceneState.MotherDialogue; currentDialogueIndex = 0; currentSpeech = motherDialogueLines[0]; UpdateUI(currentSpeech);
        }
    }

    void UpdateMotherDialogue()
    {
        if (IsAdvancePressed())
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < motherDialogueLines.Length) { currentSpeech = motherDialogueLines[currentDialogueIndex]; UpdateUI(currentSpeech); }
            else { StartCoroutine(ThachSanhGoesInsideSequence()); }
        }
    }

    IEnumerator ThachSanhGoesInsideSequence()
    {
        currentState = CutsceneState.ThachSanhGoesInside; ClearUI();
        if (doorInteraction != null) doorInteraction.OpenDoor();
        if (motherNPC != null && thachSanh != null)
        {
            transform.LookAt(new Vector3(thachSanh.position.x, transform.position.y, thachSanh.position.z));
            motherNPC.LookAt(new Vector3(thachSanh.position.x, motherNPC.position.y, thachSanh.position.z));
        }

        Vector3 targetPos = thachSanhInsidePoint != null ? thachSanhInsidePoint.position : thachSanh.position;
        bool thachSanhArrived = false;
        if (thachSanhController != null) thachSanhController.enabled = false;

        while (!thachSanhArrived && thachSanh != null)
        {
            Vector3 currentPosNoY = new Vector3(thachSanh.position.x, 0, thachSanh.position.z);
            Vector3 targetPosNoY = new Vector3(targetPos.x, 0, targetPos.z);
            float distTS = Vector3.Distance(currentPosNoY, targetPosNoY);

            if (distTS > 0.3f && thachSanhInsidePoint != null)
            {
                thachSanh.position = Vector3.MoveTowards(thachSanh.position, targetPos, guideSpeed * Time.deltaTime);
                Vector3 moveDir = (targetPosNoY - currentPosNoY).normalized;
                if (moveDir != Vector3.zero) { thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime); }
                SetCharacterWalking(thachSanh.gameObject, true);
            }
            else { SetCharacterWalking(thachSanh.gameObject, false); thachSanhArrived = true; }
            yield return null;
        }

        if (thachSanhController != null) thachSanhController.enabled = true;
        if (thachSanh != null) thachSanh.gameObject.SetActive(false);

        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) { camFollow.target = transform; camFollow.distance = 3.5f; camFollow.heightOffset = 1.3f; }
        }

        if (motherNPC != null)
        {
            transform.LookAt(new Vector3(motherNPC.position.x, transform.position.y, motherNPC.position.z));
            motherNPC.LookAt(new Vector3(transform.position.x, motherNPC.position.y, transform.position.z));
        }

        yield return new WaitForSeconds(3f);
        currentState = CutsceneState.SecretDialogue; currentDialogueIndex = 0; currentSpeech = secretDialogueLines[0]; UpdateUI(currentSpeech);
    }

    void UpdateSecretDialogue()
    {
        if (IsAdvancePressed())
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < secretDialogueLines.Length) { currentSpeech = secretDialogueLines[currentDialogueIndex]; UpdateUI(currentSpeech); }
            else { EndCutscene(); }
        }
    }

    void EndCutscene()
    {
        currentState = CutsceneState.Finished; ClearUI(); StartCoroutine(TransitionToChapter3());
    }

    private IEnumerator TransitionToChapter3()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        CanvasGroup cg = null;
        if (canvas != null)
        {
            Transform fadeObjTr = canvas.transform.Find("Chapter2FadeOverlay");
            GameObject fadeObj;
            if (fadeObjTr != null) fadeObj = fadeObjTr.gameObject;
            else
            {
                fadeObj = new GameObject("Chapter2FadeOverlay"); fadeObj.transform.SetParent(canvas.transform, false);
                RectTransform rect = fadeObj.AddComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.one;
                UnityEngine.UI.Image img = fadeObj.AddComponent<UnityEngine.UI.Image>(); img.color = Color.black; img.raycastTarget = false;
            }
            cg = fadeObj.GetComponent<CanvasGroup>(); if (cg == null) cg = fadeObj.AddComponent<CanvasGroup>();
        }

        float duration = 2.0f; float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; if (cg != null) cg.alpha = Mathf.Clamp01(elapsed / duration); yield return null; }
        if (cg != null) cg.alpha = 1.0f;
        yield return new WaitForSeconds(0.3f); SceneManager.LoadScene("Chapter3_MieuChanTinh");
    }

    private bool IsAdvancePressed()
    {
        bool pressed = false;
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame ||
                                         Keyboard.current.enterKey.wasPressedThisFrame ||
                                         Keyboard.current.numpadEnterKey.wasPressedThisFrame))
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
                pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                          Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0);
            }
            catch { }
        }
        return pressed;
    }

    private void UpdateUI(string text)
    {
        currentSpeech = text;
        EnsureDialogueUI();

        // ========================================================
        // [MỚI THÊM] HỆ THỐNG TỰ ĐỘNG PHÁT ÂM THANH THEO CÂU THOẠI
        // ========================================================
        if (loaHoiThoai != null)
        {
            loaHoiThoai.Stop(); // Ngắt tiếng câu cũ nếu người chơi bấm lướt qua nhanh
            AudioClip clipDePhat = null;

            // Nếu đang ở đoạn hội thoại với mẹ -> Lấy âm thanh từ mảng motherDialogueAudio
            if (currentState == CutsceneState.MotherDialogue && motherDialogueAudio != null && currentDialogueIndex < motherDialogueAudio.Length)
            {
                clipDePhat = motherDialogueAudio[currentDialogueIndex];
            }
            // Nếu đang ở đoạn hội thoại bí mật -> Lấy âm thanh từ mảng secretDialogueAudio
            else if (currentState == CutsceneState.SecretDialogue && secretDialogueAudio != null && currentDialogueIndex < secretDialogueAudio.Length)
            {
                clipDePhat = secretDialogueAudio[currentDialogueIndex];
            }

            // Có file thì phát
            if (clipDePhat != null) loaHoiThoai.PlayOneShot(clipDePhat);
        }
        // ========================================================

        // Tách tên người nói và lời thoại
        string speaker = "";
        string content = text;
        Color speakerColor = new Color(1.0f, 0.72f, 0.2f);

        int colonIdx = text.IndexOf(':');
        if (colonIdx > 0 && colonIdx < 30)
        {
            speaker = text.Substring(0, colonIdx).Trim();
            content = text.Substring(colonIdx + 1).Trim();

            string speakerUpper = speaker.ToUpper();
            if (speakerUpper.Contains("LÝ THÔNG") || speakerUpper.Contains("LY THONG"))
            {
                speakerColor = new Color(1.0f, 0.72f, 0.2f);
            }
            else if (speakerUpper.Contains("MẸ") || speakerUpper.Contains("ME"))
            {
                speakerColor = new Color(1.0f, 0.5f, 0.5f);
            }
            else if (speakerUpper.Contains("THẠCH SANH") || speakerUpper.Contains("THACH SANH"))
            {
                speakerColor = new Color(0.3f, 0.8f, 1.0f);
            }
        }

        string cleanContent = content.Trim();
        if (cleanContent.StartsWith("\"") && cleanContent.EndsWith("\"") && cleanContent.Length >= 2)
        {
            cleanContent = cleanContent.Substring(1, cleanContent.Length - 2).Trim();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null)
            {
                speakerText.text = (!string.IsNullOrEmpty(speaker) ? speaker : "LÝ THÔNG") + ":";
                speakerText.color = speakerColor;
            }
            if (contentText != null)
            {
                contentText.text = $"\"{cleanContent}\"";
            }
            if (hintText != null)
            {
                hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";
            }
        }
        else if (dialogueText != null)
        {
            string fullText = text + "\n\n<size=80%><color=#FFD700>(Bấm Enter hoặc Chuột trái để tiếp tục)</color></size>";
            dialogueText.text = fullText;
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void ClearUI()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        currentSpeech = "";
    }

    private void SetWalking(bool walking) { SetCharacterWalking(lyThongAnimator != null ? lyThongAnimator.gameObject : gameObject, walking); }

    private void SetCharacterWalking(GameObject targetObj, bool walking)
    {
        if (targetObj == null) return;
        Animator anim = targetObj.GetComponent<Animator>();
        if (anim == null) anim = targetObj.GetComponentInChildren<Animator>();
        if (anim == null) return;
        anim.applyRootMotion = false;
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool) { if (parameter.name.ToLower().Contains("walk")) anim.SetBool(parameter.name, walking); }
            else if (parameter.type == AnimatorControllerParameterType.Float && parameter.name.ToLower() == "speed") { anim.SetFloat(parameter.name, walking ? 1f : 0f); }
        }
    }

    private Texture2D boxBgTexture;

    private Texture2D GetBoxBackground()
    {
        if (boxBgTexture == null)
        {
            boxBgTexture = new Texture2D(1, 1);
            boxBgTexture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.72f));
            boxBgTexture.Apply();
        }
        return boxBgTexture;
    }

    void OnGUI()
    {
        // Nếu đã có Canvas DialoguePanel đang hiển thị thì không vẽ OnGUI để tránh trùng lặp
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            return;
        }

        if (currentState == CutsceneState.MotherDialogue || currentState == CutsceneState.SecretDialogue)
        {
            if (dialogueText == null && !string.IsNullOrEmpty(currentSpeech))
            {
                GUI.depth = -100;
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = GetBoxBackground();
                boxStyle.wordWrap = true;
                boxStyle.alignment = TextAnchor.UpperLeft;
                boxStyle.padding = new RectOffset(35, 35, 18, 18);
                boxStyle.normal.textColor = Color.white;
                boxStyle.richText = true;

                // Kích thước chuẩn thống nhất: bottom-center, ~75-80% màn hình, cao 215px, cách đáy 30px
                float width = Mathf.Min(Screen.width * 0.78f, 1100f);
                float height = 215f;
                float left = (Screen.width - width) / 2f;
                float top = Screen.height - height - 30f;

                // Tách tên người nói và lời thoại
                string speaker = "";
                string content = currentSpeech;
                string speakerColor = "#FFD700";

                int colonIdx = currentSpeech.IndexOf(':');
                if (colonIdx > 0 && colonIdx < 30)
                {
                    speaker = currentSpeech.Substring(0, colonIdx).Trim();
                    content = currentSpeech.Substring(colonIdx + 1).Trim();

                    string speakerUpper = speaker.ToUpper();
                    if (speakerUpper.Contains("LÝ THÔNG") || speakerUpper.Contains("LY THONG"))
                    {
                        speakerColor = "#FFB833";
                    }
                    else if (speakerUpper.Contains("MẸ") || speakerUpper.Contains("ME"))
                    {
                        speakerColor = "#FF8080";
                    }
                    else if (speakerUpper.Contains("THẠCH SANH") || speakerUpper.Contains("THACH SANH"))
                    {
                        speakerColor = "#4DC3FF";
                    }
                }

                string formattedMsg;
                if (!string.IsNullOrEmpty(speaker))
                {
                    formattedMsg = $"<size=34><b><color={speakerColor}>{speaker}:</color></b></size>\n" +
                                   $"<size=28>{content}</size>\n\n" +
                                   $"<color=#FFD700><size=20><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";
                }
                else
                {
                    formattedMsg = $"<size=28>{currentSpeech}</size>\n\n" +
                                   $"<color=#FFD700><size=20><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";
                }

                GUI.Box(new Rect(left, top, width, height), formattedMsg, boxStyle);
            }
        }
    }
}