using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class GioiThieuCotTruyen : MonoBehaviour
{
    [Header("Giao diện Màn hình Giới thiệu")]
    public GameObject panelGioiThieu;

    [Header("Giao diện Trong Game")]
    public GameObject giaoDienGame;

    [Header("Cấu hình Video Giới thiệu")]
    public VideoPlayer videoPlayer;
    public VideoClip videoClip;
    public RawImage rawImageVideo;
    public AspectRatioFitter aspectFitter;
    public RenderTexture renderTexture;

    [Header("Âm thanh Giới thiệu")]
    public AudioSource loaGioiThieu;

    [Header("Nút Bắt Đầu Chơi")]
    public Button btnBatDau;

    private RectTransform btnRect;

    // Trạng thái phát video
    private bool hasActuallyStartedPlaying = false;
    private float playbackSeconds = 0f;
    private bool hasFinishedVideo = false;
    private bool hasStartedGame = false;

    // Component điều khiển nhân vật & camera
    private MonoBehaviour playerMovement;
    private MonoBehaviour playerCut;
    private MonoBehaviour playerKick;
    private MonoBehaviour cameraFollow;

    void Awake()
    {
        EnsureEventSystemSetup();
        EnsureWiring();
    }

    void Start()
    {
        hasStartedGame = false;
        hasActuallyStartedPlaying = false;
        playbackSeconds = 0f;
        hasFinishedVideo = false;
        hasAttemptedUrlFallback = false;

        // 1. Khóa điều khiển nhân vật & camera ngay khi mở Scene
        LockPlayerControls();

        // 2. Mở con trỏ chuột để người chơi có thể bấm nút "BẮT ĐẦU CHƠI" bất cứ lúc nào
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Đảm bảo Time.timeScale = 1 để Engine giải mã video & phát audio
        Time.timeScale = 1f;

        // 4. Bắt đầu quy trình phát video đúng trình tự (Yêu cầu 6)
        StartVideoSequence();
    }

    void Update()
    {
        if (hasStartedGame) return;

        // Luôn giải phóng chuột để người chơi có thể tương tác bấm nút Bắt đầu
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Hỗ trợ phím tắt Space / Enter / NumpadEnter để bắt đầu chơi ngay lập tức
        bool startPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            startPressed = true;
        }
#endif
        if (!startPressed)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    startPressed = true;
                }
            }
            catch { }
        }

        if (startPressed)
        {
            Debug.Log("[GioiThieuCotTruyen] Nhận phím Space/Enter -> Bắt đầu chơi game!");
            BamNutBatDau();
            return;
        }

        // Click chuột trực tiếp fallback trên rect của nút
        bool mouseClicked = false;
        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseClicked = true;
            mousePos = Mouse.current.position.ReadValue();
        }
#endif
        if (!mouseClicked)
        {
            try
            {
                if (Input.GetMouseButtonDown(0))
                {
                    mouseClicked = true;
                    mousePos = Input.mousePosition;
                }
            }
            catch { }
        }

        if (mouseClicked && btnRect != null && btnRect.gameObject.activeInHierarchy)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(btnRect, mousePos, null))
            {
                Debug.Log($"[GioiThieuCotTruyen] Nhận click trực tiếp tại {mousePos} trên Btn_BatDau!");
                BamNutBatDau();
                return;
            }
        }

        // ==========================================
        // GIAI ĐOẠN 1: VIDEO ĐANG CHUẨN BỊ VÀ PHÁT
        // ==========================================
        if (!hasFinishedVideo && videoPlayer != null)
        {
            // Xác nhận video đã thực sự bắt đầu phát (thời gian chạy > 0.3s hoặc frame > 5)
            if (!hasActuallyStartedPlaying && videoPlayer.isPlaying)
            {
                if (videoPlayer.time > 0.3 || videoPlayer.frame > 5)
                {
                    hasActuallyStartedPlaying = true;
                    Debug.Log($"[GioiThieuCotTruyen] Video 1101.mp4 đã thực sự phát ổn định (Thời lượng: {videoPlayer.length:F1}s | Time: {videoPlayer.time:F2}s | Frame: {videoPlayer.frame}).");
                }
            }

            // Đếm thời gian thực phát video
            if (hasActuallyStartedPlaying && videoPlayer.isPlaying)
            {
                playbackSeconds += Time.unscaledDeltaTime;

                // Kiểm tra video chạm mốc cuối thời lượng (chỉ xét khi đã phát trên 3 giây và tổng thời lượng > 5s)
                if (playbackSeconds >= 3.0f && videoPlayer.length > 5.0f)
                {
                    if (videoPlayer.time >= (videoPlayer.length - 0.25f))
                    {
                        Debug.Log($"[GioiThieuCotTruyen] Video chạm mốc cuối thời lượng ({videoPlayer.time:F2}s / {videoPlayer.length:F2}s). Tự động vào game...");
                        OnVideoFinishedSuccessfully();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Thực hiện đúng trình tự yêu cầu:
    /// 1. Mở Scene
    /// 2. Ẩn nút BẮT ĐẦU CHƠI
    /// 3. Bật giao diện video
    /// 4. Gọi VideoPlayer.Prepare()
    /// 5. Chờ sự kiện prepareCompleted
    /// 6. Gọi VideoPlayer.Play()
    /// 7. Hiển thị hình ảnh và phát âm thanh của video
    /// 8. Khi video thực sự kết thúc, ẩn video
    /// 9. Hiện nút BẮT ĐẦU CHƠI
    /// </summary>
    private void StartVideoSequence()
    {
        // 1. Cấu hình vị trí nút BẮT ĐẦU CHƠI ở góc dưới bên phải khung video
        if (btnBatDau != null)
        {
            if (rawImageVideo != null && btnBatDau.transform.parent != rawImageVideo.transform)
            {
                btnBatDau.transform.SetParent(rawImageVideo.transform, false);
            }
            btnRect = btnBatDau.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchorMin = new Vector2(1f, 0f); // Anchor Bottom Right (Yêu cầu 2)
                btnRect.anchorMax = new Vector2(1f, 0f);
                btnRect.pivot = new Vector2(1f, 0f);     // Pivot (1, 0) (Yêu cầu 2)
                btnRect.anchoredPosition = new Vector2(-24f, 24f); // Cách mép phải 24px, mép dưới 24px (Yêu cầu 1)
                btnRect.sizeDelta = new Vector2(200f, 48f);
            }
            btnBatDau.transform.SetAsLastSibling();
            btnBatDau.gameObject.SetActive(true);
            btnBatDau.interactable = true;
        }

        // 2. Bật giao diện video (Yêu cầu 6.3)
        if (panelGioiThieu != null) panelGioiThieu.SetActive(true);
        if (giaoDienGame != null) giaoDienGame.SetActive(false);

        // 3. Đảm bảo RenderTexture tồn tại và có GPU Buffer thực sự
        if (renderTexture == null || !renderTexture.IsCreated())
        {
#if UNITY_EDITOR
            if (renderTexture == null)
            {
                renderTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/IntroRenderTexture.renderTexture");
            }
#endif
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                renderTexture.name = "DynamicIntroRT";
            }
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Bilinear;
            if (!renderTexture.IsCreated())
            {
                renderTexture.Create();
            }
        }

        // 4. Bật RawImage hiển thị RenderTexture và căn chỉnh toàn màn hình
        if (rawImageVideo != null)
        {
            RectTransform rt = rawImageVideo.rectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
            rawImageVideo.texture = renderTexture;
            rawImageVideo.color = new Color(1f, 1f, 1f, 1f); // Trắng với Alpha = 255 (Yêu cầu 6)
            rawImageVideo.raycastTarget = false;
            rawImageVideo.transform.SetAsFirstSibling();
            rawImageVideo.gameObject.SetActive(true);
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[GioiThieuCotTruyen] Không tìm thấy VideoPlayer trong Scene!");
            HandleVideoFailure();
            return;
        }

        // 5. Cấu hình VideoPlayer
        videoPlayer.playOnAwake = false; // Tắt PlayOnAwake để tự chủ gọi Prepare() và Play()
        videoPlayer.isLooping = false;  // Tắt Loop để video chỉ phát 1 lần
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.waitForFirstFrame = true;

        // Cấu hình âm thanh qua AudioSource
        if (loaGioiThieu != null)
        {
            loaGioiThieu.playOnAwake = false;
            loaGioiThieu.loop = false;
            loaGioiThieu.clip = null;
            loaGioiThieu.volume = 1f;
            loaGioiThieu.mute = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, loaGioiThieu);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        // Cấu hình nguồn Video (ưu tiên VideoClip trong Assets, đa lớp fallback StreamingAssets / Assets)
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "1101.mp4");
        string assetsMp4Path = Path.Combine(Application.dataPath, "1101.mp4");

        if (videoClip == null)
        {
            videoClip = videoPlayer.clip;
        }
#if UNITY_EDITOR
        if (videoClip == null)
        {
            videoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/1101.mp4");
        }
#endif
        if (videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            Debug.Log($"[GioiThieuCotTruyen] Gán VideoClip: {videoClip.name} (Thời lượng: {videoClip.length:F1}s)");
        }
        else if (File.Exists(streamingPath))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = streamingPath;
            Debug.Log($"[GioiThieuCotTruyen] Gán Video URL từ StreamingAssets: {streamingPath}");
        }
        else if (File.Exists(assetsMp4Path))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = assetsMp4Path;
            Debug.Log($"[GioiThieuCotTruyen] Gán Video URL từ Assets: {assetsMp4Path}");
        }
        else
        {
            Debug.LogError("[GioiThieuCotTruyen] Không tìm thấy VideoClip hoặc file 1101.mp4!");
        }

        // 6. Đăng ký các sự kiện VideoPlayer
        videoPlayer.prepareCompleted -= OnPrepareCompleted;
        videoPlayer.prepareCompleted += OnPrepareCompleted;

        videoPlayer.loopPointReached -= OnLoopPointReached;
        videoPlayer.loopPointReached += OnLoopPointReached;

        videoPlayer.errorReceived -= OnErrorReceived;
        videoPlayer.errorReceived += OnErrorReceived;

        // 7. Gọi VideoPlayer.Prepare() (Yêu cầu 6.4)
        if (videoPlayer.isPrepared)
        {
            Debug.Log("[GioiThieuCotTruyen] VideoPlayer đã được chuẩn bị trước đó, tiến hành phát ngay...");
            OnPrepareCompleted(videoPlayer);
        }
        else
        {
            Debug.Log("[GioiThieuCotTruyen] Bắt đầu gọi VideoPlayer.Prepare()...");
            videoPlayer.Prepare();
        }
    }

    /// <summary>
    /// Chờ sự kiện prepareCompleted và gọi VideoPlayer.Play() (Yêu cầu 6.5, 6.6)
    /// </summary>
    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (hasFinishedVideo || hasStartedGame) return;

        int vidWidth = (int)source.width;
        int vidHeight = (int)source.height;
        if (vidWidth <= 0 && source.texture != null) vidWidth = source.texture.width;
        if (vidHeight <= 0 && source.texture != null) vidHeight = source.texture.height;
        if (vidWidth <= 0) vidWidth = 1920;
        if (vidHeight <= 0) vidHeight = 1080;

        Debug.Log($"[GioiThieuCotTruyen] VideoPlayer.prepareCompleted thành công! (Kích thước video: {vidWidth}x{vidHeight}, Thời lượng: {source.length:F2}s, Tổng Frame: {source.frameCount})");

        // Đảm bảo RenderTexture có kích thước khớp video và đang active
        if (renderTexture == null || !renderTexture.IsCreated() || renderTexture.width != vidWidth || renderTexture.height != vidHeight)
        {
            if (renderTexture != null && renderTexture.IsCreated())
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(vidWidth, vidHeight, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = $"DynamicIntroRT_{vidWidth}x{vidHeight}";
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.Create();

            source.targetTexture = renderTexture;
        }

        // Cân chỉnh tỷ lệ AspectRatioFitter
        if (aspectFitter != null && vidHeight > 0)
        {
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = (float)vidWidth / vidHeight;
        }

        if (rawImageVideo != null)
        {
            RectTransform rt = rawImageVideo.rectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
            rawImageVideo.texture = source.targetTexture != null ? source.targetTexture : renderTexture;
            rawImageVideo.color = new Color(1f, 1f, 1f, 1f); // Trắng với Alpha = 255 (Yêu cầu 6)
            rawImageVideo.transform.SetAsFirstSibling();
            rawImageVideo.gameObject.SetActive(true);
        }

        // Bắt đầu phát từ đầu (Yêu cầu 6.6, 6.7)
        source.time = 0;
        source.Play();
        hasActuallyStartedPlaying = false;
        playbackSeconds = 0f;

        if (btnBatDau != null)
        {
            btnBatDau.transform.SetAsLastSibling();
            btnBatDau.gameObject.SetActive(true);
            btnBatDau.interactable = true;
        }

        Debug.Log("[GioiThieuCotTruyen] Đã gọi VideoPlayer.Play()! Bắt đầu phát hình ảnh và âm thanh của video 1101.mp4...");
    }

    /// <summary>
    /// Xử lý loopPointReached: Chỉ coi là kết thúc hợp lệ khi video đã bắt đầu phát thành công và thực sự đến cuối video (Yêu cầu 5)
    /// </summary>
    private void OnLoopPointReached(VideoPlayer source)
    {
        // 1. Kiểm tra video đã thực sự bắt đầu phát chưa
        if (!hasActuallyStartedPlaying)
        {
            Debug.LogWarning($"[GioiThieuCotTruyen] Bỏ qua loopPointReached sớm: Video chưa thực sự bắt đầu phát (hasActuallyStartedPlaying={hasActuallyStartedPlaying}, time={source.time:F2}s).");
            return;
        }

        // 2. Kiểm tra thời gian đã phát ít nhất 3 giây để tránh tín hiệu giả đầu video
        if (playbackSeconds < 3.0f)
        {
            Debug.LogWarning($"[GioiThieuCotTruyen] Bỏ qua loopPointReached sớm: Thời gian phát mới đạt {playbackSeconds:F2}s (< 3.0s).");
            return;
        }

        // 3. Kiểm tra thời gian hiện tại phải đạt ít nhất 90% tổng thời lượng
        if (source.length > 5.0f && source.time < (source.length * 0.9f))
        {
            Debug.LogWarning($"[GioiThieuCotTruyen] Bỏ qua loopPointReached sớm: Chưa đạt 90% thời lượng (time={source.time:F2}s / {source.length:F2}s).");
            return;
        }

        Debug.Log($"[GioiThieuCotTruyen] loopPointReached hợp lệ! Video đã phát hoàn tất (Đã xem: {playbackSeconds:F1}s / {source.length:F1}s).");
        OnVideoFinishedSuccessfully();
    }

    private bool hasAttemptedUrlFallback = false;

    /// <summary>
    /// Xử lý errorReceived: Nếu phát lỗi, ghi rõ nguyên nhân vào Console, không được thông báo video đã phát xong (Yêu cầu 7)
    /// </summary>
    private void OnErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"[GioiThieuCotTruyen] LỖI VIDEOPLAYER: {message}. Video không thể phát thành công.");

        // Thử dự phòng phát trực tiếp file MP4 qua URL nếu VideoClip bị lỗi giải mã
        if (!hasAttemptedUrlFallback && source != null)
        {
            hasAttemptedUrlFallback = true;
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "1101.mp4");
            string assetsMp4Path = Path.Combine(Application.dataPath, "1101.mp4");
            string targetPath = File.Exists(streamingPath) ? streamingPath : (File.Exists(assetsMp4Path) ? assetsMp4Path : null);

            if (!string.IsNullOrEmpty(targetPath))
            {
                Debug.LogWarning($"[GioiThieuCotTruyen] Đang kích hoạt chế độ URL Fallback phát trực tiếp từ file: {targetPath}");
                source.source = VideoSource.Url;
                source.url = targetPath;
                source.Prepare();
                return;
            }
        }

        HandleVideoFailure();
    }

    /// <summary>
    /// Khi video kết thúc: Tự động bắt đầu trò chơi theo luồng đã thiết lập (Yêu cầu 3)
    /// </summary>
    private void OnVideoFinishedSuccessfully()
    {
        if (hasFinishedVideo || hasStartedGame) return;
        hasFinishedVideo = true;

        Debug.Log($"[GioiThieuCotTruyen] Video 1101.mp4 đã phát xong toàn bộ (Thời lượng xem: {playbackSeconds:F1}s). Tự động bắt đầu trò chơi theo luồng...");
        BamNutBatDau();
    }

    /// <summary>
    /// Xử lý khi có lỗi thật: Không thông báo video phát xong, mở nút Bắt đầu để người chơi tiếp tục vào game
    /// </summary>
    private void HandleVideoFailure()
    {
        if (hasFinishedVideo || hasStartedGame) return;
        hasFinishedVideo = true;

        Debug.LogWarning("[GioiThieuCotTruyen] Do phát sinh lỗi phát video, hiển thị nút 'BẮT ĐẦU CHƠI' để người chơi tiếp tục vào game.");

        if (videoPlayer != null)
        {
            try { videoPlayer.Stop(); } catch { }
        }
        if (rawImageVideo != null)
        {
            rawImageVideo.gameObject.SetActive(false);
        }
        if (loaGioiThieu != null)
        {
            loaGioiThieu.Stop();
        }

        if (btnBatDau != null)
        {
            btnBatDau.gameObject.SetActive(true);
            btnBatDau.interactable = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Khi nhấn nút, bắt đầu gameplay như trước (Yêu cầu 11)
    /// </summary>
    public void BamNutBatDau()
    {
        if (hasStartedGame) return;
        hasStartedGame = true;

        Debug.Log("[GioiThieuCotTruyen] BẮT ĐẦU GAME! Đang đóng màn hình giới thiệu và kích hoạt gameplay...");

        // Dừng video & âm thanh
        if (videoPlayer != null)
        {
            try { videoPlayer.Stop(); } catch { }
        }
        if (loaGioiThieu != null)
        {
            loaGioiThieu.Stop();
        }

        // Đóng màn hình giới thiệu
        if (panelGioiThieu != null)
        {
            panelGioiThieu.SetActive(false);
        }

        // Bật giao diện gameplay
        if (giaoDienGame != null)
        {
            giaoDienGame.SetActive(true);
        }

        GameObject thanhMau = GameObject.Find("ThanhMau_ThachSanh");
        if (thanhMau != null) thanhMau.SetActive(true);

        GameObject bangNhiemVu = GameObject.Find("BangNhiemVu");
        if (bangNhiemVu != null) bangNhiemVu.SetActive(true);

        // Mở lại điều khiển nhân vật & camera
        UnlockPlayerControls();

        // Khôi phục Time.timeScale = 1 và khóa chuột cho gameplay
        Time.timeScale = 1f;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "RungThachSanh")
        {
            QuestManager forestQuest = Object.FindAnyObjectByType<QuestManager>();
            if (forestQuest != null) forestQuest.OnForestGameplayStarted();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[GioiThieuCotTruyen] Gameplay đã được kích hoạt thành công (Controls Unlocked, Cursor Locked).");
    }

    private void FindPlayerComponents()
    {
        GameObject player = GameObject.Find("thachsanh");
        if (player == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players != null && players.Length > 0) player = players[0];
        }

        if (player != null)
        {
            foreach (var comp in player.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (typeName == "PlayerMovement") playerMovement = comp;
                else if (typeName == "PlayerCut") playerCut = comp;
                else if (typeName == "PlayerKick") playerKick = comp;
            }

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent("CameraFollow") as MonoBehaviour;
        }
        if (cameraFollow == null)
        {
            var allComps = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allComps)
            {
                if (c != null && c.GetType().Name == "CameraFollow")
                {
                    cameraFollow = c;
                    break;
                }
            }
        }
    }

    private void LockPlayerControls()
    {
        FindPlayerComponents();

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerCut != null) playerCut.enabled = false;
        if (playerKick != null) playerKick.enabled = false;
        if (cameraFollow != null) cameraFollow.enabled = false;
    }

    private void UnlockPlayerControls()
    {
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerCut != null) playerCut.enabled = true;
        if (playerKick != null) playerKick.enabled = true;
        if (cameraFollow != null) cameraFollow.enabled = true;
    }

    private void EnsureEventSystemSetup()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
        }
        if (!es.gameObject.activeSelf) es.gameObject.SetActive(true);
        es.enabled = true;

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputSystemModule = es.GetComponent<InputSystemUIInputModule>();
        StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();

        if (inputSystemModule != null)
        {
            inputSystemModule.enabled = true;
            if (standalone != null) standalone.enabled = false;
        }
        else if (standalone != null)
        {
            standalone.enabled = true;
        }
        else
        {
            inputSystemModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            inputSystemModule.enabled = true;
        }
#else
        StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone == null)
        {
            standalone = es.gameObject.AddComponent<StandaloneInputModule>();
        }
        standalone.enabled = true;
#endif
    }

    private void EnsureWiring()
    {
        // 1. Tìm GameObject IntroVideoPlayer hoặc VideoPlayer
        if (videoPlayer == null)
        {
            GameObject introVPObj = GameObject.Find("IntroVideoPlayer");
            if (introVPObj != null)
            {
                videoPlayer = introVPObj.GetComponent<VideoPlayer>();
                if (loaGioiThieu == null) loaGioiThieu = introVPObj.GetComponent<AudioSource>();
            }
            if (videoPlayer == null)
            {
                videoPlayer = Object.FindAnyObjectByType<VideoPlayer>(FindObjectsInactive.Include);
            }
        }

        if (loaGioiThieu == null && videoPlayer != null)
        {
            loaGioiThieu = videoPlayer.GetComponent<AudioSource>();
            if (loaGioiThieu == null) loaGioiThieu = GetComponent<AudioSource>();
        }

        // 2. Tìm Panel_GioiThieu
        if (panelGioiThieu == null) panelGioiThieu = GameObject.Find("Panel_GioiThieu");
        if (giaoDienGame == null) giaoDienGame = GameObject.Find("ThanhMau_ThachSanh");

        if (panelGioiThieu != null)
        {
            // Nền đen
            Image panelImg = panelGioiThieu.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.raycastTarget = false;
                panelImg.color = Color.black;
            }

            // Vô hiệu hóa toàn bộ văn bản giới thiệu cũ (Txt_TieuDe và Txt_NoiDung)
            Transform tieuDeTr = panelGioiThieu.transform.Find("Canvas/Txt_TieuDe");
            if (tieuDeTr == null)
            {
                foreach (var t in panelGioiThieu.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Txt_TieuDe") { tieuDeTr = t; break; }
                }
            }
            if (tieuDeTr != null) tieuDeTr.gameObject.SetActive(false);

            Transform noiDungTr = panelGioiThieu.transform.Find("Canvas/Txt_NoiDung");
            if (noiDungTr == null)
            {
                foreach (var t in panelGioiThieu.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Txt_NoiDung") { noiDungTr = t; break; }
                }
            }
            if (noiDungTr != null) noiDungTr.gameObject.SetActive(false);

            // Tìm RawImage_Video
            if (rawImageVideo == null)
            {
                Transform vTr = panelGioiThieu.transform.Find("RawImage_Video");
                if (vTr != null)
                {
                    rawImageVideo = vTr.GetComponent<RawImage>();
                    aspectFitter = vTr.GetComponent<AspectRatioFitter>();
                }
            }

            if (rawImageVideo == null)
            {
                GameObject vObj = new GameObject("RawImage_Video");
                vObj.transform.SetParent(panelGioiThieu.transform, false);
                vObj.transform.SetAsFirstSibling();

                RectTransform vRect = vObj.AddComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.offsetMin = Vector2.zero;
                vRect.offsetMax = Vector2.zero;
                vRect.pivot = new Vector2(0.5f, 0.5f);

                rawImageVideo = vObj.AddComponent<RawImage>();
                rawImageVideo.raycastTarget = false;
                rawImageVideo.color = Color.white;

                aspectFitter = vObj.AddComponent<AspectRatioFitter>();
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspectFitter.aspectRatio = 16f / 9f;
            }

            if (aspectFitter == null && rawImageVideo != null)
            {
                aspectFitter = rawImageVideo.GetComponent<AspectRatioFitter>();
                if (aspectFitter == null) aspectFitter = rawImageVideo.gameObject.AddComponent<AspectRatioFitter>();
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspectFitter.aspectRatio = 16f / 9f;
            }

            if (rawImageVideo != null)
            {
                RectTransform rt = rawImageVideo.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                rawImageVideo.color = new Color(1f, 1f, 1f, 1f);
                rawImageVideo.raycastTarget = false;
                rawImageVideo.transform.SetAsFirstSibling();
                rawImageVideo.gameObject.SetActive(true);
            }

            // Tìm Nút Bắt Đầu
            if (btnBatDau == null)
            {
                Button[] buttons = panelGioiThieu.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b.name.Contains("BatDau") || b.name.Contains("Start"))
                    {
                        btnBatDau = b;
                        break;
                    }
                }
            }

            if (btnBatDau != null)
            {
                if (rawImageVideo != null && btnBatDau.transform.parent != rawImageVideo.transform)
                {
                    btnBatDau.transform.SetParent(rawImageVideo.transform, false);
                }

                btnRect = btnBatDau.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    btnRect.anchorMin = new Vector2(1f, 0f); // Anchor Bottom Right (Yêu cầu 2)
                    btnRect.anchorMax = new Vector2(1f, 0f);
                    btnRect.pivot = new Vector2(1f, 0f);     // Pivot (1, 0) (Yêu cầu 2)
                    btnRect.anchoredPosition = new Vector2(-24f, 24f); // Cách mép phải 24px, mép dưới 24px (Yêu cầu 1)
                    btnRect.sizeDelta = new Vector2(200f, 48f);
                }

                Image btnImg = btnBatDau.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.raycastTarget = true;
                    btnImg.color = new Color(0.898f, 0.663f, 0.235f, 1f); // Màu vàng cam ấm áp như hình mẫu
                }

                TextMeshProUGUI btnTxt = btnBatDau.GetComponentInChildren<TextMeshProUGUI>(true);
                if (btnTxt != null)
                {
                    btnTxt.raycastTarget = false;
                    btnTxt.text = "BẮT ĐẦU CHƠI »";
                    btnTxt.margin = Vector4.zero;
                    btnTxt.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    btnTxt.verticalAlignment = VerticalAlignmentOptions.Middle;
                    btnTxt.textWrappingMode = TextWrappingModes.NoWrap;
                    btnTxt.fontSize = 20f;
                    btnTxt.fontStyle = FontStyles.Bold;
                    btnTxt.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                }

                btnBatDau.onClick.RemoveListener(BamNutBatDau);
                btnBatDau.onClick.AddListener(BamNutBatDau);

                btnBatDau.transform.SetAsLastSibling();
                btnBatDau.gameObject.SetActive(true);
                btnBatDau.interactable = true;
            }
        }

        // Tự động gán RenderTexture
        if (renderTexture == null)
        {
#if UNITY_EDITOR
            renderTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/IntroRenderTexture.renderTexture");
#endif
        }

        if (renderTexture == null && videoPlayer != null && videoPlayer.targetTexture != null)
        {
            renderTexture = videoPlayer.targetTexture;
        }

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = "DynamicIntroRT";
            renderTexture.Create();
        }

        if (rawImageVideo != null && renderTexture != null)
        {
            rawImageVideo.texture = renderTexture;
            rawImageVideo.gameObject.SetActive(true);
        }

        if (videoPlayer != null && renderTexture != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
        }

        // Tự động gán VideoClip
        if (videoClip == null)
        {
#if UNITY_EDITOR
            videoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/1101.mp4");
#endif
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.errorReceived -= OnErrorReceived;
        }
    }
}
