using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class CaveArrivalDialogue : MonoBehaviour
{
    [Header("UI Hội thoại Canvas")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    [Header("Cài đặt Chuyển cảnh")]
    public GameObject dayThung;
    public GameObject mainCamera;     // Đã mang Main Camera trở lại
    public GameObject dialogueCamera; // Đã mang Dialogue Camera trở lại

    [Header("Nhân vật cần dừng lại")]
    public GameObject lyThong;
    public GameObject meLyThong;

    private bool isDialogueActive = false;
    private bool hasTriggered = false;
    private Texture2D boxBgTexture;

    private Color colorThachSanh = new Color(0.3f, 0.8f, 1.0f);
    private Color colorLyThong = new Color(1.0f, 0.72f, 0.2f);
    private Color colorMeLyThong = new Color(0.8f, 0.4f, 0.8f);

    private string currentDialogueSpeaker = "";
    private string currentDialogueMessage = "";
    private Color currentDialogueColor = Color.white;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            // 1. Dừng di chuyển
            DungNhanVat(other.gameObject);
            DungNhanVat(lyThong);
            DungNhanVat(meLyThong);

            // 2. Chạy kịch bản (truyền Thạch Sanh vào để xoay mặt)
            StartCoroutine(KichBanThaDay(other.gameObject));
        }
    }

    private void DungNhanVat(GameObject nhanVat)
    {
        if (nhanVat == null) return;
        var agent = nhanVat.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;
        var anim = nhanVat.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    // HÀM MỚI: Ép các nhân vật quay mặt nhìn nhau
    private void XoayMatVaoNhau(GameObject thachSanh)
    {
        if (thachSanh == null || lyThong == null || meLyThong == null) return;

        // Giữ nguyên trục Y để nhân vật không bị ngửa ra sau hay chúi nhủi xuống đất
        Vector3 viTriLyThong = new Vector3(lyThong.transform.position.x, thachSanh.transform.position.y, lyThong.transform.position.z);
        Vector3 viTriThachSanh = new Vector3(thachSanh.transform.position.x, lyThong.transform.position.y, thachSanh.transform.position.z);
        Vector3 viTriThachSanhChoMe = new Vector3(thachSanh.transform.position.x, meLyThong.transform.position.y, thachSanh.transform.position.z);

        // Thạch Sanh nhìn Lý Thông
        thachSanh.transform.LookAt(viTriLyThong);
        // Lý Thông và Mẹ Lý Thông nhìn Thạch Sanh
        lyThong.transform.LookAt(viTriThachSanh);
        meLyThong.transform.LookAt(viTriThachSanhChoMe);
    }

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
        yield return new WaitForSeconds(0.2f);
        while (true)
        {
            bool pressed = false;
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) pressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;
            if (!pressed) { try { pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0); } catch { } }
            if (pressed) break;
            yield return null;
        }
    }

    IEnumerator KichBanThaDay(GameObject thachSanh)
    {
        // Quay mặt vào nhau
        XoayMatVaoNhau(thachSanh);

        // Chuyển sang Camera tĩnh
        if (mainCamera != null) mainCamera.SetActive(false);
        if (dialogueCamera != null) dialogueCamera.SetActive(true);

        isDialogueActive = true;

        ShowDialogueLine("LÝ THÔNG", "Hang sâu hun hút thế này... Em chịu khó buộc dây ngang lưng, để anh thả xuống trước thám thính nhé!", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        ShowDialogueLine("THẠCH SANH", "Anh cứ đưa dây đây, đệ không sợ đâu. Cứu được công chúa đệ sẽ giật dây để anh kéo lên!", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());

        ShowDialogueLine("MẸ LÝ THÔNG", "Con đi cẩn thận nhé! Mẹ và anh sẽ ở trên này giữ dây thật chặt chờ con.", colorMeLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Hiện dây thừng
        if (dayThung != null) dayThung.SetActive(true);
        yield return new WaitForSeconds(1.0f);

        GameObject dayHang = GameObject.Find("DiemDen_DayHang");

        if (thachSanh != null && dayHang != null)
        {
            // Trả lại Camera chính để nhìn Thạch Sanh trượt xuống
            if (mainCamera != null) mainCamera.SetActive(true);
            if (dialogueCamera != null) dialogueCamera.SetActive(false);

            // Tạm tắt bộ va chạm để trượt
            CharacterController cc = thachSanh.GetComponent<CharacterController>();
            UnityEngine.AI.NavMeshAgent agent = thachSanh.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (cc != null) cc.enabled = false;
            if (agent != null) agent.enabled = false;

            // Xoay Thạch Sanh hướng mặt vào vách hang (hoặc hướng sợi dây) khi leo xuống
            thachSanh.transform.rotation = Quaternion.Euler(0, thachSanh.transform.rotation.eulerAngles.y, 0);

            // BẬT HOẠT ẢNH LEO DÂY
            Animator anim = thachSanh.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsClimbing", true);
            }

            Vector3 diemBatDau = thachSanh.transform.position;
            Vector3 diemKetThuc = dayHang.transform.position;

            // Bạn có thể chỉnh lại thời gian này cho khớp với tốc độ của hoạt ảnh leo dây
            float thoiGianLeo = 3.5f;
            float thoiGianDaTroiQua = 0f;

            // Quá trình trượt xuống
            while (thoiGianDaTroiQua < thoiGianLeo)
            {
                thachSanh.transform.position = Vector3.Lerp(diemBatDau, diemKetThuc, thoiGianDaTroiQua / thoiGianLeo);
                thoiGianDaTroiQua += Time.deltaTime;
                yield return null;
            }

            // ==========================================
            // ĐÁP ĐẤT VÀ KHÔI PHỤC TRẠNG THÁI ĐIỀU KHIỂN
            // ==========================================

            // Chạm đất
            thachSanh.transform.position = diemKetThuc;

            // TẮT HOẠT ẢNH LEO DÂY VÀ TRỞ VỀ ĐỨNG YÊN
            if (anim != null)
            {
                anim.SetBool("IsClimbing", false);
            }

            // 1. Bật lại bộ va chạm vật lý
            if (cc != null) cc.enabled = true;

            // 2. Tiêu diệt AI tìm đường (NavMeshAgent) để không bị khóa chân
            if (agent != null) Destroy(agent);

            // 3. Bật lại quyền điều khiển bàn phím (Đổi "PlayerMovement" nếu file di chuyển của bạn tên khác)
            MonoBehaviour playerMovement = (MonoBehaviour)thachSanh.GetComponent("PlayerMovement");
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
        }
    }

    private void ShowDialogueLine(string speaker, string message, Color speakerColor)
    {
        currentDialogueSpeaker = speaker;
        currentDialogueMessage = message;
        currentDialogueColor = speakerColor;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null) { speakerText.text = speaker + ":"; speakerText.color = speakerColor; }
            if (contentText != null) { contentText.text = $"\"{message}\""; }
            if (hintText != null) { hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)"; }
        }
    }

    void OnGUI()
    {
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy) return;

        if (isDialogueActive && !string.IsNullOrEmpty(currentDialogueMessage))
        {
            GUI.depth = -100;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = GetBoxBackground();
            boxStyle.wordWrap = true;
            boxStyle.alignment = TextAnchor.UpperLeft;

            boxStyle.padding = new RectOffset(15, 15, 8, 8);
            boxStyle.richText = true;

            float width = Mathf.Min(Screen.width * 0.45f, 600f);
            float height = 110f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - height - 15f;

            string colorHex = ColorUtility.ToHtmlStringRGB(currentDialogueColor);

            string fullMsg = $"<size=20><b><color=#{colorHex}>{currentDialogueSpeaker}:</color></b></size>\n" +
                             $"<size=16><color=#FFFFFF>\"{currentDialogueMessage}\"</color></size>\n\n" +
                             $"<color=#FFD700><size=13><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";

            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
    }
}