using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PRU.M4
{
    // Opt-in story session: standalone M4 scenes retain their existing test behavior.
    public sealed class M4StoryFlow : MonoBehaviour
    {
        public const string MenuScene = "Assets/Gameplay_M4/Story/Scene_M4_Story.unity";
        public const string Chapter1 = "Assets/Gameplay_M4/Scene_M4_Gameplay.unity";
        public const string Chapter2 = "Assets/Gameplay_M4/Chapter2/Scene_M4_Chapter2_Map.unity";
        public const string Chapter3 = "Assets/Gameplay_M4/Chapter3/Scene_M4_Chapter3_Map.unity";
        public const string ReturnHome = "Assets/Scenes/Chapter2_LyThong.unity";
        public const string Chapter4 = "Assets/Scenes/Chapter4_LythongCuopCong.unity";
        public const string Chapter5 = "Assets/Scenes/Chapter5_DaiBangBatCongChua.unity";
        public const string Tracking = "Assets/Gameplay_M4/Chapter5/Scene_M4_Chapter5_Tracking.unity";
        const string CheckpointKey = "PRU_M4_StoryCheckpoint";
        const string FlagsKey = "PRU_M4_StoryFlags";
        const string CompletedKey = "PRU_M4_StoryCompleted";
        static readonly string[] Stages = { Chapter1, Chapter2, Chapter3, ReturnHome, Chapter4, Chapter5, Tracking };
        public static bool SessionActive { get; private set; }
        public static bool HasCheckpoint => Array.IndexOf(Stages, PlayerPrefs.GetString(CheckpointKey, "")) >= 0;
        public static bool CampaignCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;
        public bool CanContinue { get; private set; }
        public string Error { get; private set; } = "";
        public string CurrentPath => gameObject.scene.path;
        string completionTitle, completionText, destination;
        float readyAt;
        bool transitioning, registeredReady;
        M4WoodQuest wood;
        M4Chapter2Quest meeting;
        M4Chapter3Game battle;
        M4Chapter5Quest tracking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { SessionActive = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= Attach;
            SceneManager.sceneLoaded += Attach;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitialScene() { Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single); }
        static void Attach(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != MenuScene && (!SessionActive || Array.IndexOf(Stages, scene.path) < 0)) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<M4StoryFlow>() != null) return;
            var host = new GameObject("M4_StoryFlow");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<M4StoryFlow>();
        }
        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (CurrentPath == MenuScene) { SessionActive = false; return; }
            SaveCheckpoint(CurrentPath);
            wood = FindFirstObjectByType<M4WoodQuest>();
            meeting = FindFirstObjectByType<M4Chapter2Quest>();
            battle = FindFirstObjectByType<M4Chapter3Game>();
            tracking = FindFirstObjectByType<M4Chapter5Quest>();
            if (meeting != null) meeting.showDemoLabel = false;
            if (tracking != null) tracking.loadNextScene = false;
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (CurrentPath == MenuScene)
            {
                if (keyboard == null) return;
                if (keyboard.nKey.wasPressedThisFrame) StartNewStory();
                else if (keyboard.enterKey.wasPressedThisFrame)
                { if (HasCheckpoint && !CampaignCompleted) ResumeStory(); else StartNewStory(); }
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Load(MenuScene); return; }
            bool complete = wood != null && wood.State == M4WoodQuest.QuestState.Completed ||
                meeting != null && meeting.State == M4Chapter2Quest.Chapter2State.Completed ||
                battle != null && battle.State == M4Chapter3Game.RoundState.Won ||
                tracking != null && tracking.State == M4Chapter5Quest.QuestState.Completed;
            if (battle != null) battle.showTemporaryHud = !complete;
            if (!complete) { CanContinue = registeredReady = false; return; }
            if (!registeredReady)
            {
                registeredReady = true; readyAt = Time.unscaledTime + .75f;
                if (wood != null) SetCompletion("CHƯƠNG 1 HOÀN THÀNH", "Đã nhặt đủ 5 bó củi. Tiếp theo: gặp Lý Thông và theo anh ấy về nhà.", Chapter2);
                else if (meeting != null) SetCompletion("CHƯƠNG 2 HOÀN THÀNH", "Thạch Sanh kết nghĩa với Lý Thông. Sau đó, Lý Thông nhờ chàng đi canh miếu thay mình. Hãy tới miếu và đối mặt Chằn Tinh.", Chapter3);
                else if (battle != null) SetCompletion("ĐÃ HẠ CHẰN TINH", "Mang đầu Chằn Tinh trở về nhà, gặp mẹ con Lý Thông để báo tin.", ReturnHome);
                else
                {
                    PlayerPrefs.SetInt(CompletedKey, 1); PlayerPrefs.Save();
                    tracking.useBuiltInInput = false;
                    tracking.player.SetInputLocked(true);
                    SetCompletion("HOÀN THÀNH CHƯƠNG 1–5", "Bạn đã tìm được cửa hang Đại Bàng. Tiến trình chương 1–5 đã hoàn thành. Trở về màn hình đầu để chơi lại; chương 6 sẽ được bổ sung sau.", MenuScene);
                }
            }
            CanContinue = Time.unscaledTime >= readyAt;
            if (CanContinue && keyboard != null && keyboard.enterKey.wasPressedThisFrame) ContinueChapter();
        }
        void SetCompletion(string title, string text, string next)
        { completionTitle = title; completionText = text; destination = next; }
        public bool ContinueChapter()
        {
            if (!CanContinue || transitioning || string.IsNullOrEmpty(destination) || !ValidateScene(destination)) return false;
            if (battle != null)
            {
                StoryStateManager.IsChanTinhDefeated = true;
                StoryStateManager.HasChanTinhHead = true;
                StoryStateManager.IsReturningToChapter2 = true;
                StoryStateManager.IsReturnDialogueCompleted = false;
                StoryStateManager.IsChapter4Reached = false;
                StoryStateManager.LyThongReturnedAsOfficial = false;
                StoryStateManager.IsChapter4Completed = false;
                StoryStateManager.IsChapter5Completed = false;
            }
            return Load(destination);
        }
        public bool StartNewStory()
        {
            if (transitioning || !ValidateScene(Chapter1)) return false;
            StoryStateManager.StartNewGame();
            PlayerPrefs.DeleteKey(CheckpointKey); PlayerPrefs.DeleteKey(FlagsKey); PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save(); SessionActive = true;
            return Load(Chapter1);
        }
        public bool ResumeStory()
        {
            string path = PlayerPrefs.GetString(CheckpointKey, "");
            if (transitioning || !HasCheckpoint || CampaignCompleted || !ValidateScene(path)) return false;
            ApplyFlags(PlayerPrefs.GetInt(FlagsKey, 0)); SessionActive = true;
            return Load(path);
        }
        bool ValidateScene(string path)
        {
            if (Application.CanStreamedLevelBeLoaded(path)) { Error = ""; return true; }
            Error = "Chưa tìm thấy cảnh trong Build Settings: " + path; return false;
        }
        bool Load(string path)
        {
            if (transitioning || !ValidateScene(path)) return false;
            transitioning = true;
            try { SceneManager.LoadScene(path); return true; }
            catch (Exception exception) { transitioning = false; Error = "Chưa chuyển được cảnh: " + exception.Message; return false; }
        }
        static int CaptureFlags() => (StoryStateManager.IsChanTinhDefeated ? 1 : 0) |
            (StoryStateManager.HasChanTinhHead ? 2 : 0) | (StoryStateManager.IsReturningToChapter2 ? 4 : 0) |
            (StoryStateManager.IsReturnDialogueCompleted ? 8 : 0) | (StoryStateManager.IsChapter4Reached ? 16 : 0) |
            (StoryStateManager.LyThongReturnedAsOfficial ? 32 : 0) | (StoryStateManager.IsChapter4Completed ? 64 : 0) |
            (StoryStateManager.IsChapter5Completed ? 128 : 0);
        static void ApplyFlags(int flags)
        {
            StoryStateManager.IsChanTinhDefeated = (flags & 1) != 0;
            StoryStateManager.HasChanTinhHead = (flags & 2) != 0;
            StoryStateManager.IsReturningToChapter2 = (flags & 4) != 0;
            StoryStateManager.IsReturnDialogueCompleted = (flags & 8) != 0;
            StoryStateManager.IsChapter4Reached = (flags & 16) != 0;
            StoryStateManager.LyThongReturnedAsOfficial = (flags & 32) != 0;
            StoryStateManager.IsChapter4Completed = (flags & 64) != 0;
            StoryStateManager.IsChapter5Completed = (flags & 128) != 0;
        }
        static void SaveCheckpoint(string path)
        {
            PlayerPrefs.SetString(CheckpointKey, path); PlayerPrefs.SetInt(FlagsKey, CaptureFlags()); PlayerPrefs.Save();
        }
        void OnGUI()
        {
            bool menu = CurrentPath == MenuScene;
            if (!menu && !registeredReady && GUI.Button(new Rect(Screen.width - 200, 18, 180, 36), "VỀ MENU [ESC]"))
            { Load(MenuScene); return; }
            if (CurrentPath == ReturnHome && !StoryStateManager.LyThongReturnedAsOfficial)
            {
                var home = Chapter2ReturnManager.Instance;
                if (home != null && !home.IsDialogueActive && !StoryStateManager.IsReturnDialogueCompleted)
                {
                    M4QuestDrawing.Objective("CHƯƠNG 3 · TRỞ VỀ BÁO TIN", "Đi vào nhà, đến gần Lý Thông rồi bấm E.",
                        "Mang đầu Chằn Tinh về gặp mẹ con Lý Thông.", "WASD: di chuyển  •  E: nói chuyện  •  Enter / Space / Chuột trái: đọc tiếp");
                    var actor = GameObject.FindWithTag("Player");
                    if (actor != null && home.InteractionTarget != null)
                        M4QuestDrawing.Target(Camera.main, home.InteractionTarget.position, actor.transform.position, "LÝ THÔNG");
                }
                if (home != null && !string.IsNullOrEmpty(home.TransitionError))
                    GUI.Label(new Rect(30, 185, Screen.width - 60, 60), home.TransitionError, M4QuestDrawing.Text(20, Color.yellow));
            }
            if (!menu && !registeredReady && string.IsNullOrEmpty(Error)) return;
            Matrix4x4 old = GUI.matrix;
            float scale = Mathf.Max(.3f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            GUI.depth = -100;
            M4QuestDrawing.Panel(new Rect(0, 0, 1280, 720), new Color(.025f, .055f, .045f, .98f));
            var title = M4QuestDrawing.Text(32, M4QuestDrawing.Gold, true);
            var body = M4QuestDrawing.Text(22, Color.white);
            GUI.Label(new Rect(170, 125, 940, 55), menu ? "THẠCH SANH · HÀNH TRÌNH CHƯƠNG 1–5" : completionTitle, title);
            string text = menu ? "Đốn củi → Gặp Lý Thông → Hạ Chằn Tinh → Lý Thông cướp công → Theo dấu Đại Bàng.\n\nTiếp tục sẽ chơi lại từ đầu cảnh đã lưu gần nhất." : completionText;
            GUI.Label(new Rect(170, 205, 940, 145), text, body);
            var button = new GUIStyle(GUI.skin.button) { fontSize = 22, padding = new RectOffset(12, 12, 12, 12) };
            if (menu)
            {
                if (HasCheckpoint && !CampaignCompleted && GUI.Button(new Rect(170, 385, 440, 58), "TIẾP TỤC [ENTER]", button)) ResumeStory();
                if (GUI.Button(new Rect(170, 465, 440, 58), HasCheckpoint ? "CHƠI LẠI TỪ CHƯƠNG 1 [N]" : "BẮT ĐẦU [ENTER / N]", button)) StartNewStory();
                GUI.Label(new Rect(170, 550, 940, 60), CampaignCompleted ? "Đã hoàn thành chương 1–5. Bạn có thể bắt đầu lượt mới." : "N: bắt đầu lượt mới, xóa tiến trình cốt truyện cũ.\nBấm vào Game để nhận phím.", M4QuestDrawing.Text(18, new Color(.75f, .85f, .78f)));
            }
            else
            {
                GUI.enabled = CanContinue && !transitioning;
                if (GUI.Button(new Rect(170, 405, 600, 65), destination == MenuScene ? "VỀ MÀN HÌNH ĐẦU [ENTER]" : "SANG PHẦN TIẾP THEO [ENTER]", button)) ContinueChapter();
                GUI.enabled = true;
            }
            if (!string.IsNullOrEmpty(Error)) GUI.Label(new Rect(170, 615, 940, 80), Error, M4QuestDrawing.Text(18, new Color(1, .55f, .4f)));
            GUI.matrix = old;
        }
    }
}
