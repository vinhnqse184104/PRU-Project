using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hệ thống tạo và quản lý giao diện hội thoại uGUI Canvas chuẩn hoá đồng bộ theo Chapter 4:
/// - Kích thước khung: 1100 x 215, Bottom-Center, cách đáy 30px
/// - Nền bán trong suốt: RGBA(0.06, 0.08, 0.12, 0.72)
/// - Font: LiberationSans SDF
/// - SpeakerText: Font size 36, Bold, căn trên-trái (35, -18), sizeDelta (-70, 44)
/// - ContentText: Font size 28, Normal, căn trên-trái (35, -68), sizeDelta (-70, 96), TextWrapping Normal
/// - HintText: Font size 20, Italic, căn giữa (0, 14), sizeDelta (-70, 32), màu vàng kim (1, 0.84, 0, 0.95)
/// </summary>
public static class ChapterDialogueUIHelper
{
    public const string FONT_RESOURCE_PATH = "Fonts & Materials/LiberationSans SDF";
    public const string FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    public static TMP_FontAsset GetStandardFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FONT_RESOURCE_PATH);
#if UNITY_EDITOR
        if (font == null)
        {
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        }
#endif
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    public static GameObject EnsureDialogueUI(
        string canvasName,
        out TextMeshProUGUI speakerText,
        out TextMeshProUGUI contentText,
        out TextMeshProUGUI hintText)
    {
        speakerText = null;
        contentText = null;
        hintText = null;

        // 1. Tìm hoặc tạo Canvas
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj == null)
        {
            canvasObj = new GameObject(canvasName);
        }

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        UnityEngine.EventSystems.EventSystem evtSys = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (evtSys == null)
        {
            GameObject evtObj = new GameObject("EventSystem");
            evtObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Tìm hoặc tạo DialoguePanel
        Transform panelTrans = canvasObj.transform.Find("DialoguePanel");
        GameObject panelObj;
        if (panelTrans == null)
        {
            panelObj = new GameObject("DialoguePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
        }
        else
        {
            panelObj = panelTrans.gameObject;
        }

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        if (panelRT == null) panelRT = panelObj.AddComponent<RectTransform>();

        // Căn chỉnh vị trí Bottom-Center (1100 x 215, cách đáy 30px)
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 30f);
        panelRT.sizeDelta = new Vector2(1100f, 215f);

        Image panelImg = panelObj.GetComponent<Image>();
        if (panelImg == null) panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.08f, 0.12f, 0.72f);
        panelImg.raycastTarget = true;

        TMP_FontAsset fontAsset = GetStandardFont();

        // 3. SpeakerText
        Transform speakerTrans = panelObj.transform.Find("SpeakerText");
        GameObject speakerObj;
        if (speakerTrans == null)
        {
            speakerObj = new GameObject("SpeakerText");
            speakerObj.transform.SetParent(panelObj.transform, false);
        }
        else
        {
            speakerObj = speakerTrans.gameObject;
        }

        RectTransform speakerRT = speakerObj.GetComponent<RectTransform>();
        if (speakerRT == null) speakerRT = speakerObj.AddComponent<RectTransform>();
        speakerRT.anchorMin = new Vector2(0f, 1f);
        speakerRT.anchorMax = new Vector2(1f, 1f);
        speakerRT.pivot = new Vector2(0f, 1f);
        speakerRT.anchoredPosition = new Vector2(35f, -18f);
        speakerRT.sizeDelta = new Vector2(-70f, 44f);

        speakerText = speakerObj.GetComponent<TextMeshProUGUI>();
        if (speakerText == null) speakerText = speakerObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) speakerText.font = fontAsset;
        speakerText.fontSize = 36f;
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.alignment = TextAlignmentOptions.TopLeft;

        // 4. ContentText
        Transform contentTrans = panelObj.transform.Find("ContentText");
        GameObject contentObj;
        if (contentTrans == null)
        {
            contentObj = new GameObject("ContentText");
            contentObj.transform.SetParent(panelObj.transform, false);
        }
        else
        {
            contentObj = contentTrans.gameObject;
        }

        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
        if (contentRT == null) contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.anchoredPosition = new Vector2(35f, -68f);
        contentRT.sizeDelta = new Vector2(-70f, 96f);

        contentText = contentObj.GetComponent<TextMeshProUGUI>();
        if (contentText == null) contentText = contentObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) contentText.font = fontAsset;
        contentText.fontSize = 28f;
        contentText.fontStyle = FontStyles.Normal;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.color = Color.white;

        // 5. HintText
        Transform hintTrans = panelObj.transform.Find("HintText");
        GameObject hintObj;
        if (hintTrans == null)
        {
            hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(panelObj.transform, false);
        }
        else
        {
            hintObj = hintTrans.gameObject;
        }

        RectTransform hintRT = hintObj.GetComponent<RectTransform>();
        if (hintRT == null) hintRT = hintObj.AddComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 14f);
        hintRT.sizeDelta = new Vector2(-70f, 32f);

        hintText = hintObj.GetComponent<TextMeshProUGUI>();
        if (hintText == null) hintText = hintObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) hintText.font = fontAsset;
        hintText.fontSize = 20f;
        hintText.fontStyle = FontStyles.Italic;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(1.0f, 0.84f, 0.0f, 0.95f);
        hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";

        panelObj.SetActive(false);
        return panelObj;
    }
}
