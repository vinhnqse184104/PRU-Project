using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class QuestManager : MonoBehaviour
{
    [Header("Bảng hiển thị nhiệm vụ")]
    public TextMeshProUGUI questText;

    [Header("Nhân vật sự kiện")]
    public GameObject lyThongNPC; // Quản lý Lý Thông

    [Header("THÊM: Animator của Lý Thông")]
    public Animator lyThongAnimator; // Dùng để ép đứng dậy

    [Header("UI Hội thoại Canvas uGUI (Chuẩn Chapter 4)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    public void EnsureDialogueUI()
    {
        if (dialoguePanel == null || speakerText == null || contentText == null || hintText == null)
        {
            dialoguePanel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter1_DialogueCanvas", out speakerText, out contentText, out hintText);
        }
    }

    [Header("Số lượng đã đạt được")]
    public int woodCollected = 0;
    public int chickenKilled = 0;
    public int deerKilled = 0;
    public int horseKilled = 0;
    public int tigerKilled = 0;

    [Header("Yêu cầu Nhiệm vụ 1")]
    public int woodRequired = 2;
    public int chickenRequired = 2;
    public int deerRequired = 1;
    public int horseRequired = 1;
    public int tigerRequired = 2;

    private int currentQuest = 1;
    private bool isTransitioning = false;

    void Start()
    {
        if (StoryStateManager.LyThongReturnedAsOfficial)
        {
            if (questText != null) questText.gameObject.SetActive(false);
            if (lyThongNPC != null) lyThongNPC.SetActive(false);
            enabled = false;
            return;
        }

        if (lyThongNPC != null)
        {
            lyThongNPC.SetActive(false);
        }
        UpdateQuestUI();
    }

    public void AddKill(string animalName)
    {
        if (animalName == "Chicken") chickenKilled++;
        else if (animalName == "Deer") deerKilled++;
        else if (animalName == "Horse") horseKilled++;
        else if (animalName == "Tiger") tigerKilled++;
        UpdateQuestUI();
    }

    public void CollectWood()
    {
        woodCollected++;
        UpdateQuestUI();
    }

    void UpdateQuestUI()
    {
        if (questText == null || isTransitioning) return;

        if (currentQuest == 1)
        {
            if (woodCollected >= woodRequired && chickenKilled >= chickenRequired &&
                deerKilled >= deerRequired && horseKilled >= horseRequired && tigerKilled >= tigerRequired)
            {
                StartCoroutine(CompleteQuestOne());
            }
            else
            {
                questText.text =
                    "NHIỆM VỤ ĐẦU TIÊN:\n" +
                    "- Đốn củi: " + woodCollected + " / " + woodRequired + "\n" +
                    "- Tiêu diệt Gà: " + chickenKilled + " / " + chickenRequired + "\n" +
                    "- Tiêu diệt Hươu: " + deerKilled + " / " + deerRequired + "\n" +
                    "- Tiêu diệt Ngựa: " + horseKilled + " / " + horseRequired + "\n" +
                    "- Tiêu diệt Hổ: " + tigerKilled + " / " + tigerRequired;
                questText.color = Color.white;
            }
        }
        else if (currentQuest == 2)
        {
            questText.text = "NHIỆM VỤ 2:\nĐi vòng quanh khu rừng cho đến khi thấy được Lý Thông";
            questText.color = Color.yellow;
        }
    }

    IEnumerator CompleteQuestOne()
    {
        isTransitioning = true;
        questText.text = "ĐÃ HOÀN THÀNH XONG NHIỆM VỤ!";
        questText.color = Color.green;
        yield return new WaitForSeconds(3f);

        currentQuest = 2;
        isTransitioning = false;

        if (lyThongNPC != null)
        {
            lyThongNPC.SetActive(true);
        }
        UpdateQuestUI();
    }

    public void MeetLyThong()
    {
        if (currentQuest == 2)
        {
            currentQuest = 3;
            StartCoroutine(KichBanHoiThoai());
        }
    }

    private bool isDialogueActive = false;
    private string currentDialogueSpeaker = "";
    private string currentDialogueMessage = "";
    private Color currentDialogueColor = Color.white;
    private bool isChapter1Completed = false;
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

    IEnumerator WaitForKeyPress()
    {
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

    // Hội thoại giữa Thạch Sanh và Lý Thông
    IEnumerator KichBanHoiThoai()
    {
        // 1. TỰ ĐỘNG TÌM KIẾM ANIMATOR (Khắc phục hoàn toàn lỗi quên kéo thả)
        if (lyThongAnimator == null && lyThongNPC != null)
        {
            lyThongAnimator = lyThongNPC.GetComponent<Animator>();
            if (lyThongAnimator == null)
            {
                lyThongAnimator = lyThongNPC.GetComponentInChildren<Animator>();
            }
        }

        // 2. PHÁT LỆNH ÉP ĐỨNG DẬY CỰC MẠNH NGAY KHI HIỆN CÂU THOẠI ĐẦU TIÊN
        if (lyThongAnimator != null)
        {
            lyThongAnimator.SetTrigger("StandUp");
            Debug.Log("<color=cyan>ĐÃ TÌM THẤY ANIMATOR TRONG QUEST MANAGER VÀ ÉP LÝ THÔNG ĐỨNG DẬY!</color>");
        }
        else
        {
            Debug.LogError("<color=red>LỖI CHÍ MẠNG: QUEST MANAGER VẪN KHÔNG THỂ TÌM THẤY BỘ NÃO ANIMATOR CỦA LÝ THÔNG!</color>");
        }

        isDialogueActive = true;
        if (questText != null) questText.text = "";

        // ================= CÂU 1 =================
        ShowDialogueLine("THẠCH SANH", "Chào huynh! Sao huynh lại nằm ngủ giữa chốn rừng thiêng nước độc thế này?", new Color(0.3f, 0.8f, 1.0f));
        yield return StartCoroutine(WaitForKeyPress());

        // ================= CÂU 2 =================
        ShowDialogueLine("LÝ THÔNG", "Ây da... ta đi bán rượu ngang qua, mệt quá nên thiếp đi mất. Cảm ơn chú em đã đánh thức! Mà nhìn chú em sức vóc quả là hơn người!", new Color(1.0f, 0.72f, 0.2f));
        yield return StartCoroutine(WaitForKeyPress());

        // ================= CÂU 3 =================
        ShowDialogueLine("THẠCH SANH", "Đệ mồ côi cha mẹ từ nhỏ, sống lủi thủi ở gốc đa này, ngày ngày đốn củi kiếm sống qua ngày thôi.", new Color(0.3f, 0.8f, 1.0f));
        yield return StartCoroutine(WaitForKeyPress());

        // ================= CÂU 4 =================
        ShowDialogueLine("LÝ THÔNG", "Ôi, hóa ra chú em cũng đơn độc. Hay là theo ta về nhà, anh em ta kết nghĩa huynh đệ, có rau ăn rau có cháo ăn cháo, chú em thấy sao?", new Color(1.0f, 0.72f, 0.2f));
        yield return StartCoroutine(WaitForKeyPress());

        // ================= KẾT THÚC CHƯƠNG 1 =================
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        isChapter1Completed = true;
        if (questText != null)
        {
            //questText.text = "HOÀN THÀNH CHƯƠNG 1!\n\nCHƯƠNG 2: GẶP LÝ THÔNG";
            //questText.color = Color.green;
        }

        yield return new WaitForSeconds(3f);
        StoryStateManager.StartNewGame();
        SceneManager.LoadScene("Chapter2_LyThong");
    }

    private void ShowDialogueLine(string speaker, string message, Color speakerColor)
    {
        EnsureDialogueUI();
        currentDialogueSpeaker = speaker;
        currentDialogueMessage = message;
        currentDialogueColor = speakerColor;

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

    void OnGUI()
    {
        // Nếu đã có Canvas DialoguePanel đang hiển thị thì không vẽ OnGUI để tránh trùng lặp
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            return;
        }

        if (isDialogueActive && !string.IsNullOrEmpty(currentDialogueMessage))
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

            string colorHex = ColorUtility.ToHtmlStringRGB(currentDialogueColor);
            string fullMsg = $"<size=34><b><color=#{colorHex}>{currentDialogueSpeaker}:</color></b></size>\n" +
                             $"<size=28>\"{currentDialogueMessage}\"</size>\n\n" +
                             $"<color=#FFD700><size=20><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";

            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
        else if (isChapter1Completed)
        {
            GUIStyle compStyle = new GUIStyle(GUI.skin.box);
            compStyle.normal.background = GetBoxBackground();
            compStyle.fontSize = 22;
            compStyle.alignment = TextAnchor.MiddleCenter;
            compStyle.normal.textColor = Color.green;
            compStyle.richText = true;

            float width = 520f;
            float height = 75f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - height - 40f;

            GUI.Box(new Rect(left, top, width, height),
                "<b><color=#00FF00>HOÀN THÀNH CHƯƠNG 1!</color></b>\n<size=18><color=#FFD700>Theo Lý Thông về nhà...</color></size>",
                compStyle);
        }
    }
}