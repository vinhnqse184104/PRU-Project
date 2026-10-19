using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PRU.M4
{
    public sealed class M4Chapter5Quest : MonoBehaviour
    {
        public enum QuestState { HuntEagle, FollowTrail, AtCave, Completed }
        public M4Chapter5Player player;
        public M4Chapter5Eagle eagle;
        public Transform[] trailPoints;
        public Transform caveEntrance;
        public GameObject[] trailVisuals;
        public float interactionRange = 3;
        public bool useBuiltInInput = true, loadNextScene = true;
        public string nextSceneName = "Chapter6_HangDaiBang";
        public UnityEvent onEagleWounded = new UnityEvent();
        public UnityEvent onClueFound = new UnityEvent();
        public UnityEvent onChapterCompleted = new UnityEvent();
        public UnityEvent onCaveEntered = new UnityEvent();
        public QuestState State { get; private set; }
        public int CluesFound { get; private set; }
        public bool TransitionRequested { get; private set; }
        public string Notice { get; private set; } = "Đại Bàng đang bay trước mặt. Bắn một mũi tên trúng nó để tạo dấu vết.";
        public Transform CurrentTarget => State == QuestState.HuntEagle ? (eagle != null ? eagle.transform : null) :
            State == QuestState.FollowTrail && trailPoints != null && CluesFound < trailPoints.Length ? trailPoints[CluesFound] : caveEntrance;
        public Vector3 Checkpoint => State == QuestState.HuntEagle || CluesFound == 0 ? player.spawnPosition : trailPoints[CluesFound - 1].position + Vector3.up * .1f;
        public bool CanInteract
        {
            get
            {
                if (player == null || CurrentTarget == null || State == QuestState.HuntEagle || TransitionRequested) return false;
                Vector3 offset = CurrentTarget.position - player.transform.position;
                if (Mathf.Abs(offset.y) > 2.5f || new Vector2(offset.x, offset.z).magnitude > interactionRange) return false;
                Vector3 origin = player.transform.position + Vector3.up;
                foreach (var hit in Physics.RaycastAll(origin, CurrentTarget.position + Vector3.up * .4f - origin,
                    Vector3.Distance(origin, CurrentTarget.position + Vector3.up * .4f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(player.transform) && !hit.transform.IsChildOf(CurrentTarget)) return false;
                }
                return true;
            }
        }
        int lastInteractionFrame = -1;
        float enterAllowedAfter;
        void Start() { RefreshVisuals(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void Update()
        {
            if (!useBuiltInInput || Keyboard.current == null) return;
            if (Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
            if (Keyboard.current.f5Key.wasPressedThisFrame) RestartQuest();
        }
        public bool RegisterEagleHit(M4Chapter5Eagle target)
        {
            if (State != QuestState.HuntEagle || target == null || target != eagle) return false;
            if (trailPoints == null || trailPoints.Length == 0) return false;
            State = QuestState.FollowTrail;
            Notice = "Bắn trúng! Đại Bàng đã bỏ chạy. Theo lông vũ và vết máu, bấm E để xem dấu vết.";
            RefreshVisuals(); onEagleWounded.Invoke(); return true;
        }
        public bool TryInteract()
        {
            if (!CanInteract || lastInteractionFrame == Time.frameCount) return false;
            lastInteractionFrame = Time.frameCount;
            if (State == QuestState.FollowTrail)
            {
                CluesFound++;
                State = CluesFound == trailPoints.Length ? QuestState.AtCave : QuestState.FollowTrail;
                Notice = State == QuestState.AtCave ? "Các dấu vết dẫn tới cửa hang phía trước. Đến gần và bấm E để xác nhận." : "Đã tìm thấy dấu vết " + CluesFound + "/" + trailPoints.Length + ". Tiếp tục theo mốc vàng.";
                RefreshVisuals(); onClueFound.Invoke(); return true;
            }
            if (State == QuestState.AtCave)
            {
                State = QuestState.Completed; StoryStateManager.IsChapter5Completed = true;
                Notice = "Đã tìm ra hang Đại Bàng. Chương 5 hoàn thành! Bấm E lần nữa để vào chương 6.";
                enterAllowedAfter = Time.unscaledTime + .5f; onChapterCompleted.Invoke(); return true;
            }
            if (State == QuestState.Completed && Time.unscaledTime >= enterAllowedAfter)
            {
                if (loadNextScene && !Application.CanStreamedLevelBeLoaded(nextSceneName))
                { Notice = "Đã hoàn thành chương 5. Chưa có scene hang trong Build Settings: " + nextSceneName; return false; }
                TransitionRequested = true; onCaveEntered.Invoke();
                if (loadNextScene) SceneManager.LoadScene(nextSceneName);
                return true;
            }
            return false;
        }
        public void RestartQuest()
        {
            State = QuestState.HuntEagle; CluesFound = 0; TransitionRequested = false;
            lastInteractionFrame = -1; enterAllowedAfter = 0;
            StoryStateManager.IsChapter5Completed = false;
            Notice = "Lượt thử mới: bắn trúng Đại Bàng rồi lần theo ba dấu vết tới cửa hang.";
            foreach (var arrow in FindObjectsByType<M4Chapter5Arrow>(FindObjectsSortMode.None)) arrow.Cancel();
            if (eagle != null) eagle.ResetFlight();
            RefreshVisuals(); if (player != null) { player.SetInputLocked(false); player.ResetBow(); player.Respawn(); }
        }
        void RefreshVisuals()
        {
            if (trailVisuals == null) return;
            for (int i = 0; i < trailVisuals.Length; i++) if (trailVisuals[i] != null) trailVisuals[i].SetActive(State != QuestState.HuntEagle && i <= CluesFound);
        }
        void OnGUI()
        {
            string objective = State == QuestState.HuntEagle ? "Bắn trúng Đại Bàng để đánh dấu đường bay." : State == QuestState.FollowTrail ? "Tìm dấu vết tiếp theo và bấm E để xem." : State == QuestState.AtCave ? "Đến cửa hang, bấm E xác nhận nơi ẩn náu." : "ĐÃ HOÀN THÀNH · Bấm E để vào hang.";
            M4QuestDrawing.Objective("CHƯƠNG 5 · THEO DẤU ĐẠI BÀNG", objective, "Dấu vết: " + CluesFound + "/" + (trailPoints != null ? trailPoints.Length : 0),
                "WASD: đi  •  Shift: chạy  •  Space: nhảy  •  Giữ chuột phải: ngắm  •  Chuột trái: bắn  •  E: tương tác  •  R: về mốc  •  F5: thử lại");
            if (player != null && CurrentTarget != null)
                M4QuestDrawing.Target(player.viewCamera, CurrentTarget.position, player.transform.position, State == QuestState.HuntEagle ? "ĐẠI BÀNG" : State == QuestState.FollowTrail ? "DẤU VẾT " + (CluesFound + 1) : "CỬA HANG");
            float width = Mathf.Min(780, Screen.width - 40);
            Rect noticeBox = new Rect((Screen.width - width) / 2, Screen.height - 132, width, 58);
            M4QuestDrawing.Panel(noticeBox, new Color(.025f, .065f, .055f, .93f));
            GUI.Label(new Rect(noticeBox.x + 14, noticeBox.y + 8, width - 28, 49), CanInteract ? "[E] " + Notice : Notice, M4QuestDrawing.Text(16, Color.white));
            if (State == QuestState.HuntEagle)
            {
                GUI.Label(new Rect(Screen.width / 2f - 12, Screen.height / 2f - 18, 30, 30), "+", M4QuestDrawing.Text(24, M4QuestDrawing.Gold, true));
                GUI.Label(new Rect(Screen.width - 190, 28, 170, 28), "Tên đã bắn: " + (player != null ? player.ShotsFired : 0), M4QuestDrawing.Text(16, Color.white));
            }
        }
    }
}
