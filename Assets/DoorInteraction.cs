using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public Transform door;

    public float openAngle = 90f;
    public float openSpeed = 3f;

    private bool playerNear = false;
    private bool isOpen = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        if (door == null)
        {
            // Tự động tìm Transform trên chính GameObject này nếu chưa kéo thả vào Inspector
            door = transform;
            Debug.LogWarning($"DoorInteraction on {gameObject.name}: Biến 'door' chưa được gán trong Inspector, tự động dùng Transform của chính GameObject.", this);
        }

        if (door != null)
        {
            closedRotation = door.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        }
    }

    void Update()
    {
        if (door == null) return;

        // Nhấn Q để mở / đóng cửa
        if (playerNear && Input.GetKeyDown(KeyCode.Q))
        {
            isOpen = !isOpen;
        }

        Quaternion targetRotation =
            isOpen ? openRotation : closedRotation;

        // Cửa mở/đóng từ từ
        door.localRotation = Quaternion.Slerp(
            door.localRotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );
    }

    public void OpenDoor()
    {
        isOpen = true;
    }

    public void CloseDoor()
    {
        isOpen = false;
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            Debug.Log("Đã tới gần cửa - nhấn Q để mở cửa");
        }
        
        // Tự động mở cửa khi Lý Thông hoặc NPC tới gần
        if (other.GetComponent<LyThongGuide>() != null || 
            other.GetComponentInParent<LyThongGuide>() != null || 
            other.name.ToLower().Contains("lythong"))
        {
            OpenDoor();
            Debug.Log("Lý Thông đã tới cửa - Cửa tự động mở!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}