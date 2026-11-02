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
    /// - Đặt lại vị trí Thạch Sanh về SpawnPoint ban đầu của Chapter 1.
    /// - Đặt lại nhiệm vụ mở đầu (QuestManager) đốn củi và săn thú.
    /// - Gỡ bỏ vật phẩm đầu Chằn Tinh nếu có.
    /// </summary>
    public void StartNewGameFromChapter1()
    {
        // 1. Reset trạng thái cốt truyện
        StoryStateManager.StartNewGame();

        // 2. Reset QuestManager
        QuestManager qm = Object.FindAnyObjectByType<QuestManager>();
        if (qm != null)
        {
            qm.woodCollected = 0;
            qm.chickenKilled = 0;
            qm.deerKilled = 0;
            qm.horseKilled = 0;
            qm.tigerKilled = 0;
            if (qm.lyThongNPC != null)
            {
                qm.lyThongNPC.SetActive(false);
            }
            qm.SendMessage("UpdateQuestUI", SendMessageOptions.DontRequireReceiver);
        }

        // 3. Gỡ bỏ vật phẩm chiến lợi phẩm
        PlayerTrophyHolder trophyHolder = Object.FindAnyObjectByType<PlayerTrophyHolder>();
        if (trophyHolder != null)
        {
            trophyHolder.RemoveTrophy();
        }

        // 4. Đặt lại vị trí Thạch Sanh về điểm xuất phát gốc của Chapter 1
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) player = pm.gameObject;
        }

        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = new Vector3(96.46f, -0.035f, 92.28f);
            player.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();

            if (cc != null) cc.enabled = true;
        }

        statusMessage = "ĐÃ BẮT ĐẦU LƯỢT CHƠI MỚI! Tiến trình nhiệm vụ đã được đặt lại ban đầu.";
        messageTimer = 4.0f;
        Debug.Log("[Chapter1NewGameHandler] Đã chủ động bắt đầu lượt chơi mới (New Game) thành công từ Chapter 1.");
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
