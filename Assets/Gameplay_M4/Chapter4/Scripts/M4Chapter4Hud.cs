using UnityEngine;
using UnityEngine.SceneManagement;

namespace PRU.M4
{
    // Adds instructions to the shared palace scene without replacing its authored actors/cameras.
    public sealed class M4Chapter4Hud : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapCurrentScene()
        {
            // sceneLoaded is skipped for the initial Editor scene when scene reload is disabled.
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Chapter2_LyThong" && StoryStateManager.LyThongReturnedAsOfficial && !StoryStateManager.IsChapter4Completed)
            {
                foreach (var root in scene.GetRootGameObjects()) if (root.GetComponent<M4Chapter4ReturnHud>() != null) return;
                new GameObject("M4_Chapter4_ReturnHUD").AddComponent<M4Chapter4ReturnHud>();
                return;
            }
            if (scene.name != "Chapter4_LythongCuopCong") return;
            foreach (var root in scene.GetRootGameObjects()) if (root.GetComponent<M4Chapter4Hud>() != null) return;
            var go = new GameObject("M4_Chapter4_QuestHUD");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<M4Chapter4Hud>();
        }

        Chapter4SoldierEscort escort;
        void OnGUI()
        {
            if (escort == null) escort = FindFirstObjectByType<Chapter4SoldierEscort>();
            if (escort == null) return;
            string objective, help;
            Transform target = null;
            switch (escort.currentState)
            {
                case Chapter4SoldierEscort.EscortState.AwaitingInteraction:
                    objective = escort.CanStartEscort ? "Bấm E để nói với lính và vào chầu." : "Đến gần lính hộ tống để xin vào chầu.";
                    help = "WASD: di chuyển  •  Rê chuột: nhìn quanh  •  E: nói với lính";
                    target = escort.escortSoldier;
                    break;
                case Chapter4SoldierEscort.EscortState.Escorting:
                    objective = "Theo lính tới đại điện — nhân vật đang tự đi.";
                    help = "Đợi lính dẫn đường. Tới nơi sẽ bắt đầu hội thoại với vua.";
                    if (escort.waypoints != null && escort.currentWaypointIndex < escort.waypoints.Length)
                        target = escort.waypoints[escort.currentWaypointIndex];
                    break;
                case Chapter4SoldierEscort.EscortState.Completed:
                    objective = string.IsNullOrEmpty(escort.transitionError) ? "Đã báo công. Đang trở về gặp mẹ Lý Thông…" : escort.transitionError;
                    help = "Tiếp theo: hội thoại tại nhà → chương 5.";
                    break;
                default:
                    objective = "Lý Thông nhận công diệt Chằn Tinh trước nhà vua.";
                    help = "Enter / Space / Chuột trái: tiếp tục hội thoại";
                    break;
            }
            M4QuestDrawing.Objective("CHƯƠNG 4 · LÝ THÔNG CƯỚP CÔNG", objective, "Mục tiêu: báo công → nhận thưởng → trở về", help);
            if (target != null && escort.lyThong != null)
                M4QuestDrawing.Target(Camera.main, target.position, escort.lyThong.position, escort.currentState == Chapter4SoldierEscort.EscortState.AwaitingInteraction ? "LÍNH HỘ TỐNG" : "ĐẠI ĐIỆN");
        }
    }

    public sealed class M4Chapter4ReturnHud : MonoBehaviour
    {
        void OnGUI()
        {
            var cutscene = Chapter2OfficialReturnCutscene.Instance;
            if (cutscene == null || !cutscene.enabled) return;
            M4QuestDrawing.Objective("CHƯƠNG 4 · LÝ THÔNG TRỞ VỀ",
                !string.IsNullOrEmpty(cutscene.transitionError) ? cutscene.transitionError : cutscene.IsCompleted ? "Hoàn thành chương 4. Chuẩn bị sang chương 5…" : "Theo dõi Lý Thông khoe công với mẹ.",
                "Nhận công tại cung điện → trở về nhà", "Enter / Space / Chuột trái: tiếp tục hội thoại");
        }
    }

    public static class M4QuestDrawing
    {
        public static readonly Color Gold = new Color(1f, .82f, .33f);
        public static GUIStyle Text(int size, Color color, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, normal = { textColor = color } };
        }
        public static void Panel(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
        public static void Objective(string title, string objective, string detail, string controls)
        {
            float scale = Mathf.Max(.5f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            Panel(new Rect(20, 20, 410, 150), new Color(.025f, .065f, .055f, .94f));
            Panel(new Rect(20, 20, 4, 150), Gold);
            GUI.Label(new Rect(38, 34, 375, 35), title, Text(20, Gold, true));
            GUI.Label(new Rect(38, 76, 370, 48), objective, Text(18, Color.white, true));
            GUI.Label(new Rect(38, 133, 370, 32), detail, Text(14, new Color(.74f, .84f, .78f)));
            Panel(new Rect(20, height - 57, width - 40, 37), new Color(.025f, .065f, .055f, .9f));
            GUI.Label(new Rect(34, height - 50, width - 66, 30), controls, Text(15, Color.white));
            GUI.matrix = old;
        }
        public static void Target(Camera camera, Vector3 target, Vector3 player, string label)
        {
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(target + Vector3.up * 2.6f);
            float distance = Vector3.Distance(player, target);
            var style = Text(17, Gold, true); style.alignment = TextAnchor.MiddleCenter;
            if (screen.z > 0 && screen.x > 50 && screen.x < Screen.width - 50 && screen.y > 80 && screen.y < Screen.height - 80)
            {
                Rect box = new Rect(screen.x - 115, Screen.height - screen.y - 20, 230, 48);
                Panel(box, new Color(.025f, .055f, .04f, .85f));
                GUI.Label(box, label + "  ·  " + distance.ToString("0") + " m\n▼", style);
            }
            else
            {
                Vector3 local = camera.transform.InverseTransformPoint(target);
                string direction = local.z < 0 ? "phía sau" : local.x < 0 ? "bên trái" : "bên phải";
                GUI.Label(new Rect(Screen.width / 2f - 190, 24, 380, 45), label + " ở " + direction + " · " + distance.ToString("0") + " m", style);
            }
        }
    }
}
