using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Chapter1NewGameHandler : MonoBehaviour
{
    public static Chapter1NewGameHandler Instance { get; private set; }

    private string statusMessage = "";
    private float messageTimer = 0f;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "RungThachSanh")
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
            {
                statusMessage = "";
            }
        }

        // Nhận phím F1 để chủ động bắt đầu lượt chơi mới từ Chapter 1
        bool f1Pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            f1Pressed = true;
        }
#endif
        if (!f1Pressed && Input.GetKeyDown(KeyCode.F1))
        {
            f1Pressed = true;
        }

        if (f1Pressed)
        {
            StartNewGameFromChapter1();
        }
    }

    /// <summary>
    /// Bắt đầu lượt chơi mới từ Chapter 1:
    /// - Xóa đúng 4 PlayerPrefs của cốt truyện (không dùng DeleteAll).
    /// - Khôi phục các nhân vật theo Transform gốc đã lưu trong Scene.
    /// - Nạp lại Chapter 1 với mục tiêu tự do tìm Lý Thông.
    /// - Gỡ bỏ vật phẩm đầu Chằn Tinh nếu có.
    /// </summary>
    public void StartNewGameFromChapter1()
    {
        // Keep the existing explicit New Game reset; a normal scene start never clears saves.
        StoryStateManager.StartNewGame();
        // Reload also resets LyThongInteraction.isMet and its standing model without moving NPCs in code.
        SceneManager.LoadScene("RungThachSanh");
    }

    void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "RungThachSanh") return;

        // Hiển thị thông báo thành công khi vừa ấn reset
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.color = Color.green;
            GUI.Box(new Rect(Screen.width / 2f - 260f, 15f, 520f, 35f), statusMessage);
            GUI.color = Color.white;
        }

        // Nếu còn lưu tiến trình cũ từ Chapter 2 hoặc 3 (ví dụ do chạy test trước đó)
        if (StoryStateManager.HasSavedProgress)
        {
            QuestManager forestQuest = Object.FindAnyObjectByType<QuestManager>();
            if (forestQuest != null && forestQuest.IsForestGameplayStarted) return;
            GUI.color = new Color(1f, 0.9f, 0.3f, 1f);
            Rect warningBox = new Rect(15f, 15f, 360f, 75f);
            GUI.Box(warningBox, "⚠️ PHÁT HIỆN DỮ LIỆU TỪ LƯỢT CHƠI TRƯỚC\nNhấn [F1] hoặc bấm nút bên dưới để\nBắt đầu chơi mới (Reset tiến trình)");

            if (GUI.Button(new Rect(25f, 60f, 340f, 24f), ">> Bắt đầu lượt chơi mới (New Game - F1) <<"))
            {
                StartNewGameFromChapter1();
            }
            GUI.color = Color.white;
        }
    }
}
