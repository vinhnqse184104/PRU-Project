#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

[InitializeOnLoad]
public class FixRungThachSanhScene
{
    private const string SCENE_PATH = "Assets/Scenes/RungThachSanh.unity";
    private const string FIX_SESSION_KEY = "PRU_RungThachSanh_FullFix_v3_IntroVideoPlayer";

    static FixRungThachSanhScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(FIX_SESSION_KEY, false))
            {
                SessionState.SetBool(FIX_SESSION_KEY, true);
                ExecuteFix();
            }
        };
    }

    [MenuItem("Tools/PRU/Fix All Rung Thach Sanh Issues")]
    public static void ExecuteFixMenu()
    {
        ExecuteFix();
    }

    public static void ExecuteFix()
    {
        Debug.Log("[FixRungThachSanhScene] Bắt đầu tự động thiết lập IntroVideoPlayer và Video 1101.mp4 cho Scene RungThachSanh...");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != SCENE_PATH)
        {
            activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // ==========================================
        // 1. BẢO TOÀN VÀ CHUẨN HÓA NHÂN VẬT THẠCH SANH
        // ==========================================
        GameObject player = GameObject.Find("thachsanh");
        if (player == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length > 0) player = players[0];
        }

        if (player != null)
        {
            foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
            {
                if (child == player.transform) continue;
                if (child.name.ToLower().Contains("meshy") || child.name.ToLower().Contains("axe") || child.name.ToLower().Contains("wanderer"))
                {
                    if (child.localPosition.y != 0f || child.localPosition.x != 0f)
                    {
                        Undo.RecordObject(child, "Reset Child LocalPosition");
                        child.localPosition = Vector3.zero;
                    }
                }
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            Undo.RecordObject(cc, "Configure CharacterController");
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.08f;
            cc.minMoveDistance = 0.001f;

            CapsuleCollider cap = player.GetComponent<CapsuleCollider>();
            if (cap == null) cap = player.AddComponent<CapsuleCollider>();
            Undo.RecordObject(cap, "Configure CapsuleCollider");
            cap.center = new Vector3(0f, 0.9f, 0f);
            cap.radius = 0.35f;
            cap.height = 1.8f;
            cap.isTrigger = true;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.AddComponent<Rigidbody>();
            Undo.RecordObject(rb, "Configure Rigidbody");
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                Undo.RecordObject(anim, "Configure Animator");
                anim.applyRootMotion = false;
            }

            if (Terrain.activeTerrain != null)
            {
                Undo.RecordObject(player.transform, "Align Player to Ground");
                Vector3 p = player.transform.position;
                float groundY = Terrain.activeTerrain.SampleHeight(p) + Terrain.activeTerrain.transform.position.y;
                player.transform.position = new Vector3(p.x, groundY, p.z);
            }
        }

        // ==========================================
        // 2. TẢI ASSET VIDEO CLIP VÀ RENDER TEXTURE
        // ==========================================
        VideoClip clip1101 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/1101.mp4");
        RenderTexture rtIntro = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/IntroRenderTexture.renderTexture");

        // ==========================================
        // 3. TẠO / CẤU HÌNH GAMEOBJECT IntroVideoPlayer
        // ==========================================
        GameObject introVPObj = GameObject.Find("IntroVideoPlayer");
        if (introVPObj == null)
        {
            introVPObj = new GameObject("IntroVideoPlayer");
            Undo.RegisterCreatedObjectUndo(introVPObj, "Create IntroVideoPlayer");
        }

        AudioSource vpAudio = introVPObj.GetComponent<AudioSource>();
        if (vpAudio == null) vpAudio = introVPObj.AddComponent<AudioSource>();
        Undo.RecordObject(vpAudio, "Configure vpAudio");
        vpAudio.playOnAwake = false;
        vpAudio.loop = false;
        vpAudio.clip = null;

        VideoPlayer vp = introVPObj.GetComponent<VideoPlayer>();
        if (vp == null) vp = introVPObj.AddComponent<VideoPlayer>();
        Undo.RecordObject(vp, "Configure VideoPlayer");
        vp.playOnAwake = false; // Tắt PlayOnAwake để GioiThieuCotTruyen chủ động gọi Prepare() và Play()
        vp.isLooping = false;  // Tắt Is Looping
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rtIntro;
        vp.timeReference = VideoTimeReference.InternalTime;
        vp.clip = clip1101;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, vpAudio);

        Debug.Log($"[FixRungThachSanhScene] Đã cấu hình IntroVideoPlayer với VideoClip 1101.mp4 và RenderTexture");

        // ==========================================
        // 4. CẤU HÌNH GIAO DIỆN Panel_GioiThieu & RawImage_Video
        // ==========================================
        GameObject panelGioiThieu = GameObject.Find("Panel_GioiThieu");
        RawImage rawImgComp = null;
        AspectRatioFitter aspectComp = null;

        if (panelGioiThieu != null)
        {
            Image panelImg = panelGioiThieu.GetComponent<Image>();
            if (panelImg != null)
            {
                Undo.RecordObject(panelImg, "Configure Panel Background");
                panelImg.raycastTarget = false;
                panelImg.color = Color.black;
            }

            Canvas rootCanvas = panelGioiThieu.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                CanvasScaler rootScaler = rootCanvas.GetComponent<CanvasScaler>();
                if (rootScaler != null)
                {
                    Undo.RecordObject(rootScaler, "Configure Root CanvasScaler");
                    rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    rootScaler.referenceResolution = new Vector2(1920f, 1080f);
                    rootScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    rootScaler.matchWidthOrHeight = 0.5f;
                }
            }

            // Vô hiệu hóa Txt_TieuDe và Txt_NoiDung
            Transform tieuDeTr = panelGioiThieu.transform.Find("Canvas/Txt_TieuDe");
            if (tieuDeTr == null)
            {
                foreach (var t in panelGioiThieu.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Txt_TieuDe") { tieuDeTr = t; break; }
                }
            }
            if (tieuDeTr != null)
            {
                Undo.RecordObject(tieuDeTr.gameObject, "Disable Txt_TieuDe");
                tieuDeTr.gameObject.SetActive(false);
            }

            Transform noiDungTr = panelGioiThieu.transform.Find("Canvas/Txt_NoiDung");
            if (noiDungTr == null)
            {
                foreach (var t in panelGioiThieu.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Txt_NoiDung") { noiDungTr = t; break; }
                }
            }
            if (noiDungTr != null)
            {
                Undo.RecordObject(noiDungTr.gameObject, "Disable Txt_NoiDung");
                noiDungTr.gameObject.SetActive(false);
            }

            // Tạo / Cấu hình RawImage_Video
            Transform rawImageTr = panelGioiThieu.transform.Find("RawImage_Video");
            GameObject videoObj = rawImageTr != null ? rawImageTr.gameObject : null;
            if (videoObj == null)
            {
                videoObj = new GameObject("RawImage_Video");
                Undo.RegisterCreatedObjectUndo(videoObj, "Create RawImage_Video");
                videoObj.transform.SetParent(panelGioiThieu.transform, false);
                videoObj.transform.SetAsFirstSibling(); // Nằm dưới nút bấm để không che nút
            }

            RectTransform vRect = videoObj.GetComponent<RectTransform>();
            if (vRect == null) vRect = videoObj.AddComponent<RectTransform>();
            Undo.RecordObject(vRect, "Layout RawImage_Video");
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            vRect.pivot = new Vector2(0.5f, 0.5f);

            rawImgComp = videoObj.GetComponent<RawImage>();
            if (rawImgComp == null) rawImgComp = videoObj.AddComponent<RawImage>();
            Undo.RecordObject(rawImgComp, "Configure RawImage");
            rawImgComp.raycastTarget = false;
            rawImgComp.color = Color.white;
            rawImgComp.texture = rtIntro;

            aspectComp = videoObj.GetComponent<AspectRatioFitter>();
            if (aspectComp == null) aspectComp = videoObj.AddComponent<AspectRatioFitter>();
            Undo.RecordObject(aspectComp, "Configure AspectRatioFitter");
            aspectComp.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectComp.aspectRatio = 16f / 9f;

            // Đảm bảo RawImage được bật (Yêu cầu 2)
            videoObj.SetActive(true);

            // Cấu hình Nút Bắt Đầu (Btn_BatDau)
            GameObject btnBatDau = GameObject.Find("Btn_BatDau");
            if (btnBatDau == null)
            {
                Transform btnTr = panelGioiThieu.transform.Find("Canvas/Btn_BatDau");
                if (btnTr != null) btnBatDau = btnTr.gameObject;
            }

            if (btnBatDau != null)
            {
                // Đặt nút làm con của videoObj (RawImage_Video) để luôn bám theo góc dưới bên phải khung video
                if (videoObj != null && btnBatDau.transform.parent != videoObj.transform)
                {
                    Undo.SetTransformParent(btnBatDau.transform, videoObj.transform, "Parent Btn_BatDau to Video");
                }

                Image btnImg = btnBatDau.GetComponent<Image>();
                if (btnImg != null)
                {
                    Undo.RecordObject(btnImg, "Configure Btn_BatDau Image");
                    btnImg.raycastTarget = true;
                    btnImg.color = new Color(0.898f, 0.663f, 0.235f, 1f); // Màu vàng cam ấm áp như hình mẫu
                }

                Button btn = btnBatDau.GetComponent<Button>();
                if (btn != null)
                {
                    Undo.RecordObject(btn, "Configure Btn_BatDau");
                    btn.interactable = true;
                    btn.targetGraphic = btnImg;

                    RectTransform btnRect = btnBatDau.GetComponent<RectTransform>();
                    if (btnRect != null)
                    {
                        Undo.RecordObject(btnRect, "Position Btn_BatDau Bottom-Right inside Video");
                        btnRect.anchorMin = new Vector2(1f, 0f); // Anchor Bottom Right
                        btnRect.anchorMax = new Vector2(1f, 0f);
                        btnRect.pivot = new Vector2(1f, 0f);     // Pivot (1, 0)
                        btnRect.anchoredPosition = new Vector2(-24f, 24f); // Cách mép phải 24px, mép dưới 24px (20-30px)
                        btnRect.sizeDelta = new Vector2(200f, 48f);
                    }

                    var btnTxt = btnBatDau.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (btnTxt != null)
                    {
                        Undo.RecordObject(btnTxt, "Configure Btn Text");
                        btnTxt.raycastTarget = false;
                        btnTxt.text = "BẮT ĐẦU CHƠI »";
                        btnTxt.margin = Vector4.zero;
                        btnTxt.horizontalAlignment = HorizontalAlignmentOptions.Center;
                        btnTxt.verticalAlignment = VerticalAlignmentOptions.Middle;
                        btnTxt.textWrappingMode = TextWrappingModes.NoWrap;
                        btnTxt.fontSize = 20f;
                        btnTxt.fontStyle = FontStyles.Bold;
                        btnTxt.color = new Color(0.12f, 0.12f, 0.12f, 1f);

                        RectTransform textRect = btnTxt.GetComponent<RectTransform>();
                        if (textRect != null)
                        {
                            Undo.RecordObject(textRect, "Center Btn Text Rect");
                            textRect.anchorMin = Vector2.zero;
                            textRect.anchorMax = Vector2.one;
                            textRect.offsetMin = Vector2.zero;
                            textRect.offsetMax = Vector2.zero;
                            textRect.pivot = new Vector2(0.5f, 0.5f);
                            textRect.anchoredPosition = Vector2.zero;
                            textRect.sizeDelta = Vector2.zero;
                        }
                    }

                    GioiThieuCotTruyen gtComp = Object.FindAnyObjectByType<GioiThieuCotTruyen>(FindObjectsInactive.Include);
                    if (gtComp != null)
                    {
                        bool hasEvent = false;
                        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                        {
                            if (btn.onClick.GetPersistentMethodName(i) == "BamNutBatDau")
                            {
                                hasEvent = true;
                                break;
                            }
                        }

                        if (!hasEvent)
                        {
                            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, gtComp.BamNutBatDau);
                        }
                    }

                    // Hiển thị nút ở góc dưới phải video
                    Undo.RecordObject(btnBatDau, "Show Btn_BatDau");
                    btnBatDau.transform.SetAsLastSibling();
                    btnBatDau.SetActive(true);
                }
            }
        }

        // ==========================================
        // 5. CẤU HÌNH THAM CHIẾU TRÊN STORYMANAGER
        // ==========================================
        GameObject storyManager = GameObject.Find("StoryManager");
        if (storyManager != null)
        {
            // Gỡ bỏ VideoPlayer cũ trên StoryManager nếu có (đã chuyển sang IntroVideoPlayer riêng)
            VideoPlayer oldVp = storyManager.GetComponent<VideoPlayer>();
            if (oldVp != null)
            {
                Undo.DestroyObjectImmediate(oldVp);
            }

            GioiThieuCotTruyen gt = storyManager.GetComponent<GioiThieuCotTruyen>();
            if (gt != null)
            {
                Undo.RecordObject(gt, "Wire GioiThieuCotTruyen References");
                gt.panelGioiThieu = panelGioiThieu;
                gt.videoPlayer = vp;
                gt.videoClip = clip1101;
                gt.rawImageVideo = rawImgComp;
                gt.aspectFitter = aspectComp;
                gt.renderTexture = rtIntro;
                gt.loaGioiThieu = vpAudio;

                GameObject btnBatDauObj = GameObject.Find("Btn_BatDau");
                if (btnBatDauObj == null && panelGioiThieu != null)
                {
                    Transform bTr = panelGioiThieu.transform.Find("Canvas/Btn_BatDau");
                    if (bTr != null) btnBatDauObj = bTr.gameObject;
                }
                if (btnBatDauObj != null)
                {
                    gt.btnBatDau = btnBatDauObj.GetComponent<Button>();
                }
            }
        }

        // ==========================================
        // 6. SỬA LỖI EVENTSYSTEM & INPUT MODULE
        // ==========================================
        EventSystem evtSys = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (evtSys == null)
        {
            GameObject evtObj = new GameObject("EventSystem");
            evtSys = evtObj.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(evtObj, "Create EventSystem");
        }

        var inputSystemModule = evtSys.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var standalone = evtSys.GetComponent<StandaloneInputModule>();

        if (inputSystemModule != null)
        {
            inputSystemModule.enabled = true;
            if (standalone != null)
            {
                Undo.RecordObject(standalone, "Disable StandaloneInputModule");
                standalone.enabled = false;
            }
        }
        else if (standalone != null)
        {
            standalone.enabled = true;
        }

        // ==========================================
        // 7. LƯU SCENE
        // ==========================================
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[FixRungThachSanhScene] ĐÃ LƯU SCENE RungThachSanh VỚI ĐẦY ĐỦ IntroVideoPlayer, RenderTexture, RawImage VÀ VIDEO 1101.MP4!");
    }
}
#endif
