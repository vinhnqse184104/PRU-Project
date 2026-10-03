using UnityEngine;
using TMPro; // BẮT BUỘC THÊM DÒNG NÀY ĐỂ DÙNG TEXTMESHPRO

public class QuestManager : MonoBehaviour
{
    [Header("Bảng hiển thị nhiệm vụ")]
    public TextMeshProUGUI questText; // ĐÃ ĐỔI TỪ "Text" SANG "TextMeshProUGUI"

    [Header("Số lượng đã giết")]
    public int chickenKilled = 0;
    public int deerKilled = 0;
    public int horseKilled = 0;
    public int tigerKilled = 0;

    void Start()
    {
        UpdateQuestUI(); // Cập nhật chữ ngay khi vừa vào game
    }

    // Hàm này sẽ được gọi mỗi khi có 1 con thú chết
    public void AddKill(string animalName)
    {
        if (animalName == "Chicken") chickenKilled++;
        else if (animalName == "Deer") deerKilled++;
        else if (animalName == "Horse") horseKilled++;
        else if (animalName == "Tiger") tigerKilled++;

        UpdateQuestUI(); // Đếm xong thì cập nhật lại bảng chữ
    }

    // Hàm cập nhật chữ trên màn hình
    void UpdateQuestUI()
    {
        if (questText == null) return;

        // Nếu đã giết đủ số lượng yêu cầu
        if (chickenKilled >= 3 && deerKilled >= 1 && horseKilled >= 1 && tigerKilled >= 2)
        {
            questText.text = "NHIỆM VỤ 1: ĐÃ HOÀN THÀNH!";
            questText.color = Color.green; // Đổi chữ thành màu xanh lá
        }
        else
        {
            // Nếu chưa đủ thì in ra tiến độ
            questText.text = "NHIỆM VỤ ĐẦU TIÊN:\n" +
                             "- Tiêu diệt Gà: " + chickenKilled + " / 3\n" +
                             "- Tiêu diệt Hươu: " + deerKilled + " / 1\n" +
                             "- Tiêu diệt Ngựa: " + horseKilled + " / 1\n" +
                             "- Tiêu diệt Hổ: " + tigerKilled + " / 2";
        }
    }
}