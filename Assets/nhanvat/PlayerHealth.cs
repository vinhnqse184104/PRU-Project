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

    [Header("Quản lý Game (THÊM MỚI)")]
    public QuanLyGame quanLyGame; // Kéo GameManager vào đây

    private Animator anim;
    private bool isDead = false; // Ngăn chặn việc gọi hàm chết nhiều lần

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        anim = GetComponent<Animator>();
    }

    public void TakeDamage(int damage, Transform keTanCong = null)
    {
        if (isDead) return; // Nếu đã chết thì không nhận thêm sát thương

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
            ThachSanhChet();
        }
    }

    private void ThachSanhChet()
    {
        isDead = true;
        Debug.Log("Thạch Sanh đã hi sinh! GAME OVER.");

        if (anim != null) anim.SetTrigger("Die");

        // Tắt bộ điều khiển để không trượt đi được nữa
        MonoBehaviour playerMovement = (MonoBehaviour)GetComponent("PlayerMovement");
        if (playerMovement != null) playerMovement.enabled = false;

        // Gọi màn hình thua hiện ra sau 2.5 giây (chờ chiếu xong hoạt ảnh ngã)
        if (quanLyGame != null)
        {
            quanLyGame.Invoke("HienThiManHinhThua", 2.5f);
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

    public void ThachSanhChemTrug()
    {
        if (isDead) return; // Không cho chém khi đã chết

        GameObject[] quaiVat = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject quai in quaiVat)
        {
            float khoangCach = Vector3.Distance(transform.position, quai.transform.position);

            if (khoangCach <= tamChem)
            {
                EagleBossAI bossAI = quai.GetComponent<EagleBossAI>();

                if (bossAI != null)
                {
                    bossAI.TakeDamage(satThuongCuaThachSanh, transform);
                }
                else
                {
                    quai.SendMessage("TakeDamage", satThuongCuaThachSanh, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}