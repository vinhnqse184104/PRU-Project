using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Tree : MonoBehaviour
{
    [Header("Chặt cây")]
    public int hitsToFell = 5;
    public int firewoodDrop = 2;
    public AudioClip chopSound;

    [Header("Vòng tiến độ")]
    public float ringDiameter = 0.9f;                 // đường kính vòng (mét)
    public Vector3 centerOffset = Vector3.zero;       // tinh chỉnh vị trí so với tâm cây
    public Color ringColor = new Color(1f, 0.6f, 0.1f);
    public float recoverDelay = 3f;                   // số giây không chặt thì bắt đầu hồi
    public float recoverSpeed = 1f;                   // hồi bao nhiêu nhát mỗi giây

    float progress;
    float lastChopTime;
    bool felled;

    AudioSource audioSource;
    Camera cam;
    GameObject barRoot;
    Image fillImage;
    Vector3 localCenter;   // tâm cây, tính theo Renderer

    static Sprite ringSprite;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        CalculateCenter();
        CreateRing();
    }

    void Start() => cam = Camera.main;

    public void Chop()
    {
        if (felled) return;

        progress += 1f;
        lastChopTime = Time.time;
        if (chopSound) audioSource.PlayOneShot(chopSound);

        if (progress >= hitsToFell)
        {
            felled = true;
            progress = hitsToFell;
            UpdateRing();
            QuestManager.Instance.AddFirewood(firewoodDrop);
            Destroy(gameObject, 0.5f);
        }
    }

    void Update()
    {
        if (felled) return;

        if (progress > 0f && Time.time - lastChopTime > recoverDelay)
            progress = Mathf.MoveTowards(progress, 0f, recoverSpeed * Time.deltaTime);

        UpdateRing();
    }

    void UpdateRing()
    {
        bool show = progress > 0.001f;
        if (barRoot.activeSelf != show) barRoot.SetActive(show);
        if (!show) return;

        fillImage.fillAmount = progress / hitsToFell;

        // Vòng nằm giữa cây, luôn quay về phía camera
        barRoot.transform.position = transform.TransformPoint(localCenter) + centerOffset;
        if (cam) barRoot.transform.rotation = cam.transform.rotation;
    }

    // Tâm thân cây = tâm của toàn bộ Renderer
    void CalculateCenter()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            localCenter = Vector3.up * 1.5f;
            return;
        }

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        localCenter = transform.InverseTransformPoint(b.center);
    }

    void CreateRing()
    {
        if (ringSprite == null) ringSprite = MakeRingSprite(128, 0.72f);

        // Canvas world space, không làm con của cây để không bị méo theo scale cây
        barRoot = new GameObject("ChopProgressRing", typeof(Canvas));
        barRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var rootRect = barRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = Vector2.one * ringDiameter * 100f;
        rootRect.localScale = Vector3.one * 0.01f;

        // Material luôn vẽ đè lên trên, để vòng không bị thân cây che khuất
        var mat = new Material(Canvas.GetDefaultCanvasMaterial());
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);

        // Vòng nền
        var bg = new GameObject("BG", typeof(Image));
        bg.transform.SetParent(barRoot.transform, false);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = ringSprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        bgImg.material = mat;
        Stretch(bg.GetComponent<RectTransform>());

        // Vòng tiến độ
        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(barRoot.transform, false);
        fillImage = fill.GetComponent<Image>();
        fillImage.sprite = ringSprite;
        fillImage.color = ringColor;
        fillImage.material = mat;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = 0f;
        Stretch(fill.GetComponent<RectTransform>());

        barRoot.SetActive(false);
    }

    // Vẽ sprite hình vòng (rỗng ở giữa) bằng code
    static Sprite MakeRingSprite(int size, float inner)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01((1f - d) * half) * Mathf.Clamp01((d - inner) * half);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (barRoot) Destroy(barRoot);
    }
}