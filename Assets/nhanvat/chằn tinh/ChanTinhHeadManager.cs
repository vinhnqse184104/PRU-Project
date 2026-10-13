using UnityEngine;
using UnityEngine.SceneManagement;

public class ChanTinhHeadManager : MonoBehaviour
{
    [Header("Đầu Chằn Tinh (Kéo thả từ xương tay vào đây)")]
    public GameObject dauChanTinh;

    void Start()
    {
        CheckAndApplyTrophy();
    }

    public void CheckAndApplyTrophy()
    {
        if (dauChanTinh == null) return;

        string currentScene = SceneManager.GetActiveScene().name;

        // CHỈ KIỂM TRA VÀ HIỆN ĐẦU KHI ĐANG Ở ĐÚNG SCENE CHUYỂN GIAO (NHÀ LÝ THÔNG)
        // Lưu ý: Đổi "Chapter2_LyThong" thành tên Scene thực tế của bạn nếu bạn đặt tên khác
        if (currentScene == "Chapter2_LyThong")
        {
            if (StoryStateManager.HasChanTinhHead == true || StoryStateManager.IsChanTinhDefeated == true)
            {
                dauChanTinh.SetActive(true);
            }
            else
            {
                dauChanTinh.SetActive(false);
            }
        }
        else
        {
            // Nếu đang ở bất kỳ Scene nào khác (kể cả lúc đang đứng test như trong ảnh), GIẤU NGAY LẬP TỨC
            dauChanTinh.SetActive(false);
        }
    }
}