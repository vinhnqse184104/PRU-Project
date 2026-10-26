using UnityEngine;

public class ChanTinhAttack : MonoBehaviour
{
    [Header("Cài đặt đòn đánh chung")]
    [Tooltip("Khoảng cách đẩy vùng đánh ra trước mặt quái")]
    public float attackOffset = 1.0f;
    [Tooltip("Độ to của vòng tròn sát thương (Bao trọn cả tay và chân)")]
    public float attackRange = 2.0f;
    public int attackDamage = 20;

    [Header("Âm thanh Bị thương")]
    public AudioSource loaThachSanh;    // Kéo Audio Source của Thạch Sanh vào đây
    public AudioClip tiengBiThuong;     // Nạp file tiếng kêu đau của nhân vật

    // Hàm này dùng chung cho TẤT CẢ các đòn tay, chân, húc đầu...
    public void QuaiVatDanh()
    {
        // Tự động tạo một tâm sát thương nằm ngay TRƯỚC NGỰC Chằn Tinh
        Vector3 tamDanh = transform.position + transform.forward * attackOffset;
        tamDanh.y += 1f; // Nâng lên cao 1 mét cho ngang ngực người chơi

        // Quét tất cả mục tiêu trong vòng tròn to trước mặt
        Collider[] hitPlayers = Physics.OverlapSphere(tamDanh, attackRange);
        foreach (Collider player in hitPlayers)
        {
            if (player.CompareTag("Player"))
            {
                PlayerHealth mauTS = player.GetComponent<PlayerHealth>();
                if (mauTS != null)
                {
                    mauTS.TakeDamage(attackDamage, transform.position);
                }
            }
        }
    }

    // Tự động vẽ một quả cầu màu vàng chà bá trước mặt quái để bạn dễ nhìn
    private void OnDrawGizmosSelected()
    {
        Vector3 tamDanh = transform.position + transform.forward * attackOffset;
        tamDanh.y += 1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(tamDanh, attackRange);
    }

    public void TakeDamage(int luongSatThuong, Vector3 viTriBiDanh)
    {
        // 1. PHÁT TIẾNG KÊU ĐAU NGAY LẬP TỨC
        if (loaThachSanh != null && tiengBiThuong != null)
        {
            loaThachSanh.PlayOneShot(tiengBiThuong);
        }

        // 2. CÁC ĐOẠN CODE TRỪ MÁU CŨ CỦA BẠN GIỮ NGUYÊN BÊN DƯỚI
        // (Ví dụ: currentHealth -= luongSatThuong; ...)
    }
}