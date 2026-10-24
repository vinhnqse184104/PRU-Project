using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EagleBossAI : MonoBehaviour
{
    [Header("Chỉ số Boss")]
    public int maxHp = 100;
    private int currentHp;
    public float tocDoBay = 3f;

    [Header("Độ cao bay (MỚI)")]
    public float doCaoBayTrenTroi = 3f; // Tự động bay cao hơn mặt đất 3 mét
    public float tocDoSaXuong = 7f;    // Tốc độ lao nhanh xuống đất khi vồ mồi

    [Header("Hiệu ứng Đẩy lùi (Bị chém)")]
    public float lucDayLui = 6f;
    public float thoiGianDayLui = 0.2f;
    private bool dangBiDay = false;

    [Header("UI Thanh Máu")]
    public Slider thanhMauBoss;

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

        // MỚI: Tính khoảng cách theo mặt phẳng ngang 2D (Chỉ lấy trục X và Z)
        // Việc này giúp chim bay tít trên cao vẫn nhận diện được Thạch Sanh đang ở ngay dưới chân mình
        float khoangCach2D = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(thachSanh.position.x, thachSanh.position.z));

        if (khoangCach2D <= tamDanh)
        {
            HienThanhMauBoss();
            QuayMatVaNghieng(thachSanh.position);

            // Chim sà xuống mặt đất để chuẩn bị đánh (Lấy độ cao Y bằng với Thạch Sanh)
            Vector3 viTriDuoiDat = new Vector3(transform.position.x, thachSanh.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, viTriDuoiDat, tocDoSaXuong * Time.deltaTime);

            // Chờ chim hạ độ cao xuống gần chạm đất mới bắt đầu vung vuốt
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

            // Bay cao trên trời lượn theo Thạch Sanh
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
        // Điểm đến tuần tra luôn giữ ở trên cao
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

    public void QuaiVatDanh()
    {
        if (isDead || thachSanh == null) return;

        // Nới rộng tầm đánh một chút để dự phòng lúc chim đang sà xuống mà hoạt ảnh đã vung vuốt
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
        StopAllCoroutines(); // Dừng mọi hành động bay lượn, tuần tra

        if (anim != null) anim.SetTrigger("Die");
        if (thanhMauBoss != null) thanhMauBoss.gameObject.SetActive(false);

        // 1. Tắt va chạm vật lý cứng, biến thành Trigger để xác nằm trên đất 
        // nhưng Thạch Sanh có thể đi xuyên qua mà không bị kẹt
        Collider coll = GetComponent<Collider>();
        if (coll != null) coll.isTrigger = true;

        // 2. Hủy bỏ hoàn toàn Rigidbody (nếu có) để tránh lỗi rơi tự do lọt map
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // 3. Nếu chim chết lúc đang bay trên cao, cho nó rơi từ từ xuống đất
        StartCoroutine(XacRoiXuongDat());
    }

    private IEnumerator XacRoiXuongDat()
    {
        // Bắn 1 tia laser ảo từ bụng chim xuống đất để đo khoảng cách
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f))
        {
            Vector3 diemChamDat = hit.point;

            // Kéo cái xác từ từ rơi xuống điểm chạm đất
            while (transform.position.y > diemChamDat.y + 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemChamDat, 5f * Time.deltaTime);
                yield return null;
            }
        }
    }
}