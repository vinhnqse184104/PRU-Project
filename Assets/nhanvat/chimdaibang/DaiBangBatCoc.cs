using System.Collections;
using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DaiBangBatCoc : MonoBehaviour
{
    [Header("M4 — Tiến trình sau cutscene")]
    public bool continueToTracking = true;
    public string trackingSceneName = "Scene_M4_Chapter5_Tracking";
    public UnityEngine.Events.UnityEvent onKidnappingCompleted = new UnityEngine.Events.UnityEvent();
    public bool IsCompleted { get; private set; }
    public string transitionError { get; private set; }
    [Header("Nhân vật tham gia")]
    public Transform daiBang;
    public Transform congChua;

    [Header("Đường bay")]
    public Transform diemBayRa;
    public Transform diemBayDi;

    [Header("Tốc độ")]
    public float tocDoLaoXuong = 25f;
    public float tocDoBayDi = 15f;

    [Header("Camera Điện Ảnh")]
    public Camera mainCamera;
    public Transform gocQuayMoi;

    [Header("===== MÀN HÌNH ĐEN INTRO =====")]
    public GameObject blackIntroPanel;
    public TextMeshProUGUI introText;
    [TextArea(2, 4)]
    public string noiDungIntro = "Vài ngày sau, tại Hoàng cung...\nNhà vua, Công chúa và Quận công Lý Thông đang vãn cảnh...";

    [Header("===== CÀI ĐẶT UI HỘI THOẠI =====")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;

    [System.Serializable]
    public struct DongThoai
    {
        public string tenNhanVat;
        [TextArea(2, 4)] public string noiDung;
        public Color mauChu;
    }

    [Header("Kịch bản Hội Thoại")]
    public DongThoai[] thoaiTruocKhiBat;
    public DongThoai[] thoaiSauKhiBat;

    void Start()
    {
        if (daiBang == null || congChua == null || diemBayRa == null || diemBayDi == null)
        {
            transitionError = "Thiếu Đại Bàng, Công Chúa hoặc điểm bay trong Inspector.";
            Debug.LogError("M4_CHAPTER5: " + transitionError);
            return;
        }
        if (daiBang != null) daiBang.gameObject.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (blackIntroPanel != null) blackIntroPanel.SetActive(false);

        StartCoroutine(LuongKichBanChinh());
    }

    private IEnumerator LuongKichBanChinh()
    {
        // GIAI ĐOẠN 0: Màn hình đen kể chuyện (Intro)
        if (blackIntroPanel != null && introText != null)
        {
            blackIntroPanel.SetActive(true);
            introText.text = noiDungIntro;
            yield return StartCoroutine(DoiNguoiChoiBamPhim());
            blackIntroPanel.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        // GIAI ĐOẠN 1: Thoại trước khi bắt
        yield return StartCoroutine(ChayHoiThoai(thoaiTruocKhiBat));

        // GIAI ĐOẠN 2: Chim đại bàng lao xuống
        yield return StartCoroutine(KichBanBatCoc());

        // GIAI ĐOẠN 3: Thoại sau khi bị bắt
        yield return StartCoroutine(ChayHoiThoai(thoaiSauKhiBat));
        IsCompleted = true;
        onKidnappingCompleted.Invoke();
        if (continueToTracking)
        {
            yield return new WaitForSeconds(1.5f);
            if (Application.CanStreamedLevelBeLoaded(trackingSceneName)) UnityEngine.SceneManagement.SceneManager.LoadScene(trackingSceneName);
            else transitionError = "Chưa có scene truy đuổi trong Build Settings: " + trackingSceneName;
        }
    }

    private IEnumerator ChayHoiThoai(DongThoai[] danhSachThoai)
    {
        if (danhSachThoai == null || danhSachThoai.Length == 0) yield break;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        foreach (var cau in danhSachThoai)
        {
            if (speakerText != null)
            {
                // Nếu tên nhân vật là "DẪN TRUYỆN", ta ẩn đi cho giống tiểu thuyết
                if (cau.tenNhanVat == "DẪN TRUYỆN")
                {
                    speakerText.text = "";
                }
                else
                {
                    speakerText.text = cau.tenNhanVat + ":";
                    Color speakerColor = cau.mauChu;
                    speakerColor.a = 1f;
                    speakerText.color = speakerColor;
                }
            }
            if (contentText != null) contentText.text = "\"" + cau.noiDung + "\"";

            yield return StartCoroutine(DoiNguoiChoiBamPhim());
        }
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private IEnumerator DoiNguoiChoiBamPhim()
    {
        yield return new WaitForSeconds(0.2f);
        bool daBam = false;
        while (!daBam)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) daBam = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) daBam = true;
#endif
            try
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    daBam = true;
            }
            catch { }
            yield return null;
        }
    }

    private IEnumerator KichBanBatCoc()
    {
        daiBang.position = diemBayRa.position;
        daiBang.gameObject.SetActive(true);

        while (Vector3.Distance(daiBang.position, congChua.position) > 1.5f)
        {
            daiBang.LookAt(congChua.position);
            daiBang.position = Vector3.MoveTowards(daiBang.position, congChua.position, tocDoLaoXuong * Time.deltaTime);
            yield return null;
        }

        congChua.SetParent(daiBang);
        congChua.localPosition = new Vector3(0f, -0.8f, 0f);
        congChua.localRotation = Quaternion.Euler(-45f, 0f, 0f);

        if (mainCamera != null && gocQuayMoi != null)
        {
            MonoBehaviour camFollow = mainCamera.GetComponent("CameraFollow") as MonoBehaviour;
            if (camFollow != null) camFollow.enabled = false;

            mainCamera.transform.position = gocQuayMoi.position;
            mainCamera.transform.rotation = gocQuayMoi.rotation;
        }

        yield return new WaitForSeconds(0.5f);

        while (Vector3.Distance(daiBang.position, diemBayDi.position) > 2f)
        {
            daiBang.LookAt(diemBayDi.position);
            daiBang.position = Vector3.MoveTowards(daiBang.position, diemBayDi.position, tocDoBayDi * Time.deltaTime);
            yield return null;
        }
        daiBang.gameObject.SetActive(false);
    }

    void OnGUI()
    {
        PRU.M4.M4QuestDrawing.Objective("CHƯƠNG 5 · ĐẠI BÀNG BẮT CÔNG CHÚA",
            string.IsNullOrEmpty(transitionError) ? (IsCompleted ? "Thạch Sanh lên đường theo dấu Đại Bàng…" : "Theo dõi câu chuyện tại hoàng cung.") : transitionError,
            "Tiếp theo: bắn cung → theo dấu → tìm cửa hang", "Enter / Space / Chuột trái: tiếp tục hội thoại");
    }
}
