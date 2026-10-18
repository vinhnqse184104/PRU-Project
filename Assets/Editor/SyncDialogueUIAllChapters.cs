#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public class SyncDialogueUIAllChapters
{
    private const string SYNC_SESSION_KEY = "PRU_DialogueUISynced_Auto_v1";
    private const string CH1_SCENE_PATH = "Assets/Scenes/RungThachSanh.unity";
    private const string CH2_SCENE_PATH = "Assets/Scenes/Chapter2_LyThong.unity";
    private const string CH3_SCENE_PATH = "Assets/Scenes/Chapter3_MieuChanTinh.unity";

    static SyncDialogueUIAllChapters()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(SYNC_SESSION_KEY, false))
            {
                SessionState.SetBool(SYNC_SESSION_KEY, true);
                ExecuteSync(false);
            }
        };
    }

    [MenuItem("Tools/PRU/Đồng bộ Dialogue UI Chapter 1, 2, 3 (Chuẩn Chapter 4)")]
    public static void SyncAllChaptersMenu()
    {
        ExecuteSync(true);
    }

    public static void ExecuteSync(bool showDialog)
    {
        Debug.Log("[SyncDialogueUIAllChapters] Bắt đầu đồng bộ giao diện Dialogue UI Chapter 1, 2, 3 theo chuẩn Chapter 4...");

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        try
        {
            // 1. CHAPTER 1: RungThachSanh
            SyncChapter1();

            // 2. CHAPTER 2: Chapter2_LyThong
            SyncChapter2();

            // 3. CHAPTER 3: Chapter3_MieuChanTinh
            SyncChapter3();

            AssetDatabase.SaveAssets();

            Debug.Log("[SyncDialogueUIAllChapters] HOÀN TẤT ĐỒNG BỘ DIALOGUE UI TẤT CẢ CHAPTER (1, 2, 3) THEO CHUẨN CHAPTER 4 THÀNH CÔNG!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Đồng bộ thành công",
                    "Đã đồng bộ hoàn tất giao diện hội thoại (Dialogue UI) của Chapter 1, 2, 3 giống 100% Chapter 4:\n\n" +
                    "- Vị trí: Bottom-Center, cách đáy 30px\n" +
                    "- Kích thước khung: 1100 x 215 pixel\n" +
                    "- Màu nền: Slate đen mờ (alpha 0.72)\n" +
                    "- Font: LiberationSans SDF\n" +
                    "- Tên nhân vật (SpeakerText): Font size 36, Bold\n" +
                    "- Nội dung thoại (ContentText): Font size 28, Normal, Auto-wrap\n" +
                    "- Dòng hướng dẫn (HintText): Font size 20, Italic, Vàng kim\n" +
                    "- CanvasScaler: 1920x1080 (Match 0.5)",
                    "Đóng");
            }
        }
        finally
        {
            // Trả về Scene ban đầu nếu khác
            if (!string.IsNullOrEmpty(originalScenePath) && EditorSceneManager.GetActiveScene().path != originalScenePath)
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }
    }

    private static void SyncChapter1()
    {
        var scene = EditorSceneManager.OpenScene(CH1_SCENE_PATH, OpenSceneMode.Single);
        GameObject panel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter1_DialogueCanvas", out var speakerTMP, out var contentTMP, out var hintTMP);

        QuestManager qm = Object.FindAnyObjectByType<QuestManager>();
        if (qm != null)
        {
            Undo.RecordObject(qm, "Gán Dialogue UI cho QuestManager");
            qm.dialoguePanel = panel;
            qm.speakerText = speakerTMP;
            qm.contentText = contentTMP;
            qm.hintText = hintTMP;
            EditorUtility.SetDirty(qm);
            Debug.Log("[SyncDialogueUIAllChapters] Chapter 1: Đã liên kết DialoguePanel vào QuestManager.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SyncChapter2()
    {
        var scene = EditorSceneManager.OpenScene(CH2_SCENE_PATH, OpenSceneMode.Single);
        GameObject panel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter2_DialogueCanvas", out var speakerTMP, out var contentTMP, out var hintTMP);

        LyThongGuide ltg = Object.FindAnyObjectByType<LyThongGuide>();
        if (ltg != null)
        {
            Undo.RecordObject(ltg, "Gán Dialogue UI cho LyThongGuide");
            ltg.dialoguePanel = panel;
            ltg.speakerText = speakerTMP;
            ltg.contentText = contentTMP;
            ltg.hintText = hintTMP;
            EditorUtility.SetDirty(ltg);
            Debug.Log("[SyncDialogueUIAllChapters] Chapter 2: Đã liên kết DialoguePanel vào LyThongGuide.");
        }

        Chapter2ReturnManager c2rm = Object.FindAnyObjectByType<Chapter2ReturnManager>();
        if (c2rm != null)
        {
            Undo.RecordObject(c2rm, "Gán Dialogue UI cho Chapter2ReturnManager");
            c2rm.dialoguePanel = panel;
            c2rm.speakerText = speakerTMP;
            c2rm.contentText = contentTMP;
            c2rm.hintText = hintTMP;
            EditorUtility.SetDirty(c2rm);
            Debug.Log("[SyncDialogueUIAllChapters] Chapter 2: Đã liên kết DialoguePanel vào Chapter2ReturnManager.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SyncChapter3()
    {
        var scene = EditorSceneManager.OpenScene(CH3_SCENE_PATH, OpenSceneMode.Single);
        GameObject panel = ChapterDialogueUIHelper.EnsureDialogueUI("Chapter3_DialogueCanvas", out var speakerTMP, out var contentTMP, out var hintTMP);

        Chapter3Intro c3i = Object.FindAnyObjectByType<Chapter3Intro>();
        if (c3i != null)
        {
            Undo.RecordObject(c3i, "Gán Dialogue UI cho Chapter3Intro");
            c3i.dialoguePanel = panel;
            c3i.speakerText = speakerTMP;
            c3i.contentText = contentTMP;
            c3i.hintText = hintTMP;
            EditorUtility.SetDirty(c3i);
            Debug.Log("[SyncDialogueUIAllChapters] Chapter 3: Đã liên kết DialoguePanel vào Chapter3Intro.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
