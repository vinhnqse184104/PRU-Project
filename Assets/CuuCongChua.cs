using UnityEngine;
using TMPro; // BẮT BUỘC PHẢI THÊM DÒNG NÀY ĐỂ DÙNG UI TEXT

public class CuuCongChua : MonoBehaviour
{
    [Header("Cài đặt Bế Công Chúa")]
    public GameObject congChuaTrenDat;
    public GameObject congChuaTrenNguoi;

    [Header("Kích hoạt lối về")]
    public GameObject vungGiaiCuuDayThung;

    [Header("UI Nhiệm vụ cần tắt")]
    public TextMeshProUGUI txtNhiemVu; // THÊM BIẾN NÀY ĐỂ KÉO CHỮ VÀO

    public static bool daBeCongChua = false;

    void Start()
    {
        // Reset lại biến khi bắt đầu game
        daBeCongChua = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !daBeCongChua)
        {
            daBeCongChua = true;

            if (congChuaTrenDat != null) congChuaTrenDat.SetActive(false);
            if (congChuaTrenNguoi != null) congChuaTrenNguoi.SetActive(true);
            if (vungGiaiCuuDayThung != null) vungGiaiCuuDayThung.SetActive(true);

            // =====================================
            // TẮT DÒNG CHỮ NHIỆM VỤ ĐI
            // =====================================
            if (txtNhiemVu != null)
            {
                txtNhiemVu.gameObject.SetActive(false);
                // Hoặc nếu bạn muốn đổi chữ thay vì tắt luôn thì dùng:
                // txtNhiemVu.text = "Nhiệm vụ: Đưa Công chúa ra khỏi hang.";
            }
        }
    }
}