using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EagleBossAI : MonoBehaviour
{
    [Header("Chỉ số Boss")]
    public int maxHp = 100;
    private int currentHp;
    public float tocDoBay = 3f;

    [Header("Độ cao bay (MỚI)")]
    public float doCaoBayTrenTroi = 3f;
    public float tocDoSaXuong = 7f;

    [Header("Hiệu ứng Đẩy lùi (Bị chém)")]
    public float lucDayLui = 6f;
    public float thoiGianDayLui = 0.2f;
    private bool dangBiDay = false;

    [Header("UI Thanh Máu & Hướng dẫn")]
    public Slider thanhMauBoss;
    public TextMeshProUGUI txtNhiemVu;

    [Header("Bay & Nghiêng Cánh")]
    public float tocDoXoay = 4f;
    public float doNghiengToiDa = 45f;
    public float heSoNghieng = 1.5f;

    [Header("Tầm nhìn & Tầm đánh")]
    public float tamPhatHien = 15f;
    public float tamDanh = 2.5f;
    public int lucDanhCuaBoss = 15;

    [Header("Tuần tra (Đi dạo)")]
    public float banKinhTuanTra = 7f;
    private Vector3 diemXuatPhat;
    private Vector3 diemDenHienTai;
    private bool dangChoDoi = false;

    [Header("Tấn công")]
    public float thoiGianHoiDon = 2f;
    private float thoiGianDanhTiepTheo = 0f;
    private string[] danhSachDonDanh = { "DoChem1", "DoChem2", "DoChem3", "DoDa" };

    // ==========================================
    // KHAI BÁO ÂM THANH (MỚI THÊM)
    // ==========================================
    [Header("Âm thanh Boss")]
    public AudioSource loaBoss;      // Loa gắn trên Đại Bàng
    public AudioClip tiengBossDanh;  // File âm thanh tiếng rít/tiếng cào
    public AudioClip tiengBiThuong;  // [THÊM DÒNG NÀY]: Tiếng kêu la khi bị chém trúng
    // ==========================================

    [Header("Âm thanh Bay")]
    public AudioSource loaBay;       // Loa thứ 2 gắn trên Đại Bàng
    public AudioClip tiengBay;       // File tiếng vỗ cánh phành phạch

    [Header("Liên kết")]
    public Transform thachSanh;
    private Animator anim;
    private bool isDead = false;

    void Start()
    {
        currentHp = maxHp;
        anim = GetComponent<Animator>();

        if (thanhMauBoss != null)
        {
            thanhMauBoss.maxValue = maxHp;
            thanhMauBoss.value = currentHp;
            thanhMauBoss.gameObject.SetActive(false);
        }

        if (thachSanh == null)
        {
            GameObject ts = GameObject.Find("thachsanh");
            if (ts != null) thachSanh = ts.transform;
        }

        if (loaBay != null && tiengBay != null)
        {
            loaBay.clip = tiengBay;
            loaBay.loop = true; // Code tự động đánh dấu Loop giúp bạn
            loaBay.Play();
        }

        diemXuatPhat = transform.position;
        TimDiemTuanTraMoi();
    }

    private void HienThanhMauBoss()
    {
        if (thanhMauBoss != null && !thanhMauBoss.gameObject.activeSelf)
        {
            thanhMauBoss.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (isDead || thachSanh == null || dangBiDay) return;

        float khoangCach2D = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(thachSanh.position.x, thachSanh.position.z));

        if (khoangCach2D <= tamDanh)
        {
            HienThanhMauBoss();
            QuayMatVaNghieng(thachSanh.position);

            Vector3 viTriDuoiDat = new Vector3(transform.position.x, thachSanh.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, viTriDuoiDat, tocDoSaXuong * Time.deltaTime);

            if (Mathf.Abs(transform.position.y - thachSanh.position.y) <= 1.5f)
            {
                if (Time.time >= thoiGianDanhTiepTheo)
                {
                    TungDonNgauNhien();
                    thoiGianDanhTiepTheo = Time.time + thoiGianHoiDon;
                }
            }
        }
        else if (khoangCach2D <= tamPhatHien)
        {
            HienThanhMauBoss();
            QuayMatVaNghieng(thachSanh.position);

            Vector3 viTriTrenTroi = new Vector3(thachSanh.position.x, thachSanh.position.y + doCaoBayTrenTroi, thachSanh.position.z);
            transform.position = Vector3.MoveTowards(transform.position, viTriTrenTroi, tocDoBay * Time.deltaTime);
        }
        else
        {
            DiTuanTra();
        }
    }

    private void DiTuanTra()
    {
        if (dangChoDoi) return;
        QuayMatVaNghieng(diemDenHienTai);
        transform.position = Vector3.MoveTowards(transform.position, diemDenHienTai, (tocDoBay * 0.7f) * Time.deltaTime);

        float khoangCachToiDiemDen = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(diemDenHienTai.x, diemDenHienTai.z));
        if (khoangCachToiDiemDen < 0.5f)
        {
            StartCoroutine(ChoDoiTruocKhiBayTiep());
        }
    }

    private IEnumerator ChoDoiTruocKhiBayTiep()
    {
        dangChoDoi = true;
        float thoiGianCho = Random.Range(1f, 3f);
        float thoiGianDaQua = 0f;
        while (thoiGianDaQua < thoiGianCho && !dangBiDay)
        {
            QuayMatVaNghieng(transform.position + transform.forward);
            thoiGianDaQua += Time.deltaTime;
            yield return null;
        }
        TimDiemTuanTraMoi();
    }

    private void TimDiemTuanTraMoi()
    {
        Vector2 diemNgauNhien2D = Random.insideUnitCircle * banKinhTuanTra;
        float yTrenCao = thachSanh != null ? thachSanh.position.y + doCaoBayTrenTroi : diemXuatPhat.y + doCaoBayTrenTroi;
        diemDenHienTai = new Vector3(diemXuatPhat.x + diemNgauNhien2D.x, yTrenCao, diemXuatPhat.z + diemNgauNhien2D.y);
        dangChoDoi = false;
    }

    private void QuayMatVaNghieng(Vector3 mucTieu)
    {
        Vector3 huongBay = mucTieu - transform.position;
        huongBay.y = 0;
        if (huongBay.sqrMagnitude > 0.01f)
        {
            Quaternion gocQuayPhang = Quaternion.LookRotation(huongBay);
            float gocRe = Vector3.SignedAngle(transform.forward, huongBay, Vector3.up);
            float gocNghiengThucTe = Mathf.Clamp(gocRe * heSoNghieng, -doNghiengToiDa, doNghiengToiDa);
            Quaternion gocQuayHoanChinh = gocQuayPhang * Quaternion.Euler(0, 0, -gocNghiengThucTe);
            transform.rotation = Quaternion.Slerp(transform.rotation, gocQuayHoanChinh, tocDoXoay * Time.deltaTime);
        }
    }

    private void TungDonNgauNhien()
    {
        int indexNgauNhien = Random.Range(0, danhSachDonDanh.Length);
        anim.SetTrigger(danhSachDonDanh[indexNgauNhien]);
    }

    // ĐÂY LÀ HÀM ĐƯỢC ANIMATION GỌI KHI ĐẠI BÀNG ĐÁNH XUỐNG
    public void QuaiVatDanh()
    {
        if (isDead || thachSanh == null) return;

        // ==========================================
        // PHÁT ÂM THANH TẤN CÔNG (MỚI THÊM)
        // ==========================================
        if (loaBoss != null && tiengBossDanh != null)
        {
            loaBoss.PlayOneShot(tiengBossDanh);
        }
        // ==========================================

        float khoangCach = Vector3.Distance(transform.position, thachSanh.position);
        if (khoangCach <= tamDanh + 1.5f)
        {
            PlayerHealth tsHealth = thachSanh.GetComponent<PlayerHealth>();
            if (tsHealth != null)
            {
                tsHealth.NhanSatThuong(lucDanhCuaBoss, transform);
            }
        }
    }

    public void TakeDamage(int luongSatThuong, Transform keTanCong = null)
    {
        if (isDead) return;

        HienThanhMauBoss();

        // ==========================================
        // PHÁT ÂM THANH BỊ THƯƠNG (MỚI THÊM)
        // ==========================================
        if (loaBoss != null && tiengBiThuong != null)
        {
            loaBoss.PlayOneShot(tiengBiThuong);
        }
        // ==========================================

        currentHp -= luongSatThuong;
        if (thanhMauBoss != null) thanhMauBoss.value = currentHp;

        if (currentHp > 0)
        {
            if (anim != null) anim.SetTrigger("BiThuong");

            if (keTanCong != null && !dangBiDay)
            {
                StartCoroutine(HieuUngDayLui(keTanCong));
            }
        }
        else
        {
            Chet();
        }
    }

    private IEnumerator HieuUngDayLui(Transform keTanCong)
    {
        dangBiDay = true;
        Vector3 huongLui = (transform.position - keTanCong.position).normalized;
        huongLui.y = 0;

        float timer = 0;
        while (timer < thoiGianDayLui)
        {
            transform.position += huongLui * lucDayLui * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
        dangBiDay = false;
    }

    private void Chet()
    {
        isDead = true;
        StopAllCoroutines();

        if (anim != null) anim.SetTrigger("Die");
        if (thanhMauBoss != null) thanhMauBoss.gameObject.SetActive(false);

        if (txtNhiemVu != null)
        {
            txtNhiemVu.gameObject.SetActive(true);
            txtNhiemVu.text = "Nhiệm vụ: Đi tìm Công chúa và bế đến chỗ sợi dây thừng.";
        }

        Collider coll = GetComponent<Collider>();
        if (coll != null) coll.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (loaBay != null) loaBay.Stop();

        StartCoroutine(XacRoiXuongDat());
    }

    private IEnumerator XacRoiXuongDat()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f))
        {
            Vector3 diemChamDat = hit.point;

            while (transform.position.y > diemChamDat.y + 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemChamDat, 5f * Time.deltaTime);
                yield return null;
            }
        }
    }
}