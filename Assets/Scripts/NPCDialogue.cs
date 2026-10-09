using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    public Transform player;
    public float talkRange = 3f;
    public GameObject hintUI;   // chữ "Nhấn E để nói chuyện"

    public DialogueLine[] lines = new DialogueLine[]
    {
        new DialogueLine { speaker = "Lý Thông", text = "Thạch Sanh à, em ở với anh bấy lâu, anh coi em như ruột thịt." },
        new DialogueLine { speaker = "Thạch Sanh", text = "Anh Thông cứ nói, em nghe đây." },
        new DialogueLine { speaker = "Lý Thông", text = "Tối nay đến lượt anh canh miếu thờ. Anh trót đau bụng, em đi thay anh một đêm được không?" },
        new DialogueLine { speaker = "Thạch Sanh", text = "Được ạ, anh cứ yên tâm nghỉ ngơi. Em đi ngay đây." },
        new DialogueLine { speaker = "Lý Thông", text = "(thầm nghĩ) Chằn tinh sẽ ăn thịt nó, ta khỏi phải chết thay." },
        new DialogueLine { speaker = "Lý Thông", text = "Ừ, em đi cẩn thận nhé. Nhớ mang theo búa." },
    };

    void Update()
    {
        if (DialogueManager.Instance.IsTalking)
        {
            if (hintUI) hintUI.SetActive(false);
            return;
        }

        bool near = Vector3.Distance(player.position, transform.position) <= talkRange;
        if (hintUI) hintUI.SetActive(near);

        if (near && Input.GetKeyDown(KeyCode.E))
            DialogueManager.Instance.StartDialogue(lines, OnDialogueEnd);
    }

    void OnDialogueEnd()
    {
        Debug.Log("Hội thoại kết thúc");
        // Sau này: giao nhiệm vụ mới, mở cửa, bắt đầu cảnh tiếp theo...
    }
}