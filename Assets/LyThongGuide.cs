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
    public Transform homePoint; // LyThongHomePoint (Điểm trước cửa nhà)
    public Transform thachSanh;  // Reference tới Thạch Sanh
    public Transform motherNPC;  // Reference tới Mẹ Lý Thông

    [Header("3 Điểm đứng trong nhà (Deepest -> Outer)")]
    public Transform motherInsidePoint;     // Sâu nhất trong nhà
    public Transform lyThongInsidePoint;    // Phía sau / gần Mẹ
    public Transform thachSanhInsidePoint;  // Phía sau Lý Thông

    [Header("Cấu hình Cửa & UI")]
    public DoorInteraction doorInteraction;
    public TextMeshProUGUI dialogueText;
    public Light directionalLight;

    [Header("Cài đặt")]
    public float triggerDistance = 4.0f;
    public float guideSpeed = 2.5f;

    public enum CutsceneState
    {
        WaitingForPlayer,     // State 1: Chờ Thạch Sanh tới gần (<=4m)
        FirstDialogue,        // State 2: 5 câu thoại đầu giữa Lý Thông & Thạch Sanh
        GuideHome,            // State 3: Lý Thông dẫn Thạch Sanh về trước cửa nhà
        MotherDialogue,       // State 4: 8 câu thoại ngoài nhà với Mẹ Lý Thông
        MotherLeadsInside,    // State 5: Mẹ -> Lý Thông -> Thạch Sanh nối đuôi nhau vào nhà (Coroutine)
        FinalInsideDialogue,  // State 6: Câu thoại cuối của Mẹ khi cả 3 đã đứng trong nhà
        Finished              // State 7: Hoàn thành Cutscene, trả quyền điều khiển
    }

    [Header("Trạng thái hiện tại")]
    public CutsceneState currentState = CutsceneState.WaitingForPlayer;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerMovement playerMovement;
    private Animator thachSanhAnim;
    private CharacterController thachSanhController;

    private int currentDialogueIndex = 0;
    private string currentSpeech = "";

    // 5 câu thoại đầu giữa Lý Thông & Thạch Sanh
    private readonly string[] firstDialogueLines = new string[]
    {
        "LÝ THÔNG:\n\"Chú em đi đâu một mình giữa lúc trời sắp tối thế này?\"",
        "THẠCH SANH:\n\"Tôi vốn sống một mình, nay đi đây đó, cũng chưa biết nghỉ chân nơi nào.\"",
        "LÝ THÔNG:\n\"Trời sắp tối rồi. Nếu chú không chê, hãy theo ta về nhà nghỉ tạm.\"",
        "THẠCH SANH:\n\"Nếu được vậy thì thật quý hóa. Tôi xin đa tạ huynh.\"",
        "LÝ THÔNG:\n\"Nhà ta ở ngay phía trước. Chú cứ theo ta.\""
    };

    // 8 câu thoại với Mẹ Lý Thông ngoài nhà
    private readonly string[] motherDialogueLines = new string[]
    {
        "LÝ THÔNG:\n\"Mẹ, con về rồi!\"",
        "MẸ LÝ THÔNG:\n\"Con về đấy à? Người đi cùng con là ai vậy?\"",
        "LÝ THÔNG:\n\"Trên đường về, con gặp chú ấy đi một mình giữa lúc trời sắp tối nên con mời về nhà nghỉ tạm.\"",
        "THẠCH SANH:\n\"Cháu chào bác. Cháu tên là Thạch Sanh. Cảm ơn bác đã cho cháu tá túc.\"",
        "MẸ LÝ THÔNG:\n\"Ừ, đã đến nhà thì cứ tự nhiên. Cháu vào nghỉ đi.\"",
        "LÝ THÔNG:\n\"Nếu chú không chê thì từ nay cứ ở lại đây với mẹ con ta.\"",
        "THẠCH SANH:\n\"Tôi xin đa tạ huynh và bác.\"",
        "MẸ LÝ THÔNG:\n\"Thôi, trời cũng tối rồi. Hai đứa theo ta vào nhà.\""
    };

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        // Tự động tìm Thạch Sanh nếu chưa gán
        if (thachSanh == null)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) thachSanh = pObj.transform;
        }

        if (thachSanh != null)
        {
            playerMovement = thachSanh.GetComponent<PlayerMovement>();
            thachSanhAnim = thachSanh.GetComponent<Animator>();
            thachSanhController = thachSanh.GetComponent<CharacterController>();
        }

        // Tự động tìm Mẹ Lý Thông nếu chưa kéo vào Inspector
        if (motherNPC == null)
        {
            GameObject mObj = GameObject.Find("Mother") ?? GameObject.Find("MotherNPC") ?? GameObject.Find("MeLyThong") ?? GameObject.Find("Me_LyThong");
            if (mObj != null) motherNPC = mObj.transform;
        }

        // Đảm bảo Mẹ Lý Thông có NavMeshAgent gắn ở Root Object
        EnsureNavMeshAgentOnMother();

        // Tự động tìm Cửa
        if (doorInteraction == null)
        {
            doorInteraction = FindObjectOfType<DoorInteraction>();
        }

        // Tự động tìm Text UI
        if (dialogueText == null)
        {
            QuestManager qm = FindObjectOfType<QuestManager>();
            if (qm != null && qm.questText != null)
            {
                dialogueText = qm.questText;
            }
            else
            {
                dialogueText = FindObjectOfType<TextMeshProUGUI>();
            }
        }

        // Chỉnh ánh sáng chiều muộn/hoàng hôn
        SetupSunsetLighting();
    }

    private void EnsureNavMeshAgentOnMother()
    {
        if (motherNPC == null) return;

        // NavMeshAgent BẮT BUỘC nằm ở Root Object của Mẹ
        NavMeshAgent mAgent = motherNPC.GetComponent<NavMeshAgent>();
        if (mAgent == null)
        {
            mAgent = motherNPC.gameObject.AddComponent<NavMeshAgent>();
            mAgent.speed = guideSpeed * 0.9f;
            mAgent.stoppingDistance = 0.3f;
            mAgent.radius = 0.4f;
            mAgent.height = 1.8f;
        }

        // Snap Mẹ vào mặt sàn NavMesh nếu chưa ở trên NavMesh
        if (!mAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(motherNPC.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                mAgent.Warp(hit.position);
            }
        }
    }

    void SetupSunsetLighting()
    {
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
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
        // Cập nhật animator cho Lý Thông theo tốc độ thực tế của NavMeshAgent
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
                animator.applyRootMotion = false;
        }

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

            case CutsceneState.FirstDialogue:
                UpdateFirstDialogue();
                break;

            case CutsceneState.GuideHome:
                UpdateGuideHome();
                break;

            case CutsceneState.MotherDialogue:
                UpdateMotherDialogue();
                break;

            case CutsceneState.MotherLeadsInside:
                // Được quản lý hoàn toàn bằng Coroutine EnterHouseSequence()
                break;

            case CutsceneState.FinalInsideDialogue:
                UpdateFinalInsideDialogue();
                break;

            case CutsceneState.Finished:
                break;
        }
    }

    // STATE 1: WAITING
    void UpdateWaiting()
    {
        if (thachSanh == null) return;

        float dist = Vector3.Distance(transform.position, thachSanh.position);
        if (dist <= triggerDistance)
        {
            currentState = CutsceneState.FirstDialogue;
            currentDialogueIndex = 0;
            currentSpeech = firstDialogueLines[0];
            UpdateUI(currentSpeech);

            if (playerMovement != null) playerMovement.enabled = false;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            SetWalking(false);
            RotateTowards(thachSanh.position);
        }
    }

    // STATE 2: FIRST DIALOGUE
    void UpdateFirstDialogue()
    {
        RotateTowards(thachSanh.position);

        if (IsAdvancePressed())
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < firstDialogueLines.Length)
            {
                currentSpeech = firstDialogueLines[currentDialogueIndex];
                UpdateUI(currentSpeech);
            }
            else
            {
                ClearUI();
                currentState = CutsceneState.GuideHome;

                if (homePoint != null && agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = guideSpeed;
                    agent.SetDestination(homePoint.position);
                }
            }
        }
    }

    // STATE 3: GUIDE HOME (Về trước cửa nhà)
    void UpdateGuideHome()
    {
        bool isMoving = agent != null && agent.velocity.magnitude > 0.1f;
        SetWalking(isMoving);

        // Thạch Sanh đi theo phía sau Lý Thông 2.2m
        if (thachSanh != null)
        {
            Vector3 followTarget = transform.position - transform.forward * 2.2f;
            followTarget.y = thachSanh.position.y;

            float distToTarget = Vector3.Distance(thachSanh.position, followTarget);
            if (distToTarget > 0.4f)
            {
                Vector3 moveDir = (followTarget - thachSanh.position).normalized;
                if (thachSanhController != null)
                {
                    thachSanhController.Move((moveDir * guideSpeed + Vector3.down * 9.81f) * Time.deltaTime);
                }
                else
                {
                    thachSanh.position = Vector3.MoveTowards(thachSanh.position, followTarget, guideSpeed * Time.deltaTime);
                }

                if (moveDir != Vector3.zero)
                {
                    thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
                }
                SetCharacterWalking(thachSanh.gameObject, true);
            }
            else
            {
                SetCharacterWalking(thachSanh.gameObject, false);
            }
        }

        // Khi Lý Thông tới gần homePoint (trước cửa)
        float distToHome = homePoint != null ? Vector3.Distance(transform.position, homePoint.position) : 999f;
        if (distToHome <= 1.8f || (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f))
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            SetWalking(false);
            if (thachSanh != null) SetCharacterWalking(thachSanh.gameObject, false);

            // Mở cửa và bắt đầu thoại 8 câu ngoài nhà với Mẹ
            if (doorInteraction != null) doorInteraction.OpenDoor();

            currentState = CutsceneState.MotherDialogue;
            currentDialogueIndex = 0;
            currentSpeech = motherDialogueLines[0];
            UpdateUI(currentSpeech);
        }
    }

    // STATE 4: MOTHER DIALOGUE (Hội thoại 8 câu ngoài nhà)
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
                // KHÔNG GỌI EndCutscene() Ở ĐÂY!
                // Bắt đầu Coroutine dẫn cả nhóm vào nhà
                StartCoroutine(EnterHouseSequence());
            }
        }
    }

    // STATE 5: ENTER HOUSE SEQUENCE (Mẹ -> Lý Thông -> Thạch Sanh)
    IEnumerator EnterHouseSequence()
    {
        currentState = CutsceneState.MotherLeadsInside;
        ClearUI();

        // 1. MỞ CỬA TRƯỚC
        if (doorInteraction != null)
        {
            doorInteraction.OpenDoor();
        }

        // 2. LẤY NAV MESH AGENT TRÊN ROOT CỦA MẸ LÝ THÔNG
        EnsureNavMeshAgentOnMother();

        NavMeshAgent motherAgent = null;
        if (motherNPC != null)
        {
            motherAgent = motherNPC.GetComponent<NavMeshAgent>();
            if (motherAgent != null && !motherAgent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(motherNPC.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    motherAgent.Warp(hit.position);
                }
            }

            Debug.Log("Mother start moving inside");
            Debug.Log("Mother isOnNavMesh: " + (motherAgent != null ? motherAgent.isOnNavMesh.ToString() : "false"));
        }
        else
        {
            Debug.LogWarning("Chưa gán MotherNPC trong Inspector của LyThongGuide!");
        }

        // 3. KIỂM TRA ĐIỂM ĐÍCH MOTHER INSIDE POINT
        Vector3 motherTargetPos = transform.position;
        if (motherInsidePoint != null)
        {
            motherTargetPos = motherInsidePoint.position;
            Debug.Log("Mother destination: " + motherTargetPos);

            NavMeshHit pointHit;
            if (!NavMesh.SamplePosition(motherTargetPos, out pointHit, 3.0f, NavMesh.AllAreas))
            {
                Debug.LogError("MotherInsidePoint đang NẰM NGOÀI NavMesh! Vui lòng kéo MotherInsidePoint vào vùng NavMesh màu xanh trong Unity Editor.");
            }
        }
        else
        {
            Debug.LogWarning("Chưa kéo MotherInsidePoint vào Inspector! Tự động tính điểm phía sau cửa.");
            motherTargetPos = homePoint != null ? homePoint.position + homePoint.forward * 4.0f : transform.position;
        }

        // 4. MẸ BẮT ĐẦU ĐI TỚI MOTHER INSIDE POINT
        bool motherArrived = false;
        if (motherNPC != null && motherAgent != null && motherAgent.isOnNavMesh)
        {
            motherAgent.isStopped = false;
            motherAgent.speed = guideSpeed * 0.9f;
            motherAgent.SetDestination(motherTargetPos);
            SetCharacterWalking(motherNPC.gameObject, true);
        }
        else
        {
            if (motherAgent != null && !motherAgent.isOnNavMesh)
            {
                Debug.LogError("Mother is NOT on NavMesh! Cannot use SetDestination.");
            }
            if (motherNPC == null) motherArrived = true; // Bỏ qua nếu không có Mẹ
        }

        // 5. CHỜ 0.7 GIÂY -> LÝ THÔNG BẮT ĐẦU ĐI
        yield return new WaitForSeconds(0.7f);

        Vector3 lyThongTargetPos = lyThongInsidePoint != null ? lyThongInsidePoint.position : (homePoint != null ? homePoint.position + homePoint.forward * 2.5f : transform.position);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = guideSpeed;
            agent.SetDestination(lyThongTargetPos);
            SetWalking(true);
        }
        bool lyThongArrived = false;

        // 6. CHỜ THÊM 0.7 GIÂY -> THẠCH SANH BẮT ĐẦU ĐI
        yield return new WaitForSeconds(0.7f);

        Vector3 thachSanhTargetPos = thachSanhInsidePoint != null ? thachSanhInsidePoint.position : (homePoint != null ? homePoint.position + homePoint.forward * 1.3f : thachSanh.position);
        bool thachSanhArrived = false;

        // 7. VÒNG LẶP DI CHUYỂN & KIỂM TRA ĐIỀU KIỆN ĐẾN ĐÍCH CỦA CẢ 3
        while (!motherArrived || !lyThongArrived || !thachSanhArrived)
        {
            if (doorInteraction != null) doorInteraction.OpenDoor();

            // --- KIỂM TRA MẸ ĐÃ TỚI CHƯA ---
            if (!motherArrived && motherNPC != null && motherAgent != null)
            {
                if (motherAgent.isOnNavMesh)
                {
                    if (!motherAgent.pathPending && motherAgent.remainingDistance <= motherAgent.stoppingDistance + 0.1f)
                    {
                        motherAgent.isStopped = true;
                        SetCharacterWalking(motherNPC.gameObject, false);
                        motherArrived = true;
                        Debug.Log("Mother reached MotherInsidePoint");
                    }
                    else
                    {
                        SetCharacterWalking(motherNPC.gameObject, motherAgent.velocity.magnitude > 0.1f);
                    }
                }
                else
                {
                    // Fallback di chuyển thủ công nếu chưa ở trên NavMesh
                    motherNPC.position = Vector3.MoveTowards(motherNPC.position, motherTargetPos, guideSpeed * 0.9f * Time.deltaTime);
                    Vector3 dir = (motherTargetPos - motherNPC.position).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero) motherNPC.rotation = Quaternion.Slerp(motherNPC.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);

                    if (Vector3.Distance(motherNPC.position, motherTargetPos) <= 0.4f)
                    {
                        SetCharacterWalking(motherNPC.gameObject, false);
                        motherArrived = true;
                        Debug.Log("Mother reached MotherInsidePoint (Fallback)");
                    }
                    else
                    {
                        SetCharacterWalking(motherNPC.gameObject, true);
                    }
                }
            }

            // --- KIỂM TRA LÝ THÔNG ĐÃ TỚI CHƯA ---
            if (!lyThongArrived)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        agent.isStopped = true;
                        SetWalking(false);
                        lyThongArrived = true;
                        Debug.Log("LyThong reached LyThongInsidePoint");
                    }
                    else
                    {
                        SetWalking(agent.velocity.magnitude > 0.1f);
                    }
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, lyThongTargetPos, guideSpeed * Time.deltaTime);
                    Vector3 dir = (lyThongTargetPos - transform.position).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);

                    if (Vector3.Distance(transform.position, lyThongTargetPos) <= 0.4f)
                    {
                        SetWalking(false);
                        lyThongArrived = true;
                        Debug.Log("LyThong reached LyThongInsidePoint (Fallback)");
                    }
                    else
                    {
                        SetWalking(true);
                    }
                }
            }

            // --- KIỂM TRA THẠCH SANH ĐÃ TỚI CHƯA ---
            if (!thachSanhArrived && thachSanh != null)
            {
                float distTS = Vector3.Distance(thachSanh.position, thachSanhTargetPos);
                if (distTS > 0.4f)
                {
                    Vector3 moveDir = (thachSanhTargetPos - thachSanh.position).normalized;
                    if (thachSanhController != null)
                    {
                        thachSanhController.Move((moveDir * guideSpeed + Vector3.down * 9.81f) * Time.deltaTime);
                    }
                    else
                    {
                        thachSanh.position = Vector3.MoveTowards(thachSanh.position, thachSanhTargetPos, guideSpeed * Time.deltaTime);
                    }

                    if (moveDir != Vector3.zero)
                    {
                        moveDir.y = 0;
                        thachSanh.rotation = Quaternion.Slerp(thachSanh.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
                    }
                    SetCharacterWalking(thachSanh.gameObject, true);
                }
                else
                {
                    SetCharacterWalking(thachSanh.gameObject, false);
                    thachSanhArrived = true;
                    Debug.Log("ThachSanh reached ThachSanhInsidePoint");
                }
            }
            else
            {
                thachSanhArrived = true;
            }

            yield return null;
        }

        // 8. CHỈ KHI CẢ 3 ĐÃ VÀO NHÀ MỚI CHUYỂN SANG CÂU THOẠI CUỐI IN-HOUSE
        currentState = CutsceneState.FinalInsideDialogue;
        currentSpeech = "MẸ LÝ THÔNG:\n\"Đêm nay các con cứ nghỉ ở đây, có gì mai hãy tính.\"";
        UpdateUI(currentSpeech);
    }

    // STATE 6: FINAL INSIDE DIALOGUE
    void UpdateFinalInsideDialogue()
    {
        if (IsAdvancePressed())
        {
            // CHỈ KHI ĐÃ HẾT CÂU THOẠI TRONG NHÀ MỚI KẾT THÚC CUTSCENE!
            EndCutscene();
        }
    }

    // STATE 7: END CUTSCENE
    void EndCutscene()
    {
        currentState = CutsceneState.Finished;
        ClearUI();

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Debug.Log("Hoàn thành Cutscene Chapter 2! Chuyển sang Chapter 3...");
        SceneManager.LoadScene("Chapter3_MieuChanTinh");
    }

    private bool IsAdvancePressed()
    {
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return spacePressed || mousePressed;
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
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        currentSpeech = "";
    }

    private void RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        }
    }

    private void SetWalking(bool walking)
    {
        SetCharacterWalking(gameObject, walking);
    }

    private void SetCharacterWalking(GameObject targetObj, bool walking)
    {
        if (targetObj == null) return;
        Animator anim = targetObj.GetComponent<Animator>();
        if (anim == null)
            anim = targetObj.GetComponentInChildren<Animator>();
        if (anim == null) return;

        anim.applyRootMotion = false;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                if (string.Equals(parameter.name, "IsWalking", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter.name, "Walk", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter.name, "isWalking", System.StringComparison.OrdinalIgnoreCase))
                {
                    anim.SetBool(parameter.name, walking);
                }
            }
            else if (parameter.type == AnimatorControllerParameterType.Float)
            {
                if (string.Equals(parameter.name, "Speed", System.StringComparison.OrdinalIgnoreCase))
                {
                    anim.SetFloat(parameter.name, walking ? 1f : 0f);
                }
            }
        }
    }

    void OnGUI()
    {
        if (currentState == CutsceneState.FirstDialogue || 
            currentState == CutsceneState.MotherDialogue || 
            currentState == CutsceneState.FinalInsideDialogue)
        {
            if (dialogueText == null && !string.IsNullOrEmpty(currentSpeech))
            {
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.fontSize = 18;
                boxStyle.wordWrap = true;
                boxStyle.alignment = TextAnchor.MiddleCenter;
                boxStyle.normal.textColor = Color.white;

                float width = Screen.width * 0.7f;
                float height = 130f;
                float left = (Screen.width - width) / 2f;
                float top = Screen.height - height - 40f;

                GUI.Box(new Rect(left, top, width, height), currentSpeech + "\n\n(Bấm Space hoặc Click chuột để tiếp tục)", boxStyle);
            }
        }
    }
}