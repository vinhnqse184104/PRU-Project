using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Chỉ số Thạch Sanh")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Chỉ số Đẩy lùi")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    private bool dangBiDay = false;

    [Header("UI Thanh Máu")]
    public Slider healthBar;

    [Header("Chỉ số Chém Đại Bàng (Mới)")]
    public int satThuongCuaThachSanh = 25;
    public float tamChem = 3f;

    // THÊM DÒNG NÀY ĐỂ ĐIỀU KHIỂN HOẠT ẢNH
    private Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        // Tự động tìm Animator trên người Thạch Sanh
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(int damage, Transform keTanCong = null)
    {
        currentHealth -= damage;
        if (healthBar != null) healthBar.value = currentHealth;

        Debug.Log("Thạch Sanh bị đánh! Máu còn: " + currentHealth);

        if (currentHealth > 0)
        {
            if (anim != null) anim.SetTrigger("BiThuong");

            if (keTanCong != null && !dangBiDay)
            {
                StartCoroutine(HieuUngDayLui(keTanCong));
            }
        }
        else
        {
            // THẠCH SANH HẾT MÁU -> KÍCH HOẠT HOẠT ẢNH CHẾT
            Debug.Log("Thạch Sanh đã hi sinh! GAME OVER.");
            if (anim != null) anim.SetTrigger("Die");

            // Tùy chọn: Tắt kịch bản di chuyển để người chơi không điều khiển được xác chết
            // GetComponent<PlayerMovement>().enabled = false; 
        }
    }

    public void TakeDamage<T>(int damage, T thamSoBoSung)
    {
        if (thamSoBoSung is Transform) TakeDamage(damage, thamSoBoSung as Transform);
        else TakeDamage(damage, null);
    }

    public void NhanSatThuong(int damage, Transform keTanCong = null)
    {
        TakeDamage(damage, keTanCong);
    }

    private IEnumerator HieuUngDayLui(Transform keTanCong)
    {
        dangBiDay = true;
        Vector3 huongLui = (transform.position - keTanCong.position).normalized;
        huongLui.y = 0;

        float timer = 0;
        while (timer < knockbackDuration)
        {
            transform.position += huongLui * knockbackForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
        dangBiDay = false;
    }

    // ----- THẠCH SANH CHÉM ĐẠI BÀNG (CÓ MÁY DÒ LỖI) -----
    // ----- THẠCH SANH CHÉM MỌI LOẠI QUÁI -----
    public void ThachSanhChemTrug()
    {
        GameObject[] quaiVat = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject quai in quaiVat)
        {
            float khoangCach = Vector3.Distance(transform.position, quai.transform.position);

            if (khoangCach <= tamChem)
            {
                // Kiểm tra xem quái vật này có phải là Đại Bàng không
                EagleBossAI bossAI = quai.GetComponent<EagleBossAI>();

                if (bossAI != null)
                {
                    // Nếu LÀ Đại Bàng -> Dùng code có đẩy lùi của Đại Bàng
                    bossAI.TakeDamage(satThuongCuaThachSanh, transform);
                }
                else
                {
                    // Nếu KHÔNG PHẢI Đại Bàng (tức là Chằn Tinh) 
                    // -> Dùng lệnh SendMessage để tự động gọi hàm trừ máu cũ của Chằn Tinh
                    quai.SendMessage("TakeDamage", satThuongCuaThachSanh, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}