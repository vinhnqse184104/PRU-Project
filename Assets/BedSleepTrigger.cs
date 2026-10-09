using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BedSleepTrigger : MonoBehaviour
{
    [Header("Sleeping & Scene Settings")]
    public float fadeDuration = 2.0f;
    public string nextSceneName = "Chapter3_MieuChanTinh";

    [Header("UI References")]
    public CanvasGroup fadeCanvasGroup;
    public TMP_Text questText;

    private bool playerInside = false;
    private bool isSleeping = false;

    private void Start()
    {
        EnsureUIOverlay();
    }

    private void EnsureUIOverlay()
    {
        if (fadeCanvasGroup == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("SleepCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Transform fadeObjTr = canvas.transform.Find("SleepFadeOverlay");
            GameObject fadeObj;
            if (fadeObjTr != null)
            {
                fadeObj = fadeObjTr.gameObject;
            }
            else
            {
                fadeObj = new GameObject("SleepFadeOverlay");
                fadeObj.transform.SetParent(canvas.transform, false);
                RectTransform rect = fadeObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.one;

                Image img = fadeObj.AddComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = false;
            }

            fadeCanvasGroup = fadeObj.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null) fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (questText == null)
        {
            QuestManager qm = Object.FindAnyObjectByType<QuestManager>();
            if (qm != null && qm.questText != null)
            {
                questText = qm.questText;
            }
            else
            {
                questText = Object.FindAnyObjectByType<TMP_Text>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSleeping) return;
        if (other.CompareTag("Player") || other.name.ToLower().Contains("thachsan"))
        {
            playerInside = true;
            if (questText != null)
            {
                questText.text = "<color=#FFFF00>Nhấn E để ngủ</color>\n<size=80%>(Bấm phím E để nghỉ ngơi qua đêm)</size>";
                questText.gameObject.SetActive(true);
            }
            Debug.Log("[BedSleepTrigger] Player entered Bed trigger zone");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isSleeping || !playerInside) return;
        if (!other.CompareTag("Player") && !other.name.ToLower().Contains("thachsan")) return;

        bool interactPressed = false;
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            interactPressed = true;
        }
        if (!interactPressed)
        {
            try { interactPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Space); } catch { }
        }

        if (interactPressed)
        {
            StartCoroutine(ExecuteSleepSequence(other.gameObject));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isSleeping) return;
        if (other.CompareTag("Player") || other.name.ToLower().Contains("thachsan"))
        {
            playerInside = false;
        }
    }

    private IEnumerator ExecuteSleepSequence(GameObject playerObj)
    {
        isSleeping = true;
        playerInside = false;

        // Vô hiệu hóa di chuyển nhân vật
        PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        if (questText != null)
        {
            questText.text = "<color=#00FF00>ĐANG ĐI NGỦ...</color>\n<size=80%>(Một đêm trôi qua... Sáng hôm sau tại Miếu Chằn Tinh)</size>";
        }

        EnsureUIOverlay();

        // 1. Màn hình tối dần trong 2 giây
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }
            yield return null;
        }
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1.0f;

        yield return new WaitForSeconds(0.5f);

        // 2. Chuyển sang Chapter 3
        Debug.Log("[BedSleepTrigger] Sleep complete! Loading Chapter3_MieuChanTinh...");
        SceneManager.LoadScene(nextSceneName);
    }
}
