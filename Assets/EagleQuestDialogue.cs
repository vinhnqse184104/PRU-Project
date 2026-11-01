using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.AI; // QUAN TRỌNG: Thêm thư viện AI để dùng NavMesh

public class EagleQuestDialogue : MonoBehaviour
{
    [Header("UI Hội thoại Canvas")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    [Header("Cài đặt Nhân vật")]
    public GameObject thachSanh;
    public GameObject lyThong;
    public GameObject meLyThong;
    public GameObject mainCamera;     // Camera đi theo Thạch Sanh lúc chơi
    public GameObject dialogueCamera; // Camera tĩnh góc rộng lúc nói chuyện

    [Header("Hệ thống Di chuyển tự động")]
    public Transform caveDestination; // Kéo DiemDen_Hang vào đây
    public PlayerMovement thachSanhMovement; // Kéo Thạch Sanh vào đây để tắt phím điều khiển lúc cutscene chạy

    private bool isDialogueActive = false;
    private string currentDialogueSpeaker = "";
    private string currentDialogueMessage = "";
    private Color currentDialogueColor = Color.white;
    private Texture2D boxBgTexture;

    private Color colorThachSanh = new Color(0.3f, 0.8f, 1.0f);
    private Color colorLyThong = new Color(1.0f, 0.72f, 0.2f);
    private Color colorMeLyThong = new Color(0.8f, 0.4f, 0.8f);

    void Start()
    {
        StartCoroutine(KichBanNhoCuuCongChua());
    }

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

    IEnumerator WaitForKeyPress()
    {
        yield return new WaitForSeconds(0.2f);
        while (true)
        {
            bool pressed = false;
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) pressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;
            if (!pressed) { try { pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0); } catch { } }
            if (pressed) break;
            yield return null;
        }
    }

    [Header("Cài đặt Xoay nhân vật")]
    public float turnSpeed = 360f; // Tốc độ xoay mượt theo trục Y (độ/giây)

    private Coroutine rotateCoroutine;

    // Tạm khóa hoặc mở lại điều khiển của Thạch Sanh (di chuyển và tấn công)
    private void SetPlayerControl(bool enabled)
    {
        if (thachSanhMovement != null)
        {
            thachSanhMovement.enabled = enabled;
        }

        if (thachSanh != null)
        {
            PlayerMovement pm = thachSanh.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = enabled;

            PlayerCut cut = thachSanh.GetComponent<PlayerCut>() ?? thachSanh.GetComponentInChildren<PlayerCut>();
            if (cut != null) cut.enabled = enabled;

            PlayerKick kick = thachSanh.GetComponent<PlayerKick>() ?? thachSanh.GetComponentInChildren<PlayerKick>();
            if (kick != null) kick.enabled = enabled;

            Animator anim = thachSanh.GetComponent<Animator>() ?? thachSanh.GetComponentInChildren<Animator>();
            if (anim != null && !enabled)
            {
                anim.SetFloat("Speed", 0f);
            }

            if (enabled)
            {
                NavMeshAgent agent = thachSanh.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                }
            }
        }
    }

    // Ra lệnh cho Thạch Sanh xoay mặt mượt mà về phía đối phương
    public void RotateThachSanhTowards(GameObject target, float speed = 0f)
    {
        if (thachSanh == null || target == null) return;
        if (speed <= 0f) speed = turnSpeed;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }
        rotateCoroutine = StartCoroutine(SmoothRotateThachSanh(target.transform.position, speed));
    }

    // Coroutine xoay mượt bằng Quaternion.RotateTowards CHỈ trên trục Y, không nghiêng ngã
    private IEnumerator SmoothRotateThachSanh(Vector3 targetPos, float speed)
    {
        if (thachSanh == null) yield break;

        Vector3 startPos = thachSanh.transform.position;
        Vector3 direction = targetPos - startPos;
        direction.y = 0f; // Chỉ xoay theo trục Y, không nghiêng hoặc ngã người

        if (direction.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (thachSanh != null && Quaternion.Angle(thachSanh.transform.rotation, targetRotation) > 0.5f)
        {
            thachSanh.transform.rotation = Quaternion.RotateTowards(
                thachSanh.transform.rotation,
                targetRotation,
                speed * Time.deltaTime
            );
            // Bảo toàn tuyệt đối vị trí đứng của Thạch Sanh không bị xê dịch
            thachSanh.transform.position = startPos;
            yield return null;
        }

        if (thachSanh != null)
        {
            thachSanh.transform.rotation = targetRotation;
            thachSanh.transform.position = startPos;
        }
    }

    IEnumerator KichBanNhoCuuCongChua()
    {
        // 1. Tạm khóa toàn bộ điều khiển di chuyển và tấn công của Thạch Sanh
        SetPlayerControl(false);

        // 2. Chuyển sang góc Camera Hội Thoại nếu có
        if (mainCamera != null) mainCamera.SetActive(false);
        if (dialogueCamera != null) dialogueCamera.SetActive(true);

        isDialogueActive = true;

        // Câu 1: MẸ LÝ THÔNG cất tiếng gọi -> Thạch Sanh tự động quay người lại đối diện Mẹ Lý Thông
        RotateThachSanhTowards(meLyThong);
        ShowDialogueLine("MẸ LÝ THÔNG", "Ôi Thạch Sanh con ơi! Mẹ và anh Thông tìm con mỏi cả mắt! May quá con đây rồi!", colorMeLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        // Câu 2: THẠCH SANH đáp lại
        ShowDialogueLine("THẠCH SANH", "Ủa, mẹ và anh Thông? Sao hai người lại vào tận chốn rừng sâu hiểm độc thế này?", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());

        // Câu 3: LÝ THÔNG nói -> Thạch Sanh quay người hướng về Lý Thông
        RotateThachSanhTowards(lyThong);
        ShowDialogueLine("LÝ THÔNG", "Em ơi, Công chúa vừa bị Đại Bàng tinh quắp bay về phía rặng núi mờ kia rồi!", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        // Câu 4: MẸ LÝ THÔNG khẩn khoản -> Thạch Sanh quay người hướng về Mẹ Lý Thông
        RotateThachSanhTowards(meLyThong);
        ShowDialogueLine("MẸ LÝ THÔNG", "Anh con thì sức yếu tài hèn... Con hãy thương lấy mẹ và anh, giúp gia đình ta cứu công chúa nhé!", colorMeLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        // Câu 5: THẠCH SANH khẳng định
        ShowDialogueLine("THẠCH SANH", "Mẹ và anh đừng lo! Đệ sẽ đi tìm hang ổ của Đại Bàng tinh để cứu công chúa!", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());

        // Câu 6: LÝ THÔNG kết luận -> Thạch Sanh quay người hướng về Lý Thông
        RotateThachSanhTowards(lyThong);
        ShowDialogueLine("LÝ THÔNG", "Tốt lắm! Anh và mẹ sẽ đi cùng em .", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 3. Trả lại Camera chính để theo dõi nhân vật di chuyển
        if (mainCamera != null) mainCamera.SetActive(true);
        if (dialogueCamera != null) dialogueCamera.SetActive(false);

        // ==========================================
        // 4. RA LỆNH CHO LÝ THÔNG VÀ MẸ ĐI ĐẾN CỬA HANG, MỞ LẠI ĐIỀU KHIỂN CHO THẠCH SANH
        // ==========================================
        if (caveDestination != null)
        {
            MoveCharacter(lyThong);
            MoveCharacter(meLyThong);
        }

        SetPlayerControl(true);
    }

    // Hàm gắn AI và ra lệnh đi đến đích
    private void MoveCharacter(GameObject character)
    {
        if (character == null || caveDestination == null) return;

        NavMeshAgent agent = character.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = character.AddComponent<NavMeshAgent>();
            agent.speed = 5f;
        }

        // Cài đặt khoảng cách dừng để các nhân vật không đùn đẩy nhau khi cùng đứng 1 chỗ
        agent.stoppingDistance = 1.5f;

        Animator anim = character.GetComponent<Animator>();
        if (anim == null) anim = character.GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.SetFloat("Speed", 1f);
        }

        agent.SetDestination(caveDestination.position);

        // Bắt đầu luồng theo dõi để ép nhân vật dừng lại khi đến đích
        StartCoroutine(KiemTraDenDich(agent, anim));
    }

    // Luồng theo dõi khoảng cách
    IEnumerator KiemTraDenDich(NavMeshAgent agent, Animator anim)
    {
        // Chờ 1 chút để AI kịp tính toán đường đi
        yield return new WaitForSeconds(0.2f);

        // Liên tục kiểm tra: Khi khoảng cách còn lại lớn hơn khoảng cách dừng thì vẫn đợi
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null; // Tạm dừng 1 frame rồi kiểm tra tiếp
        }

        // KHI VÒNG LẶP KẾT THÚC (ĐÃ ĐẾN NƠI) -> CHUYỂN VỀ TRẠNG THÁI ĐỨNG YÊN
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
        }
    }

    private void ShowDialogueLine(string speaker, string message, Color speakerColor)
    {
        currentDialogueSpeaker = speaker;
        currentDialogueMessage = message;
        currentDialogueColor = speakerColor;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null) { speakerText.text = speaker + ":"; speakerText.color = speakerColor; }
            if (contentText != null) { contentText.text = $"\"{message}\""; }
            if (hintText != null) { hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)"; }
        }
    }

    void OnGUI()
    {
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy) return;

        if (isDialogueActive && !string.IsNullOrEmpty(currentDialogueMessage))
        {
            GUI.depth = -100;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = GetBoxBackground();
            boxStyle.wordWrap = true;
            boxStyle.alignment = TextAnchor.UpperLeft;

            // 1. Thu nhỏ khoảng cách từ chữ ra lề hộp thoại (Padding)
            boxStyle.padding = new RectOffset(20, 20, 10, 10);
            boxStyle.richText = true;

            // 2. Bóp nhỏ chiều rộng và chiều cao của hộp nền đen
            float width = Mathf.Min(Screen.width * 0.55f, 650f); // Giảm chiều rộng
            float height = 120f; // Giảm chiều cao từ 215 xuống 120
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - height - 20f; // Nằm sát mép dưới màn hình hơn

            string colorHex = ColorUtility.ToHtmlStringRGB(currentDialogueColor);

            // 3. Giảm cỡ chữ (size) của Tên, Nội dung và Dòng nhắc nhở
            string fullMsg = $"<size=24><b><color=#{colorHex}>{currentDialogueSpeaker}:</color></b></size>\n" +
                             $"<size=18><color=#FFFFFF>\"{currentDialogueMessage}\"</color></size>\n\n" +
                             $"<color=#FFD700><size=15><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";

            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
    }


}