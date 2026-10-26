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

    [Header("Âm thanh Vũ khí")]
    public AudioSource loaVuKhi;
    public AudioClip tiengChemDa;
    public AudioClip tiengChemQuai; // Tiếng "phập" hoặc tiếng kêu của động vật
    public AudioClip tiengChatCay;  // Tiếng "cộc cộc" khi búa rìu đập vào gỗ

    [Header("Âm thanh Nhân vật")]
    public AudioSource loaThachSanh;         // Loa phát tiếng của Thạch Sanh
    public AudioClip tiengThachSanhKeu;      // File âm thanh tiếng "Á", "Hự"

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

    // GOM CHUNG ÂM THANH VÀO HÀM NÀY ĐỂ TẤT CẢ QUÁI/VẬT ĐÁNH ĐỀU KÊU
    public void TakeDamage(int damage, Transform keTanCong = null)
    {
        if (isDead) return; // Nếu đã chết thì không nhận thêm sát thương

        // ==========================================
        // 1. PHÁT TIẾNG KÊU ĐAU KHI BỊ ĐÁNH
        // ==========================================
        if (loaThachSanh != null && tiengThachSanhKeu != null)
        {
            loaThachSanh.PlayOneShot(tiengThachSanhKeu);
        }

        // ==========================================
        // 2. XỬ LÝ TRỪ MÁU VÀ ĐẨY LÙI
        // ==========================================
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

        // 1. Tắt kịch bản di chuyển
        MonoBehaviour playerMovement = (MonoBehaviour)GetComponent("PlayerMovement");
        if (playerMovement != null) playerMovement.enabled = false;

        // 2. Tắt TẤT CẢ các loại vỏ bọc va chạm có thể cản trở việc nằm bẹp
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Collider col = GetComponent<Collider>(); // Tắt luôn Capsule Collider nếu có
        if (col != null) col.enabled = false;

        // 3. Đóng băng trọng lực để xác không bị rơi lọt thỏm xuyên qua bản đồ 
        // khi đã tắt hết va chạm ở bước 2
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 4. Gọi màn hình thua hiện ra sau 2.5 giây
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

    // Giữ nguyên hàm này để lỡ các script cũ đang gọi tới không bị lỗi
    public void NhanSatThuong(int damage, Transform keTanCong = null)
    {
        // Chuyển hướng xử lý sang hàm gốc
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
        if (isDead) return;

        // ==========================================
        // 1. TÌM VÀ CHÉM QUÁI VẬT (Gà, Hươu, Chằn Tinh...)
        // ==========================================
        GameObject[] quaiVat = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject quai in quaiVat)
        {
            float khoangCach = Vector3.Distance(transform.position, quai.transform.position);
            if (khoangCach <= tamChem)
            {
                // PHÁT ÂM THANH CHÉM QUÁI
                if (loaVuKhi != null && tiengChemQuai != null)
                {
                    loaVuKhi.PlayOneShot(tiengChemQuai);
                }

                EagleBossAI bossAI = quai.GetComponent<EagleBossAI>();
                if (bossAI != null) bossAI.TakeDamage(satThuongCuaThachSanh, transform);
                else quai.SendMessage("TakeDamage", satThuongCuaThachSanh, SendMessageOptions.DontRequireReceiver);
            }
        }

        // ==========================================
        // 2. TÌM VÀ CHẶT CÂY
        // ==========================================
        GameObject[] cacCay = GameObject.FindGameObjectsWithTag("Tree");
        foreach (GameObject cay in cacCay)
        {
            float khoangCachCay = Vector3.Distance(transform.position, cay.transform.position);
            if (khoangCachCay <= tamChem)
            {
                // PHÁT ÂM THANH CHẶT CÂY
                if (loaVuKhi != null && tiengChatCay != null)
                {
                    loaVuKhi.PlayOneShot(tiengChatCay);
                }

                cay.SendMessage("TakeDamage", satThuongCuaThachSanh, SendMessageOptions.DontRequireReceiver);
                cay.SendMessage("BiChat", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}