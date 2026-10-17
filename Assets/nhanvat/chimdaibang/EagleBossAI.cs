using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EagleBossAI : MonoBehaviour
{
    [Header("Chỉ số Boss")]
    public int maxHp = 100;
    private int currentHp;
    public float tocDoBay = 4f;

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
    public float banKinhTuanTra = 10f;
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
            // Ẩn thanh máu của Boss khi vừa vào game
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

    // Hàm phụ trợ để gọi thanh máu ra
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

        float khoangCach = Vector3.Distance(transform.position, thachSanh.position);

        if (khoangCach <= tamDanh)
        {
            HienThanhMauBoss(); // Thấy Thạch Sanh lại gần là hiện thanh máu
            QuayMatVaNghieng(thachSanh.position);

            if (Time.time >= thoiGianDanhTiepTheo)
            {
                TungDonNgauNhien();
                thoiGianDanhTiepTheo = Time.time + thoiGianHoiDon;
            }
        }
        else if (khoangCach <= tamPhatHien)
        {
            HienThanhMauBoss(); // Bắt đầu rượt đuổi cũng hiện thanh máu
            QuayMatVaNghieng(thachSanh.position);
            transform.position = Vector3.MoveTowards(transform.position, thachSanh.position, tocDoBay * Time.deltaTime);
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
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(diemDenHienTai.x, 0, diemDenHienTai.z)) < 0.5f)
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
        diemDenHienTai = new Vector3(diemXuatPhat.x + diemNgauNhien2D.x, diemXuatPhat.y, diemXuatPhat.z + diemNgauNhien2D.y);
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

        float khoangCach = Vector3.Distance(transform.position, thachSanh.position);
        if (khoangCach <= tamDanh + 0.5f)
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

        HienThanhMauBoss(); // Nếu bị Thạch Sanh chém trúng thì chắc chắn phải hiện thanh máu

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

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.down * 10f;

        // Ẩn thanh máu đi khi Boss đã chết
        if (thanhMauBoss != null) thanhMauBoss.gameObject.SetActive(false);
    }
}