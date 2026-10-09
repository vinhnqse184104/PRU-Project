using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public TextMeshProUGUI firewoodText;
    public GameObject completePanel;
    public AudioClip completeSound;
    public AudioSource uiAudio;

    void OnEnable()
    {
        QuestManager.Instance.OnFirewoodChanged += UpdateText;
        QuestManager.Instance.OnQuestCompleted += ShowComplete;
    }

    void OnDisable()
    {
        QuestManager.Instance.OnFirewoodChanged -= UpdateText;
        QuestManager.Instance.OnQuestCompleted -= ShowComplete;
    }

    void UpdateText(int current, int target)
    {
        firewoodText.text = $"Thu thập củi: {current}/{target}";
    }

    void ShowComplete()
    {
        completePanel.SetActive(true);
        uiAudio.PlayOneShot(completeSound);
        // Tuỳ chọn: ẩn panel sau 3 giây
        Invoke(nameof(HidePanel), 3f);
    }

    void HidePanel() => completePanel.SetActive(false);
}