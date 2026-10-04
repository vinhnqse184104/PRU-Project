using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem; // Bắt buộc phải có dòng này để nhận phím

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
                questText.text = "NHIỆM VỤ ĐẦU TIÊN:\n" +
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
            // ĐÃ THAY ĐỔI CÂU CHỮ THEO ĐÚNG Ý BẠN
            questText.text = "NHIỆM VỤ 2:\nĐi vòng quanh khu rừng cho đến khi thấy được Lý Thông";
            questText.color = Color.yellow; // Đổi màu vàng cho nổi bật nhiệm vụ mới
        }
    }

    IEnumerator CompleteQuestOne()
    {
        isTransitioning = true; // Khóa UI không cho cập nhật số nữa

        // Hiện thông báo hoàn thành
        questText.text = "ĐÃ HOÀN THÀNH XONG NHIỆM VỤ!";
        questText.color = Color.green;

        // Chờ đúng 3 giây
        yield return new WaitForSeconds(3f);

        // Chuyển sang Nhiệm vụ 2 và mở khóa UI
        currentQuest = 2;
        isTransitioning = false;

        // Cho Lý Thông xuất hiện ở gốc đa
        if (lyThongNPC != null)
        {
            lyThongNPC.SetActive(true);
        }

        // Gọi hàm update để in ra dòng chữ Nhiệm vụ 2
        UpdateQuestUI();
    }

    // Gọi khi Thạch Sanh lại gần Lý Thông
    public void MeetLyThong()
    {
        if (currentQuest == 2)
        {
            currentQuest = 3; // Chốt chuyển sang nhiệm vụ 3

            // Bắt đầu chạy kịch bản hội thoại theo thời gian
            StartCoroutine(KichBanHoiThoai());
        }
    }

    // Bộ đếm thời gian cho hội thoại
    // Bộ đếm thời gian cho kịch bản hội thoại dài
    // HÀM MỚI: Chuyên dùng để chờ người chơi bấm nút
    IEnumerator WaitForKeyPress()
    {
        // Vòng lặp này sẽ giữ game đứng đợi mãi mãi...
        while (true)
        {
            // ...cho đến khi người chơi Bấm phím Enter hoặc Chuột trái thì mới phá vòng lặp đi tiếp
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                break;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                break;

            yield return null; // Chờ khung hình (frame) tiếp theo
        }
        // Đợi 1 chút xíu (0.1s) để tránh lỗi bấm đúp (double-click) trôi 2 câu cùng lúc
        yield return new WaitForSeconds(0.1f);
    }

    // Bộ đếm hội thoại đã được nâng cấp sang "Bấm nút chuyển câu"
    IEnumerator KichBanHoiThoai()
    {
        // Câu hướng dẫn mờ mờ ở dưới cùng
        string huongDan = "(Bấm Enter hoặc Chuột trái để tiếp tục cuộc hội thoại.)";

        // ================= TRANG 1: HỎI HAN =================
        string cau1 = "THẠCH SANH:\n\"Chào huynh! Sao huynh lại nằm ngủ giữa chốn rừng thiêng nước độc thế này?\"";
        string cau2 = "\n\nLÝ THÔNG:\n\"Ây da... ta đi bán rượu ngang qua, mệt quá nên thiếp đi mất. Cảm ơn chú em đã đánh thức! Mà nhìn chú em sức vóc quả là hơn người!\"";

        // In câu 1
        questText.text = cau1 + huongDan;
        questText.color = Color.white;
        yield return StartCoroutine(WaitForKeyPress()); // CHỜ BẤM NÚT MỚI ĐI TIẾP

        // In câu 1 + 2
        questText.text = cau1 + cau2 + huongDan;
        yield return StartCoroutine(WaitForKeyPress()); // CHỜ BẤM NÚT


        // ================= TRANG 2: KẾT NGHĨA =================
        string cau3 = "THẠCH SANH:\n\"Đệ mồ côi cha mẹ từ nhỏ, sống lủi thủi ở gốc đa này, ngày ngày đốn củi kiếm sống qua ngày thôi.\"";
        string cau4 = "\n\nLÝ THÔNG:\n\"Ôi, hóa ra chú em cũng đơn độc. Hay là theo ta về nhà, anh em ta kết nghĩa huynh đệ, có rau ăn rau có cháo ăn cháo, chú em thấy sao?\"";

        // Sang trang mới, xóa chữ cũ in câu 3
        questText.text = cau3 + huongDan;
        yield return StartCoroutine(WaitForKeyPress()); // CHỜ BẤM NÚT

        // In câu 3 + 4
        questText.text = cau3 + cau4 + huongDan;
        yield return StartCoroutine(WaitForKeyPress()); // CHỜ BẤM NÚT


        
    }
}