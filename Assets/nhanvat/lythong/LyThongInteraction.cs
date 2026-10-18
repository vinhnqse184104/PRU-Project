using System.Collections;
using UnityEngine;

public class LyThongInteraction : MonoBehaviour
{
    [Header("Quest Manager")]
    public QuestManager questManager;

    [Header("Standing Model Reference")]
    public GameObject standingPrefab;
    public RuntimeAnimatorController standingController;
    public Material lyThongMaterial;

    [Header("Interaction Settings")]
    public float triggerDistance = 3.0f;
    public float standUpDuration = 1.6f;

    private GameObject standingInstance;
    private Animator standingAnimator;
    private bool isMet = false;
    private bool isStanding = false;
    private Transform playerTransform;

    void Awake()
    {
#if UNITY_EDITOR
        if (standingPrefab == null)
            standingPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/nhanvat/lythong/dung/Meshy_AI_The_Wayfarer_Sage_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin.fbx");
        if (standingController == null)
            standingController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/nhanvat/lythong/LyThongChapter1Anim.controller");
        if (lyThongMaterial == null)
            lyThongMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/nhanvat/lythong/dung/maulythong.mat");
#endif
        EnsureStandingInstance();
    }

    void Start()
    {
        if (questManager == null)
        {
            questManager = Object.FindAnyObjectByType<QuestManager>();
        }

        FindPlayer();
        EnsureStandingInstance();
    }

    void OnEnable()
    {
        FindPlayer();
        EnsureStandingInstance();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("thachsanh") ?? GameObject.Find("ThachSanh");
            if (p != null) playerTransform = p.transform;
        }
    }

    private void EnsureStandingInstance()
    {
        if (standingInstance == null && standingPrefab != null)
        {
            standingInstance = Instantiate(standingPrefab, transform);
            standingInstance.name = "LyThong_StandingModel";
            standingInstance.transform.localPosition = Vector3.zero;
            standingInstance.transform.localRotation = Quaternion.identity;
            standingInstance.transform.localScale = Vector3.one;

            standingAnimator = standingInstance.GetComponent<Animator>();
            if (standingAnimator == null) standingAnimator = standingInstance.AddComponent<Animator>();
            standingAnimator.applyRootMotion = false;
            if (standingAnimator.avatar == null && standingPrefab != null)
            {
                Animator pAnim = standingPrefab.GetComponent<Animator>();
                if (pAnim != null) standingAnimator.avatar = pAnim.avatar;
            }
            if (standingController != null) standingAnimator.runtimeAnimatorController = standingController;

            if (lyThongMaterial != null)
            {
                Renderer[] rends = standingInstance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.material = lyThongMaterial;
                }
            }

            standingInstance.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isMet)
        {
            isMet = true;
            playerTransform = other.transform;
            StartCoroutine(WakeUpSequence());
        }
    }

    void Update()
    {
        // Kiểm tra khoảng cách bổ sung phòng trường hợp Collider bị miss
        if (!isMet && playerTransform != null && gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= triggerDistance)
            {
                isMet = true;
                StartCoroutine(WakeUpSequence());
            }
        }
    }

    private IEnumerator WakeUpSequence()
    {
        FindPlayer();

        // Tạm dừng 0.25s tạo nhịp phản ứng tự nhiên
        yield return new WaitForSeconds(0.25f);

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 lookDir = playerTransform != null ? (playerTransform.position - startPos) : transform.forward;
        lookDir.y = 0;
        Quaternion targetRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir.normalized) : startRot;

        float groundHeight = startPos.y;
        if (Terrain.activeTerrain != null)
        {
            groundHeight = Terrain.activeTerrain.SampleHeight(startPos) + Terrain.activeTerrain.transform.position.y;
        }
        else if (Physics.Raycast(startPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
        {
            groundHeight = hit.point.y;
        }
        Vector3 targetStandingPos = new Vector3(startPos.x, groundHeight + 0.02f, startPos.z);

        // Chuỗi chuyển động đứng dậy bằng code: nhấc người, xoay mặt về phía Thạch Sanh
        float elapsed = 0f;
        while (elapsed < standUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / standUpDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Nâng độ cao lên mặt đất
            transform.position = Vector3.Lerp(startPos, targetStandingPos, smoothT);

            // Xoay dần về phía Thạch Sanh, đồng thời ngả người thẳng đứng dậy
            float tiltAngle = Mathf.Sin(t * Mathf.PI) * -18f;
            Quaternion facingRot = Quaternion.Slerp(startRot, targetRot, smoothT);
            transform.rotation = facingRot * Quaternion.Euler(tiltAngle, 0f, 0f);

            yield return null;
        }

        transform.position = targetStandingPos;
        transform.rotation = targetRot;

        // Chuyển sang mô hình đứng (Standing Idle)
        SwitchToStandingModel();
        isStanding = true;

        // Điều chỉnh CapsuleCollider thành kích thước người đứng chuẩn
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.height = 1.8f;
            col.center = new Vector3(0, 0.9f, 0);
            col.radius = 0.35f;
        }

        // Tắt SphereCollider trigger để tránh kích hoạt lại
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null) sc.enabled = false;

        // Chờ 0.2s để tư thế đứng hoàn toàn ổn định trước khi bắt đầu hiện câu thoại đầu tiên
        yield return new WaitForSeconds(0.2f);

        // Kích hoạt hội thoại
        if (questManager != null)
        {
            if (standingAnimator != null) questManager.lyThongAnimator = standingAnimator;
            questManager.MeetLyThong();
        }
    }

    private void SwitchToStandingModel()
    {
        EnsureStandingInstance();

        // Ẩn mesh của mô hình nằm
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (standingInstance != null && r.transform.IsChildOf(standingInstance.transform)) continue;
            r.enabled = false;
        }

        // Tắt Animator cũ của mô hình nằm
        Animator rootAnim = GetComponent<Animator>();
        if (rootAnim != null) rootAnim.enabled = false;

        // Bật mô hình đứng và kích hoạt animation Standing Idle
        if (standingInstance != null)
        {
            standingInstance.transform.localPosition = Vector3.zero;
            standingInstance.transform.localRotation = Quaternion.identity;
            standingInstance.SetActive(true);

            if (standingAnimator != null)
            {
                standingAnimator.enabled = true;
                standingAnimator.applyRootMotion = false;
                standingAnimator.Play("đứng", 0, 0f);
                standingAnimator.SetTrigger("StandUp");
            }
        }
    }

    void LateUpdate()
    {
        // Khi đã đứng dậy, giữ mặt Lý Thông luôn hướng về phía Thạch Sanh trong suốt cuộc hội thoại
        if (isStanding && playerTransform != null)
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 4f);
            }
        }
    }
}