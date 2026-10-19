using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PRU.M4;

public static class M4StoryMenu
{
    [MenuItem("Thach Sanh/M4/Mo hanh trinh chuong 1-5")]
    public static void OpenStory()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(M4StoryFlow.MenuScene);
    }
}
