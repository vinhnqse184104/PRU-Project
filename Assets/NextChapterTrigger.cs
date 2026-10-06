using UnityEngine;
using UnityEngine.SceneManagement;

public class NextChapterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("Chapter2_LyThong");
        }
    }
}