using UnityEngine;
using UnityEngine.InputSystem;

public class Chapter3Intro : MonoBehaviour
{
    [Header("Nội dung dẫn truyện")]
    [TextArea(5, 10)]
    public string introText = "Đêm hôm đó, Lý Thông lấy cớ bận việc, nhờ Thạch Sanh đi canh miếu thờ thay mình.\n\nThạch Sanh thật thà mang búa ra đi mà không hề hay biết rằng... đây là miếu thờ của một con Chằn Tinh hung ác chuyên ăn thịt người.";

    private bool isShowing = true;
    private PlayerMovement playerMovement;

    void Start()
    {
        // 1. Tìm Thạch Sanh và khóa di chuyển khi mới vào màn
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("thachsanh");

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null) playerMovement.enabled = false;
        }
    }

    void Update()
    {
        if (!isShowing) return;

        // 2. Chờ người chơi bấm Space hoặc Click chuột
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            isShowing = false;

            // 3. Ẩn chữ và mở khóa di chuyển cho Thạch Sanh
            if (playerMovement != null) playerMovement.enabled = true;
        }
    }

    void OnGUI()
    {
        if (isShowing)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 20; // Giảm nhẹ từ 24 xuống 20 để chữ gọn gàng
            boxStyle.wordWrap = true;
            boxStyle.alignment = TextAnchor.MiddleCenter;
            boxStyle.normal.textColor = Color.white;

            // Tạo khoảng đệm cách mép viền để chữ không bị dính vào mép hộp
            boxStyle.padding = new RectOffset(35, 35, 25, 25);
            boxStyle.richText = true;

            float width = Screen.width * 0.8f;   // Độ rộng chiếm 80% màn hình
            float height = 290f;                 // Tăng chiều cao lên 290 để chứa đủ các dòng
            float left = (Screen.width - width) / 2f;
            float top = (Screen.height - height) / 2f; // Căn chính giữa màn hình

            string fullText = introText + "\n\n<color=#FFD700><size=17>(Bấm Space hoặc Click chuột để bắt đầu)</size></color>";

            GUI.skin.box.richText = true;
            GUI.Box(new Rect(left, top, width, height), fullText, boxStyle);
        }
    }
}