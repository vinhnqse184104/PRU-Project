using UnityEngine;
using UnityEngine.SceneManagement;

public class NextChapterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StoryStateManager.StartNewGame();
            SceneManager.LoadScene("Chapter2_LyThong");
        }
    }
}