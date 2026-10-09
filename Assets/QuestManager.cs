using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem; // Bắt buộc phải có dòng này để nhận phím
using UnityEngine.SceneManagement; // THÊM: dùng để chuyển Scene

public class QuestManager : MonoBehaviour
{
    [Header("Bảng hiển thị nhiệm vụ")]
    public TextMeshProUGUI questText;

    [Header("Nhân vật sự kiện")]
    public GameObject lyThongNPC; // Quản lý Lý Thông

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
        // Tắt Lý Thông ngay khi mới vào game (giấu đi)
        if (lyThongNPC != null)
        {
            lyThongNPC.SetActive(false);
        }

        UpdateQuestUI();
    }

    public void AddKill(string animalName)
    {
        if (animalName == "Chicken")
            chickenKilled++;

        else if (animalName == "Deer")
            deerKilled++;

        else if (animalName == "Horse")
            horseKilled++;

        else if (animalName == "Tiger")
            tigerKilled++;

        UpdateQuestUI();
    }

    public void CollectWood()
    {
        woodCollected++;

        UpdateQuestUI();
    }

    void UpdateQuestUI()
    {
        if (questText == null || isTransitioning)
            return;

        if (currentQuest == 1)
        {
            if (woodCollected >= woodRequired &&
                chickenKilled >= chickenRequired &&
                deerKilled >= deerRequired &&
                horseKilled >= horseRequired &&
                tigerKilled >= tigerRequired)
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
            questText.text =
                "NHIỆM VỤ 2:\n" +
                "Đi vòng quanh khu rừng cho đến khi thấy được Lý Thông";

            questText.color = Color.yellow;
        }
    }

    IEnumerator CompleteQuestOne()
    {
        isTransitioning = true;

        // Hiện thông báo hoàn thành nhiệm vụ đầu tiên
        questText.text = "ĐÃ HOÀN THÀNH XONG NHIỆM VỤ!";
        questText.color = Color.green;

        // Chờ 3 giây
        yield return new WaitForSeconds(3f);

        // Chuyển sang Nhiệm vụ 2
        currentQuest = 2;
        isTransitioning = false;

        // Cho Lý Thông xuất hiện
        if (lyThongNPC != null)
        {
            lyThongNPC.SetActive(true);
        }

        UpdateQuestUI();
    }

    // Gọi khi Thạch Sanh lại gần Lý Thông
    public void MeetLyThong()
    {
        if (currentQuest == 2)
        {
            currentQuest = 3;

            StartCoroutine(KichBanHoiThoai());
        }
    }

    // Chờ người chơi bấm Enter hoặc Chuột trái
    IEnumerator WaitForKeyPress()
    {
        while (true)
        {
            if (Keyboard.current != null &&
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                break;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                break;
            }

            yield return null;
        }

        // Tránh bấm một lần nhảy qua nhiều câu
        yield return new WaitForSeconds(0.1f);
    }

    // Hội thoại giữa Thạch Sanh và Lý Thông
    IEnumerator KichBanHoiThoai()
    {
        string huongDan =
            "\n\n(Bấm Enter hoặc Chuột trái để tiếp tục cuộc hội thoại.)";

        // ================= TRANG 1 =================

        string cau1 =
            "THẠCH SANH:\n" +
            "\"Chào huynh! Sao huynh lại nằm ngủ giữa chốn rừng thiêng nước độc thế này?\"";

        string cau2 =
            "\n\nLÝ THÔNG:\n" +
            "\"Ây da... ta đi bán rượu ngang qua, mệt quá nên thiếp đi mất. " +
            "Cảm ơn chú em đã đánh thức! Mà nhìn chú em sức vóc quả là hơn người!\"";

        questText.text = cau1 + huongDan;
        questText.color = Color.white;

        yield return StartCoroutine(WaitForKeyPress());

        questText.text = cau1 + cau2 + huongDan;

        yield return StartCoroutine(WaitForKeyPress());

        // ================= TRANG 2 =================

        string cau3 =
            "THẠCH SANH:\n" +
            "\"Đệ mồ côi cha mẹ từ nhỏ, sống lủi thủi ở gốc đa này, " +
            "ngày ngày đốn củi kiếm sống qua ngày thôi.\"";

        string cau4 =
            "\n\nLÝ THÔNG:\n" +
            "\"Ôi, hóa ra chú em cũng đơn độc. Hay là theo ta về nhà, " +
            "anh em ta kết nghĩa huynh đệ, có rau ăn rau có cháo ăn cháo, " +
            "chú em thấy sao?\"";

        questText.text = cau3 + huongDan;

        yield return StartCoroutine(WaitForKeyPress());

        questText.text = cau3 + cau4 + huongDan;

        yield return StartCoroutine(WaitForKeyPress());

        // ================= KẾT THÚC CHƯƠNG 1 =================

        questText.text =
            "HOÀN THÀNH CHƯƠNG 1!\n\n" +
            "CHƯƠNG 2\n" +
            "GẶP LÝ THÔNG";

        questText.color = Color.green;

        // Chờ 3 giây trước khi chuyển cảnh
        yield return new WaitForSeconds(3f);

        // Chuyển sang Scene chương 2
        SceneManager.LoadScene("Chapter2_LyThong");
    }
}