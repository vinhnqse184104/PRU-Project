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

    IEnumerator KichBanNhoCuuCongChua()
    {
        // 1. Tạm khóa người chơi điều khiển Thạch Sanh khi đang nói chuyện
        if (thachSanhMovement != null) thachSanhMovement.enabled = false;

        isDialogueActive = true;
        ShowDialogueLine("MẸ LÝ THÔNG", "Ôi Thạch Sanh con ơi! Mẹ và anh Thông tìm con mỏi cả mắt! May quá con đây rồi!", colorMeLyThong);
        yield return StartCoroutine(WaitForKeyPress());
        ShowDialogueLine("THẠCH SANH", "Ủa, mẹ và anh Thông? Sao hai người lại vào tận chốn rừng sâu hiểm độc thế này?", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());
        ShowDialogueLine("LÝ THÔNG", "Em ơi, Công chúa vừa bị Đại Bàng tinh quắp bay về phía rặng núi mờ kia rồi!", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());
        ShowDialogueLine("MẸ LÝ THÔNG", "Anh con thì sức yếu tài hèn... Con hãy thương lấy mẹ và anh, giúp gia đình ta cứu công chúa nhé!", colorMeLyThong);
        yield return StartCoroutine(WaitForKeyPress());
        ShowDialogueLine("THẠCH SANH", "Mẹ và anh đừng lo! Đệ sẽ đi tìm hang ổ của Đại Bàng tinh để cứu công chúa!", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());
        ShowDialogueLine("LÝ THÔNG", "Tốt lắm! Anh và mẹ sẽ đi cùng em .", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // ==========================================
        // 2. RA LỆNH CHO 3 NGƯỜI ĐI ĐẾN HANG ĐẠI BÀNG
        // ==========================================
        MoveCharacter(thachSanh);
        MoveCharacter(lyThong);
        MoveCharacter(meLyThong);

        // 1. Chuyển sang góc Camera Hội Thoại
        if (mainCamera != null) mainCamera.SetActive(false);
        if (dialogueCamera != null) dialogueCamera.SetActive(true);

        if (thachSanhMovement != null) thachSanhMovement.enabled = false;

        isDialogueActive = true;



        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 2. Hội thoại xong -> Trả lại Camera chính để theo dõi nhân vật chạy
        if (mainCamera != null) mainCamera.SetActive(true);
        if (dialogueCamera != null) dialogueCamera.SetActive(false);

        // 3. Ra lệnh chạy đến đích
        MoveCharacter(thachSanh);
        MoveCharacter(lyThong);
        MoveCharacter(meLyThong);
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