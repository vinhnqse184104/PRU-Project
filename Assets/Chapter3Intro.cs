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

    void Update()
    {
        if (!isShowing) return;

        // 2. Chờ người chơi bấm Enter, Space hoặc Click chuột
        bool advancePressed = false;
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                         Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                         Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            advancePressed = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            advancePressed = true;
        }
        if (!advancePressed)
        {
            try
            {
                advancePressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                 Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            }
            catch { }
        }

        if (advancePressed)
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

            string cleanIntro = introText.Replace("\n\n", " ");
            string fullText = "<size=34><b><color=#FFD700>DẪN TRUYỆN:</color></b></size>\n" +
                             $"<size=26>\"{cleanIntro}\"</size>\n\n" +
                             "<color=#FFD700><size=20><i>(Bấm Enter, Space hoặc Chuột trái để bắt đầu)</i></size></color>";

            GUI.Box(new Rect(left, top, width, height), fullText, boxStyle);
        }
    }
}