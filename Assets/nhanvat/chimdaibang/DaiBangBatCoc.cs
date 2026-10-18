using System.Collections;
using UnityEngine;
using TMPro;

public class DaiBangBatCoc : MonoBehaviour
{
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
    }

    private IEnumerator ChayHoiThoai(DongThoai[] danhSachThoai)
    {
        if (danhSachThoai == null || danhSachThoai.Length == 0) yield break;

        dialoguePanel.SetActive(true);
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
                    speakerText.color = cau.mauChu;
                }
            }
            if (contentText != null) contentText.text = "\"" + cau.noiDung + "\"";

            yield return StartCoroutine(DoiNguoiChoiBamPhim());
        }
        dialoguePanel.SetActive(false);
    }

    private IEnumerator DoiNguoiChoiBamPhim()
    {
        yield return new WaitForSeconds(0.2f);
        bool daBam = false;
        while (!daBam)
        {
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
}