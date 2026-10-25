using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện bắt buộc để load lại màn chơi

public class QuanLyGame : MonoBehaviour
{
    [Header("Giao diện")]
    public GameObject panelGameOver;

    // Gọi hàm này khi Thạch Sanh chết
    public void HienThiManHinhThua()
    {
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        // Hiện chuột để người chơi có thể bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Hàm này sẽ gắn vào nút "Chơi Lại"
    public void BamNutChoiLai()
    {
        // Khôi phục lại thời gian thực nếu game đang bị tạm dừng
        Time.timeScale = 1f;

        // Tải lại chính màn chơi hiện tại từ đầu
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}