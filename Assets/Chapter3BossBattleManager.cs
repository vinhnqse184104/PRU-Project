using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class Chapter3BossBattleManager : MonoBehaviour
{
    // Shared only by Chapter 3 damage handlers. Sweep the whole body, then
    // require cave floor under the leading footprint (Terrain is not cave floor).
    public static Vector3 LimitKnockback(Collider body, Vector3 motion)
    {
        if (body == null || motion.sqrMagnitude == 0f) return Vector3.zero;
        Bounds bounds = body.bounds;
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z));
        Vector3 center = bounds.center;
        float halfLine = Mathf.Max(0f, bounds.extents.y - radius);
        float distance = motion.magnitude;
        Vector3 direction = motion / distance;
        foreach (RaycastHit hit in Physics.CapsuleCastAll(center + Vector3.up * halfLine,
                     center - Vector3.up * halfLine, radius, direction, distance + 0.03f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root == body.transform.root || IsCaveFloor(hit.collider) || hit.normal.y > 0.5f) continue;
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.03f));
        }
        Vector3 accepted = Vector3.zero;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * radius * 0.7f;
        for (float step = 0f; step < distance;)
        {
            step = Mathf.Min(distance, step + 0.1f);
            Vector3 front = center + direction * (step + radius);
            if (!HasCaveFloor(front, bounds.min.y) ||
                !HasCaveFloor(front + side, bounds.min.y) ||
                !HasCaveFloor(front - side, bounds.min.y)) break;
            accepted = direction * step;
        }
        return accepted;
    }

    private static bool HasCaveFloor(Vector3 point, float feetY)
    {
        point.y = feetY + 0.5f;
        foreach (RaycastHit hit in Physics.RaycastAll(point, Vector3.down, 10f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < 0.5f) continue;
            if (IsCaveFloor(hit.collider)) return true;
        }
        return false;
    }

    private static bool IsCaveFloor(Collider collider)
    {
        for (Transform parent = collider.transform; parent != null; parent = parent.parent)
            if (parent.name == "CaveFloor" || parent.name == "CaveGround") return true;
        return false;
    }

    public static Chapter3BossBattleManager Instance { get; private set; }

    [Header("Cài đặt")]
    public float delayBeforeReturn = 5.0f;
    public string returnSceneName = "Chapter2_LyThong";

    [Header("UI Tùy chọn (Nếu để trống sẽ tự động hiển thị GUI)")]
    public TextMeshProUGUI victoryText;
    public CanvasGroup fadeCanvasGroup;

    private bool isBossDead = false;
    private bool isTransitioning = false;
    private float returnTimer = 0f;
    private string victoryMessage = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        // Đảm bảo Thạch Sanh có PlayerTrophyHolder
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh");
        if (player != null && player.GetComponent<PlayerTrophyHolder>() == null)
        {
            player.AddComponent<PlayerTrophyHolder>();
        }
    }

    /// <summary>
    /// Được gọi từ MauChanTinh khi boss hết máu và bắt đầu hoạt ảnh chết.
    /// </summary>
    public void OnBossDefeated(GameObject bossObject)
    {
        if (isBossDead) return;
        isBossDead = true;

        Debug.Log("[Chapter3BossBattleManager] Xác nhận Chằn Tinh đã bị tiêu diệt!");

        // 1. Cập nhật trạng thái cốt truyện bền vững
        StoryStateManager.IsChanTinhDefeated = true;
        StoryStateManager.HasChanTinhHead = true;
        StoryStateManager.IsReturningToChapter2 = true;

        // 2. Trao chiến lợi phẩm Đầu Chằn Tinh cho Thạch Sanh
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh");
        if (player != null)
        {
            PlayerTrophyHolder trophyHolder = player.GetComponent<PlayerTrophyHolder>();
            if (trophyHolder == null) trophyHolder = player.AddComponent<PlayerTrophyHolder>();
            trophyHolder.AttachTrophy();
        }

        // 3. Khởi chạy chuỗi hoàn thành nhiệm vụ và đếm ngược 5 giây
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        victoryMessage = "<color=#00FF66>THẠCH SANH ĐÃ TIÊU DIỆT CHẰN TINH!</color>\n<size=75%><color=#FFD700>Đã thu được chiến lợi phẩm: Đầu Chằn Tinh</color></size>";
        if (victoryText != null)
        {
            victoryText.text = victoryMessage;
            victoryText.gameObject.SetActive(true);
        }

        returnTimer = delayBeforeReturn;

        while (returnTimer > 0f)
        {
            returnTimer -= Time.deltaTime;
            yield return null;
        }

        // 4. Bắt đầu chuyển cảnh về Chapter 2
        StartCoroutine(TransitionToChapter2());
    }

    private IEnumerator TransitionToChapter2()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        EnsureFadeOverlay();

        float fadeDuration = 1.5f;
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
        yield return new WaitForSeconds(0.3f);

        Debug.Log("[Chapter3BossBattleManager] Chuyển cảnh về " + returnSceneName);
        SceneManager.LoadScene(returnSceneName);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeCanvasGroup != null) return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("VictoryCanvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cObj.AddComponent<CanvasScaler>();
            cObj.AddComponent<GraphicRaycaster>();
        }

        GameObject fadeObj = new GameObject("SceneFadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        RectTransform rect = fadeObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.one;

        Image img = fadeObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
    }

    void OnGUI()
    {
        if (isBossDead && !string.IsNullOrEmpty(victoryMessage))
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 22;
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            style.richText = true;

            float width = Screen.width * 0.75f;
            float height = 130f;
            float left = (Screen.width - width) / 2f;
            float top = 40f;

            string displayStr = victoryMessage;
            if (returnTimer > 0f)
            {
                displayStr += $"\n<size=15><color=#CCCCCC>Đang trở về nhà Lý Thông sau {Mathf.CeilToInt(returnTimer)}s...</color></size>";
            }

            GUI.Box(new Rect(left, top, width, height), displayStr, style);
        }
    }
}
