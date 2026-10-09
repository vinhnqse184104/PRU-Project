using System;
using System.Collections;
using UnityEngine;
using TMPro;

[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 5)] public string text;
}

[DefaultExecutionOrder(-100)]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject panel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public float typeSpeed = 0.03f;

    [Tooltip("Các script bị tắt khi đang nói chuyện (PlayerMovement, MouseLook, PlayerChopper)")]
    public MonoBehaviour[] lockedWhileTalking;

    public bool IsTalking { get; private set; }

    DialogueLine[] lines;
    int index;
    bool typing;
    Action onEnd;
    Coroutine typeRoutine;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void StartDialogue(DialogueLine[] newLines, Action onFinished = null)
    {
        if (IsTalking || newLines == null || newLines.Length == 0) return;

        lines = newLines;
        onEnd = onFinished;
        index = 0;
        IsTalking = true;

        foreach (var s in lockedWhileTalking) if (s) s.enabled = false;
        if (BGMManager.Instance) BGMManager.Instance.Duck(true);

        panel.SetActive(true);
        ShowLine();
    }

    void Update()
    {
        if (!IsTalking) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Next();
    }

    void ShowLine()
    {
        DialogueLine line = lines[index];
        speakerText.text = line.speaker;
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string full)
    {
        typing = true;
        bodyText.text = "";
        foreach (char c in full)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        typing = false;
    }

    void Next()
    {
        if (typing)
        {
            // đang chạy chữ: hiện đủ câu ngay
            StopCoroutine(typeRoutine);
            bodyText.text = lines[index].text;
            typing = false;
            return;
        }

        index++;
        if (index >= lines.Length) EndDialogue();
        else ShowLine();
    }

    void EndDialogue()
    {
        panel.SetActive(false);
        IsTalking = false;

        foreach (var s in lockedWhileTalking) if (s) s.enabled = true;
        if (BGMManager.Instance) BGMManager.Instance.Duck(false);

        onEnd?.Invoke();
    }
}