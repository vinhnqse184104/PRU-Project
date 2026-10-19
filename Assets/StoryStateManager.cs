using UnityEngine;

public static class StoryStateManager
{
    private const string KEY_CHAN_TINH_DEFEATED = "PRU_ChanTinhDefeated";
    private const string KEY_HAS_CHAN_TINH_HEAD = "PRU_HasChanTinhHead";
    private const string KEY_CHAPTER2_RETURNED = "PRU_Chapter2Returned";
    private const string KEY_RETURN_DIALOGUE_DONE = "PRU_ReturnDialogueDone";
    private const string KEY_CHAPTER4_REACHED = "PRU_Chapter4Reached";
    private const string KEY_LY_THONG_OFFICIAL_RETURNED = "PRU_LyThongReturnedAsOfficial";
    private const string KEY_CHAPTER4_COMPLETED = "PRU_M4_Chapter4Completed";
    private const string KEY_CHAPTER5_COMPLETED = "PRU_M4_Chapter5Completed";

    public static bool IsChapter4Completed
    {
        get => PlayerPrefs.GetInt(KEY_CHAPTER4_COMPLETED, 0) == 1;
        set { PlayerPrefs.SetInt(KEY_CHAPTER4_COMPLETED, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool IsChapter5Completed
    {
        get => PlayerPrefs.GetInt(KEY_CHAPTER5_COMPLETED, 0) == 1;
        set { PlayerPrefs.SetInt(KEY_CHAPTER5_COMPLETED, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool LyThongReturnedAsOfficial
    {
        get => PlayerPrefs.GetInt(KEY_LY_THONG_OFFICIAL_RETURNED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_LY_THONG_OFFICIAL_RETURNED, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsChanTinhDefeated
    {
        get => PlayerPrefs.GetInt(KEY_CHAN_TINH_DEFEATED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_CHAN_TINH_DEFEATED, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool HasChanTinhHead
    {
        get => PlayerPrefs.GetInt(KEY_HAS_CHAN_TINH_HEAD, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_HAS_CHAN_TINH_HEAD, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsReturningToChapter2
    {
        get => PlayerPrefs.GetInt(KEY_CHAPTER2_RETURNED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_CHAPTER2_RETURNED, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsReturnDialogueCompleted
    {
        get => PlayerPrefs.GetInt(KEY_RETURN_DIALOGUE_DONE, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_RETURN_DIALOGUE_DONE, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsChapter4Reached
    {
        get => PlayerPrefs.GetInt(KEY_CHAPTER4_REACHED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_CHAPTER4_REACHED, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Kiểm tra xem hiện tại có bất kỳ dữ liệu tiến trình cốt truyện nào đang được lưu không.
    /// </summary>
    public static bool HasSavedProgress
    {
        get => PlayerPrefs.HasKey(KEY_CHAN_TINH_DEFEATED) ||
               PlayerPrefs.HasKey(KEY_HAS_CHAN_TINH_HEAD) ||
               PlayerPrefs.HasKey(KEY_CHAPTER2_RETURNED) ||
               PlayerPrefs.HasKey(KEY_RETURN_DIALOGUE_DONE) ||
               PlayerPrefs.HasKey(KEY_CHAPTER4_REACHED) ||
               PlayerPrefs.HasKey(KEY_LY_THONG_OFFICIAL_RETURNED) ||
               PlayerPrefs.HasKey(KEY_CHAPTER4_COMPLETED) || PlayerPrefs.HasKey(KEY_CHAPTER5_COMPLETED);
    }

    /// <summary>
    /// Bắt đầu lượt chơi mới (New Game):
    /// Chỉ xóa đúng các key liên quan đến tiến trình cốt truyện, không dùng PlayerPrefs.DeleteAll().
    /// </summary>
    public static void StartNewGame()
    {
        PlayerPrefs.DeleteKey(KEY_CHAN_TINH_DEFEATED);
        PlayerPrefs.DeleteKey(KEY_HAS_CHAN_TINH_HEAD);
        PlayerPrefs.DeleteKey(KEY_CHAPTER2_RETURNED);
        PlayerPrefs.DeleteKey(KEY_RETURN_DIALOGUE_DONE);
        PlayerPrefs.DeleteKey(KEY_CHAPTER4_REACHED);
        PlayerPrefs.DeleteKey(KEY_LY_THONG_OFFICIAL_RETURNED);
        PlayerPrefs.DeleteKey(KEY_CHAPTER4_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_CHAPTER5_COMPLETED);
        PlayerPrefs.Save();
        Debug.Log("[StoryStateManager] BẮT ĐẦU LƯỢT CHƠI MỚI (New Game): Đã reset toàn bộ tiến trình cốt truyện về ban đầu.");
    }

    /// <summary>
    /// Alias cho StartNewGame để giữ tương thích ngược.
    /// </summary>
    public static void ResetProgress()
    {
        StartNewGame();
    }
}
