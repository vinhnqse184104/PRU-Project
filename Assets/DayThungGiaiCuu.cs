using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DayThungGiaiCuu : MonoBehaviour
{
    [Header("UI Hội thoại Canvas")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI hintText;

    [Header("Cài đặt Chuyển cảnh")]
    public GameObject congChuaTrenNguoi;
    public GameObject congChuaBuocVaoDay;
    public GameObject dayThung; // Kéo sợi dây thừng vào đây để Lý Thông "cắt"

    private bool isDialogueActive = false;
    private bool hasTriggered = false;
    private Texture2D boxBgTexture;

    private Color colorThachSanh = new Color(0.3f, 0.8f, 1.0f);
    private Color colorLyThong = new Color(1.0f, 0.2f, 0.2f); // Đổi màu Lý Thông sang đỏ cho ác

    private string currentDialogueSpeaker = "";
    private string currentDialogueMessage = "";
    private Color currentDialogueColor = Color.white;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && CuuCongChua.daBeCongChua && !hasTriggered)
        {
            hasTriggered = true;
            DungNhanVat(other.gameObject);
            StartCoroutine(KichBanKeoLen(other.gameObject));
        }
    }

    private void DungNhanVat(GameObject nhanVat)
    {
        if (nhanVat == null) return;
        var agent = nhanVat.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;

        var anim = nhanVat.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetFloat("Speed", 0f);

        MonoBehaviour playerMovement = (MonoBehaviour)nhanVat.GetComponent("PlayerMovement");
        if (playerMovement != null) playerMovement.enabled = false;
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

    IEnumerator KichBanKeoLen(GameObject thachSanh)
    {
        // ==========================================
        // 1. THOẠI TRƯỚC KHI KÉO
        // ==========================================
        isDialogueActive = true;

        ShowDialogueLine("THẠCH SANH", "Anh Thông ơi! Đệ cứu được Công chúa rồi!", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());

        ShowDialogueLine("LÝ THÔNG", "Tốt lắm chú em! Em cột Công chúa vào dây để anh kéo lên trước, sau đó anh sẽ thả dây xuống kéo em lên sau nhé!", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());

        ShowDialogueLine("THẠCH SANH", "Dạ anh .", colorThachSanh);
        yield return StartCoroutine(WaitForKeyPress());

        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // ==========================================
        // 2. KÉO CÔNG CHÚA LÊN
        // ==========================================
        if (congChuaTrenNguoi != null) congChuaTrenNguoi.SetActive(false);
        if (congChuaBuocVaoDay != null)
        {
            congChuaBuocVaoDay.SetActive(true);

            Vector3 diemBatDauCongChua = congChuaBuocVaoDay.transform.position;
            Vector3 diemKetThucCongChua = new Vector3(diemBatDauCongChua.x, diemBatDauCongChua.y + 20f, diemBatDauCongChua.z);

            float thoiGianKeoCC = 3.5f;
            float thoiGianDaTroiQua = 0f;

            while (thoiGianDaTroiQua < thoiGianKeoCC)
            {
                congChuaBuocVaoDay.transform.position = Vector3.Lerp(diemBatDauCongChua, diemKetThucCongChua, thoiGianDaTroiQua / thoiGianKeoCC);
                thoiGianDaTroiQua += Time.deltaTime;
                yield return null;
            }
            congChuaBuocVaoDay.SetActive(false);
        }

        yield return new WaitForSeconds(1f); // Chờ 1 giây để Lý Thông thả dây lại

        // ==========================================
        // 3. THẠCH SANH LEO LÊN LƯNG CHỪNG
        // ==========================================
        CharacterController cc = thachSanh.GetComponent<CharacterController>();
        UnityEngine.AI.NavMeshAgent agent = thachSanh.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (cc != null) cc.enabled = false;
        if (agent != null) agent.enabled = false;

        Animator anim = thachSanh.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsClimbing", false);

            // ========================================================
            // ĐỔI LỆNH TẠI ĐÂY: Dùng CrossFade thay vì Play
            anim.CrossFade("LeoLen", 0.1f);
            // ========================================================

            anim.SetBool("IsClimbingUp", true);
        }

        Vector3 viTriDuoiDat = thachSanh.transform.position;
        Vector3 viTriLungChung = new Vector3(viTriDuoiDat.x, viTriDuoiDat.y + 8f, viTriDuoiDat.z);

        float thoiGianLeo = 6f;
        float t = 0f;
        while (t < thoiGianLeo)
        {
            thachSanh.transform.position = Vector3.Lerp(viTriDuoiDat, viTriLungChung, t / thoiGianLeo);
            t += Time.deltaTime;
            yield return null;
        }

        // ==========================================
        // 4. LÝ THÔNG LẬT MẶT & CẮT DÂY
        // ==========================================
        if (anim != null) anim.SetFloat("Speed", 0f);

        isDialogueActive = true;
        ShowDialogueLine("LÝ THÔNG", "Ha ha ha! Công chúa và vinh hoa phú quý cứ để anh hưởng! Mày ở lại dưới hang làm bạn với ma quỷ đi!", colorLyThong);
        yield return StartCoroutine(WaitForKeyPress());
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (dayThung != null) dayThung.SetActive(false);

        // ==========================================
        // 5. THẠCH SANH RƠI XUỐNG VÀ ÉP LƯNG CHẠM SÀN
        // ==========================================
        if (anim != null)
        {
            anim.SetBool("IsClimbingUp", false);
            anim.SetTrigger("TeXuong");
        }

        float thoiGianRot = 4.0f; // Thời gian rớt
        t = 0f;

        // Tọa độ ngã: Ép trục Y thấp xuống khoảng 1.5 mét để lưng chạm sát mặt đất
        Vector3 toaDoNga = new Vector3(viTriDuoiDat.x, viTriDuoiDat.y - 1.5f, viTriDuoiDat.z);

        while (t < thoiGianRot)
        {
            thachSanh.transform.position = Vector3.Lerp(viTriLungChung, toaDoNga, t / thoiGianRot);
            t += Time.deltaTime;
            yield return null;
        }

        // Khóa chặt vị trí nằm ngửa dưới đất
        thachSanh.transform.position = toaDoNga;

        // Chờ 2 giây để Thạch Sanh diễn cảnh nằm rên rỉ
        yield return new WaitForSeconds(60.0f);

        // ==========================================
        // QUAN TRỌNG: ÉP CHẶT XUỐNG MẶT ĐẤT BẰNG WARP
        // ==========================================
         agent = thachSanh.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            // Hút chặt Thạch Sanh vào lưới sàn đáy hang tại vị trí lúc chưa leo
            agent.Warp(viTriDuoiDat);
            agent.enabled = true;
        }

        // BẬT LẠI VA CHẠM VÀ ĐIỀU KHIỂN
         cc = thachSanh.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        MonoBehaviour playerMovement = (MonoBehaviour)thachSanh.GetComponent("PlayerMovement");
        if (playerMovement != null) playerMovement.enabled = true;

        if (anim != null) anim.SetFloat("Speed", 1f);

        Debug.Log("Sự kiện phản bội hoàn tất! Thạch Sanh đã đứng dậy, chuẩn bị mở nhiệm vụ tìm lối thoát...");
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