using System.Collections;
using UnityEngine;

public class LyThongInteraction : MonoBehaviour
{
    public QuestManager questManager;
    private Animator anim;
    private bool isMet = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Khi Thạch Sanh bước vào vùng 3 mét
        if (other.CompareTag("Player") && !isMet)
        {
            isMet = true;

            // Bắt đầu chuỗi kịch bản thức dậy
            StartCoroutine(WakeUpSequence());
        }
    }

    // Chuỗi sự kiện thức dậy
    IEnumerator WakeUpSequence()
    {
        // 1. Chờ đúng 5 giây khi Thạch Sanh lại gần
        yield return new WaitForSeconds(5f);

        // 2. Kích hoạt hoạt ảnh đứng dậy
        if (anim != null)
        {
            anim.SetTrigger("StandUp");
        }

        // 3. Chờ thêm 2.5 giây để Lý Thông đứng thẳng người lên (cho khớp hoạt ảnh)
        yield return new WaitForSeconds(2.5f);

        // 4. Xong xuôi hết mới bắt đầu hiện chữ hội thoại
        if (questManager != null)
        {
            questManager.MeetLyThong();
        }
    }
}