using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LyThongGuide : MonoBehaviour
{
    [Header("Các Object chính")]
    public Transform homePoint;
    public Transform thachSanh;
    public Transform motherNPC;

    [Header("3 Điểm đứng trong nhà (Deepest -> Outer)")]
    public Transform motherInsidePoint;
    public Transform lyThongInsidePoint;
    public Transform thachSanhInsidePoint;

    [Header("Cấu hình Cửa & UI")]
    public DoorInteraction doorInteraction;
    public TextMeshProUGUI dialogueText;
    public Light directionalLight;

    [Header("Cài đặt")]
    public float triggerDistance = 4.0f;
    public float guideSpeed = 2.5f;

    public enum CutsceneState
    {
        WaitingForPlayer,
        GuideHome,
        MotherDialogue,
        ThachSanhGoesInside,  // Thạch Sanh tự đi vào nhà
        SecretDialogue,       // Đoạn thoại âm mưu của 2 mẹ con
        Finished
    }

    [Header("Trạng thái hiện tại")]
    public CutsceneState currentState = CutsceneState.WaitingForPlayer;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerMovement playerMovement;
    private CharacterController thachSanhController;

    private int currentDialogueIndex = 0;
    private string currentSpeech = "";

    // 8 câu thoại với Mẹ Lý Thông ngoài nhà (Giả vờ nhận người một nhà)
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

    // Hội thoại âm mưu của mẹ con Lý Thông (Lật mặt nhanh như chớp)
    private readonly string[] secretDialogueLines = new string[]
    {
        "MẸ LÝ THÔNG:\n\"Trời đất ơi, mày rước cái thằng to lù lù này về làm gì cho tốn cơm tốn gạo hả con?\"",
        "LÝ THÔNG:\n\"Mẹ bé cái mồm thôi! Mẹ quên là nay mai đến phiên con phải ra miếu nộp mạng cho Chằn Tinh rồi à?\"",
        "MẸ LÝ THÔNG:\n\"Chết cha... mẹ quên béng mất! Thế ý mày rước nó về, bắt nó gọi tao bằng mẹ là để...\"",
        "LÝ THÔNG:\n\"Đúng rồi đấy! Nó thật thà ngốc nghếch, lại đang mang ơn anh em ta. Đêm mai con sẽ lừa nó đi canh miếu, cho nó đi thế mạng thay con!\"",
        "MẸ LÝ THÔNG:\n\"Ôi trời ơi, con trai mẹ thông minh tuyệt đỉnh! Khà khà... Mưu kế hay lắm, mẹ con ta yên tâm vào ngủ thôi!\""
    };

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        if (thachSanh == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) thachSanh = pObj.transform;
        }

        if (thachSanh != null)
        {
            playerMovement = thachSanh.GetComponent<PlayerMovement>();
            thachSanhController = thachSanh.GetComponent<CharacterController>();
        }

        if (motherNPC == null)
        {
            GameObject mObj = GameObject.Find("Mother") ?? GameObject.Find("MotherNPC") ?? GameObject.Find("MeLyThong") ?? GameObject.Find("Me_LyThong");
            if (mObj != null) motherNPC = mObj.transform;
        }

        if (doorInteraction == null) doorInteraction = FindObjectOfType<DoorInteraction>();
        if (dialogueText == null)
        {
            QuestManager qm = FindObjectOfType<QuestManager>();
            if (qm != null && qm.questText != null) dialogueText = qm.questText;
            else dialogueText = FindObjectOfType<TextMeshProUGUI>();
        }

        SetupSunsetLighting();
    }

    void SetupSunsetLighting()
    {
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional) { directionalLight = l; break; }
            }
        }
        if (directionalLight != null)
        {
            directionalLight.color = new Color(1.0f, 0.75f, 0.45f);
            directionalLight.intensity = Mathf.Min(directionalLight.intensity, 0.65f);
        }
    }

    void Update()
    {
        if (animator != null && animator.runtimeAnimatorController != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }

        switch (currentState)
        {
            case CutsceneState.WaitingForPlayer:
                UpdateWaiting();
                break;
            case CutsceneState.GuideHome:
                UpdateGuideHome();
                break;
            case CutsceneState.MotherDialogue:
                UpdateMotherDialogue();
                break;
            case CutsceneState.ThachSanhGoesInside:
                // Xử lý bằng Coroutine
                break;
            case CutsceneState.SecretDialogue:
                UpdateSecretDialogue();
                break;
        }
    }

    void UpdateWaiting()
    {
        if (thachSanh == null) return;
        float dist = Vector3.Distance(transform.position, thachSanh.position);
        if (dist <= triggerDistance)
        {
            currentState = CutsceneState.GuideHome;
            ClearUI();
            if (playerMovement != null) playerMovement.enabled = false;
            if (homePoint != null && agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = guideSpeed;
                agent.SetDestination(homePoint.position);
            }
        }
    }

    void UpdateGuideHome()
    {
        bool isMoving = agent != null && agent.velocity.magnitude > 0.1f;
        SetWalking(isMoving);

        if (thachSanh != null)
        {
            Vector3 followTarget = transform.position - transform.forward * 2.2f + transform.right * 1.2f;
            followTarget.y = thachSanh.position.y;
            float distToTarget = Vector3.Distance(thachSanh.position, followTarget);

            if (distToTarget > 0.4f)
            {
                Vector3 moveDir = (followTarget - thachSanh.position).normalized;
                if (thachSanhController != null)
                    thachSanhController.Move((moveDir * guideSpeed + Vector3.down * 9.81f) * Time.deltaTime);
                else
                    thachSanh.position = Vector3.MoveTowards(thachSanh.position, followTarget, guideSpeed * Time.deltaTime);

                if (moveDir != Vector3.zero)
                    thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);

                SetCharacterWalking(thachSanh.gameObject, true);
            }
            else
            {
                SetCharacterWalking(thachSanh.gameObject, false);
            }
        }

        float distToHome = homePoint != null ? Vector3.Distance(transform.position, homePoint.position) : 999f;
        if (distToHome <= 1.8f || (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f))
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            SetWalking(false);
            if (thachSanh != null) SetCharacterWalking(thachSanh.gameObject, false);

            if (doorInteraction != null) doorInteraction.OpenDoor();

            if (motherNPC != null)
            {
                transform.LookAt(new Vector3(motherNPC.position.x, transform.position.y, motherNPC.position.z));
                motherNPC.LookAt(new Vector3(transform.position.x, motherNPC.position.y, transform.position.z));
                if (thachSanh != null)
                    thachSanh.LookAt(new Vector3(motherNPC.position.x, thachSanh.position.y, motherNPC.position.z));
            }

            currentState = CutsceneState.MotherDialogue;
            currentDialogueIndex = 0;
            currentSpeech = motherDialogueLines[0];
            UpdateUI(currentSpeech);
        }
    }

    void UpdateMotherDialogue()
    {
        if (IsAdvancePressed())
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < motherDialogueLines.Length)
            {
                currentSpeech = motherDialogueLines[currentDialogueIndex];
                UpdateUI(currentSpeech);
            }
            else
            {
                // Thay vì mẹ dẫn vào, giờ chạy Coroutine Thạch Sanh đi vào nhà
                StartCoroutine(ThachSanhGoesInsideSequence());
            }
        }
    }

    // COROUTINE MỚI: Thạch Sanh lủi thủi đi vào nhà rồi biến mất
    // COROUTINE MỚI: Thạch Sanh lủi thủi đi vào nhà rồi biến mất
    // COROUTINE MỚI: Thạch Sanh lủi thủi đi vào nhà rồi biến mất
    IEnumerator ThachSanhGoesInsideSequence()
    {
        currentState = CutsceneState.ThachSanhGoesInside;
        ClearUI();

        if (doorInteraction != null) doorInteraction.OpenDoor();

        // Mẹ và Lý Thông nhìn theo bóng lưng Thạch Sanh
        if (motherNPC != null && thachSanh != null)
        {
            transform.LookAt(new Vector3(thachSanh.position.x, transform.position.y, thachSanh.position.z));
            motherNPC.LookAt(new Vector3(thachSanh.position.x, motherNPC.position.y, thachSanh.position.z));
        }

        // Lấy tọa độ điểm đích bạn vừa kéo vào
        Vector3 targetPos = thachSanhInsidePoint != null ? thachSanhInsidePoint.position : thachSanh.position;
        bool thachSanhArrived = false;

        if (thachSanhController != null) thachSanhController.enabled = false;

        while (!thachSanhArrived && thachSanh != null)
        {
            // BẢO HIỂM: Bỏ qua trục Y (độ cao) để Thạch Sanh không bị kẹt nếu sàn nhà cao
            Vector3 currentPosNoY = new Vector3(thachSanh.position.x, 0, thachSanh.position.z);
            Vector3 targetPosNoY = new Vector3(targetPos.x, 0, targetPos.z);

            float distTS = Vector3.Distance(currentPosNoY, targetPosNoY);

            // Chỉ chạy khi khoảng cách còn xa và BẮT BUỘC phải có điểm đến
            if (distTS > 0.3f && thachSanhInsidePoint != null)
            {
                thachSanh.position = Vector3.MoveTowards(thachSanh.position, targetPos, guideSpeed * Time.deltaTime);

                Vector3 moveDir = (targetPosNoY - currentPosNoY).normalized;
                if (moveDir != Vector3.zero)
                {
                    thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
                }
                SetCharacterWalking(thachSanh.gameObject, true);
            }
            else
            {
                SetCharacterWalking(thachSanh.gameObject, false);
                thachSanhArrived = true; // Đã tới đích, thoát vòng lặp
            }
            yield return null;
        }

        // Tới đích -> Bật lại vật lý và Biến mất
        if (thachSanhController != null) thachSanhController.enabled = true;
        if (thachSanh != null) thachSanh.gameObject.SetActive(false);

        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.target = transform; // Đổi mục tiêu theo dõi sang Lý Thông
                camFollow.distance = 3.5f;    // Kéo máy quay gần lại một chút để đặc tả khuôn mặt
                camFollow.heightOffset = 1.3f;
            }
        }
        // ===================================================

        // Hai mẹ con Lý Thông quay mặt lại nhìn nhau
        if (motherNPC != null)
            // Hai mẹ con Lý Thông quay mặt lại nhìn nhau
            if (motherNPC != null)
        {
            transform.LookAt(new Vector3(motherNPC.position.x, transform.position.y, motherNPC.position.z));
            motherNPC.LookAt(new Vector3(transform.position.x, motherNPC.position.y, transform.position.z));
        }

        // Chờ tĩnh lặng 3 giây tạo kịch tính
        yield return new WaitForSeconds(3f);

        // Hiện đoạn thoại âm mưu
        currentState = CutsceneState.SecretDialogue;
        currentDialogueIndex = 0;
        currentSpeech = secretDialogueLines[0];
        UpdateUI(currentSpeech);
    }

    // STATE MỚI: Hội thoại âm mưu
    void UpdateSecretDialogue()
    {
        if (IsAdvancePressed())
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < secretDialogueLines.Length)
            {
                currentSpeech = secretDialogueLines[currentDialogueIndex];
                UpdateUI(currentSpeech);
            }
            else
            {
                // Đọc xong âm mưu thì Fade đen chuyển chương
                EndCutscene();
            }
        }
    }

    void EndCutscene()
    {
        currentState = CutsceneState.Finished;
        ClearUI();
        StartCoroutine(TransitionToChapter3());
    }

    private IEnumerator TransitionToChapter3()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        CanvasGroup cg = null;
        if (canvas != null)
        {
            Transform fadeObjTr = canvas.transform.Find("Chapter2FadeOverlay");
            GameObject fadeObj;
            if (fadeObjTr != null) fadeObj = fadeObjTr.gameObject;
            else
            {
                fadeObj = new GameObject("Chapter2FadeOverlay");
                fadeObj.transform.SetParent(canvas.transform, false);
                RectTransform rect = fadeObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.one;
                UnityEngine.UI.Image img = fadeObj.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.black;
                img.raycastTarget = false;
            }
            cg = fadeObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = fadeObj.AddComponent<CanvasGroup>();
        }

        float duration = 2.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (cg != null) cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        if (cg != null) cg.alpha = 1.0f;

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Chapter3_MieuChanTinh");
    }

    private bool IsAdvancePressed()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void UpdateUI(string text)
    {
        string fullText = text + "\n\n<size=80%><color=#FFD700>(Bấm Space hoặc Click chuột để tiếp tục)</color></size>";
        if (dialogueText != null)
        {
            dialogueText.text = fullText;
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void ClearUI()
    {
        if (dialogueText != null) dialogueText.text = "";
        currentSpeech = "";
    }

    private void SetWalking(bool walking) { SetCharacterWalking(gameObject, walking); }

    private void SetCharacterWalking(GameObject targetObj, bool walking)
    {
        if (targetObj == null) return;
        Animator anim = targetObj.GetComponent<Animator>();
        if (anim == null) anim = targetObj.GetComponentInChildren<Animator>();
        if (anim == null) return;

        anim.applyRootMotion = false;
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                if (parameter.name.ToLower().Contains("walk")) anim.SetBool(parameter.name, walking);
            }
            else if (parameter.type == AnimatorControllerParameterType.Float && parameter.name.ToLower() == "speed")
            {
                anim.SetFloat(parameter.name, walking ? 1f : 0f);
            }
        }
    }

    void OnGUI()
    {
        if (currentState == CutsceneState.MotherDialogue || currentState == CutsceneState.SecretDialogue)
        {
            if (dialogueText == null && !string.IsNullOrEmpty(currentSpeech))
            {
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.fontSize = 18;
                boxStyle.wordWrap = true;
                boxStyle.alignment = TextAnchor.MiddleCenter;
                boxStyle.normal.textColor = Color.white;

                float width = Screen.width * 0.7f, height = 130f;
                GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height - height - 40f, width, height),
                    currentSpeech + "\n\n(Bấm Space hoặc Click chuột để tiếp tục)", boxStyle);
            }
        }
    }
}