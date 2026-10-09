using System;
using UnityEngine;
[DefaultExecutionOrder(-100)]

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public int targetFirewood = 10;
    public int currentFirewood = 0;
    public bool isCompleted = false;

    public event Action<int, int> OnFirewoodChanged; // (hiện tại, mục tiêu)
    public event Action OnQuestCompleted;

    void Awake() => Instance = this;

    void Start() => OnFirewoodChanged?.Invoke(currentFirewood, targetFirewood);

    public void AddFirewood(int amount)
    {
        if (isCompleted) return;

        currentFirewood = Mathf.Min(currentFirewood + amount, targetFirewood);
        OnFirewoodChanged?.Invoke(currentFirewood, targetFirewood);

        if (currentFirewood >= targetFirewood)
        {
            isCompleted = true;
            OnQuestCompleted?.Invoke();
        }
    }
}