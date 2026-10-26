using UnityEngine;

public class GioiThieuCotTruyen : MonoBehaviour
{
    [Header("Giao diện Cốt truyện")]
    public GameObject panelGioiThieu;

    [Header("Giao diện Trong Game")]
    public GameObject giaoDienGame;

    [Header("Âm thanh Giới thiệu (THÊM MỚI)")]
    public AudioSource loaGioiThieu; // Nơi phát ra tiếng
    public AudioClip nhacGioiThieu;  // File âm thanh (giọng đọc hoặc nhạc nền)

    void Start()
    {
        if (panelGioiThieu != null) panelGioiThieu.SetActive(true);
        if (giaoDienGame != null) giaoDienGame.SetActive(false);

        // Phát âm thanh giới thiệu ngay khi mở game
        if (loaGioiThieu != null && nhacGioiThieu != null)
        {
            loaGioiThieu.clip = nhacGioiThieu;
            loaGioiThieu.Play();
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BamNutBatDau()
    {
        if (panelGioiThieu != null) panelGioiThieu.SetActive(false);
        if (giaoDienGame != null) giaoDienGame.SetActive(true);

        // Tắt âm thanh giới thiệu khi bấm nút chơi
        if (loaGioiThieu != null)
        {
            loaGioiThieu.Stop();
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}