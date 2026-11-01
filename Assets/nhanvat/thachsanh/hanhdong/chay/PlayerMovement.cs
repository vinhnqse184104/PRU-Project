using UnityEngine;
using UnityEngine.InputSystem; // Đang dùng New Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;

    [Header("Combat Settings")]
    public Transform attackPoint; // Kéo thả AttackPoint vào đây
    public float attackRange = 1.5f; // Bán kính tầm đánh
    public int attackDamage = 35; // Sát thương mỗi nhát chém/đấm
    public int kickDamage = 25;   // Lực đá (sát thương thấp hơn chém)

    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;
    private Camera mainCam;
    private bool wasGroundedLastFrame = true;
    private float jumpCooldownTimer = 0f; // Thời gian chống nhận nhầm chạm đất và chống spam nhảy (Grace period)

    [Header("Âm thanh Vũ khí")]
    public AudioSource loaVuKhi;
    public AudioClip tiengChemQuai;
    public AudioClip tiengChatCay;

    [Header("Âm thanh Di chuyển")]
    public AudioSource loaDiChuyen;      // Loa riêng gắn dưới chân Thạch Sanh
    public AudioClip tiengBuocChan;      // File âm thanh tiếng chạy (lộp cộp, xào xạc)

    void Start()
    {
        controller = GetComponent<CharacterController>();

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        GameObject spawnPoint = null;

        if (currentScene == "Chapter2_LyThong")
        {
            if (StoryStateManager.IsChanTinhDefeated && StoryStateManager.IsReturningToChapter2)
            {
                // GIAI ĐOẠN 2: QUAY VỀ NHÀ LÝ THÔNG SAU KHI TIÊU DIỆT CHẰN TINH
                spawnPoint = GameObject.Find("Chapter2_ReturnSpawnPoint") ?? GameObject.Find("ReturnSpawnPoint");
                if (spawnPoint == null)
                {
                    GameObject spObj = new GameObject("Chapter2_ReturnSpawnPoint");
                    Vector3 outdoorPos = new Vector3(81.98f, 0.25f, 36.5f);
                    if (Terrain.activeTerrain != null)
                    {
                        outdoorPos.y = Terrain.activeTerrain.SampleHeight(outdoorPos) + Terrain.activeTerrain.transform.position.y + 0.05f;
                    }
                    spObj.transform.position = outdoorPos;
                    spObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    spawnPoint = spObj;
                }

                if (Chapter2ReturnManager.Instance == null && Object.FindAnyObjectByType<Chapter2ReturnManager>() == null)
                {
                    GameObject retManagerObj = new GameObject("Chapter2ReturnManager");
                    retManagerObj.AddComponent<Chapter2ReturnManager>();
                }
            }
            else
            {
                // GIAI ĐOẠN 1: LẦN ĐẦU ĐẾN CHAPTER 2 (VỊ TRÍ THAM CHIẾU CHUẨN X=97.64, Y=0.33, Z=89.05)
                Vector3 startPos = new Vector3(97.64f, 0.33f, 89.05f);
                float groundHeight = startPos.y;
                if (Terrain.activeTerrain != null)
                {
                    groundHeight = Terrain.activeTerrain.SampleHeight(startPos) + Terrain.activeTerrain.transform.position.y;
                    if (groundHeight > startPos.y) startPos.y = groundHeight + 0.05f;
                }
                else if (Physics.Raycast(new Vector3(startPos.x, startPos.y + 2f, startPos.z), Vector3.down, out RaycastHit hit, 5f))
                {
                    groundHeight = hit.point.y;
                    startPos.y = hit.point.y + 0.05f;
                }

                if (controller != null) controller.enabled = false;
                transform.position = startPos;
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.position = startPos;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                Physics.SyncTransforms();
                if (controller != null) controller.enabled = true;

                Debug.Log($"[Chapter2 Spawn Check] Scene: {currentScene} | Start WorldPos: {transform.position} | GroundHeight: {groundHeight:F3}");
                StartCoroutine(LogPositionAfterOneSecond(currentScene));
            }
        }
        else if (currentScene == "RungThachSanh")
        {
            if (Chapter1NewGameHandler.Instance == null && Object.FindAnyObjectByType<Chapter1NewGameHandler>() == null)
            {
                GameObject newGameObj = new GameObject("Chapter1NewGameHandler");
                newGameObj.AddComponent<Chapter1NewGameHandler>();
            }

            if (Terrain.activeTerrain != null)
            {
                Vector3 curPos = transform.position;
                float groundY = Terrain.activeTerrain.SampleHeight(curPos) + Terrain.activeTerrain.transform.position.y;
                if (curPos.y < groundY || Mathf.Abs(curPos.y - groundY) > 0.3f)
                {
                    if (controller != null) controller.enabled = false;
                    transform.position = new Vector3(curPos.x, groundY, curPos.z);
                    Physics.SyncTransforms();
                    if (controller != null) controller.enabled = true;
                }
            }
        }
        else if (currentScene == "Chapter3_MieuChanTinh")
        {
            spawnPoint = GameObject.Find("Chapter3_StartPoint") ?? GameObject.Find("Chapter3StartPoint") ?? GameObject.Find("Chapter3SpawnPoint");
        }
        else if (currentScene == "Chapter6_HangDaiBang" || currentScene.ToLower().Contains("chapter6"))
        {
            spawnPoint = GameObject.Find("Chapter6_StartPoint") ?? GameObject.Find("Chapter6StartPoint") ?? GameObject.Find("ThachSanh_SpawnPoint");
        }

        // Áp dụng spawnPoint nếu có (cho Chapter 2 quay về, Chapter 3 hoặc Chapter 6)
        if (spawnPoint != null)
        {
            if (controller != null) controller.enabled = false;
            Vector3 pos = spawnPoint.transform.position;
            float groundHeight = pos.y;
            bool groundDetected = false;

            if (Terrain.activeTerrain != null)
            {
                groundHeight = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
                if (groundHeight > pos.y) pos.y = groundHeight + 0.05f;
                groundDetected = true;
            }
            else
            {
                // Bắn tia raycast xuống đất để phát hiện collider mặt sàn (MeshCollider, BoxCollider)
                // Lọc bỏ collider của chính nhân vật và bỏ qua Trigger
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(pos.x, pos.y + 3f, pos.z), Vector3.down, 15f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider != null && h.collider.transform != transform && !h.collider.transform.IsChildOf(transform))
                    {
                        groundHeight = h.point.y;
                        pos.y = groundHeight + 0.05f;
                        groundDetected = true;
                        break;
                    }
                }
            }

            // Phòng vệ an toàn: Nếu spawnPoint nằm lơ lửng ngoài hư không không có mặt đất bên dưới
            // nhưng vị trí hiện tại của nhân vật trong scene đang đứng trên mặt đất hợp lệ -> giữ vị trí hiện tại
            if (!groundDetected && Terrain.activeTerrain == null)
            {
                RaycastHit[] selfHits = Physics.RaycastAll(new Vector3(transform.position.x, transform.position.y + 3f, transform.position.z), Vector3.down, 15f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(selfHits, (a, b) => a.distance.CompareTo(b.distance));
                bool currentHasGround = false;
                foreach (var h in selfHits)
                {
                    if (h.collider != null && h.collider.transform != transform && !h.collider.transform.IsChildOf(transform))
                    {
                        currentHasGround = true;
                        break;
                    }
                }

                if (currentHasGround)
                {
                    Debug.LogWarning($"[PlayerMovement] SpawnPoint '{spawnPoint.name}' tại {pos} không có mặt đất bên dưới! Giữ vị trí scene hiện tại {transform.position}.");
                    pos = transform.position;
                }
            }

            transform.position = pos;
            transform.rotation = spawnPoint.transform.rotation;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            if (controller != null) controller.enabled = true;

            Debug.Log($"[Spawn Check] Scene: {currentScene} | Player Spawned At: {transform.position} | GroundHeight: {groundHeight:F3}");
            if (currentScene == "Chapter2_LyThong")
            {
                StartCoroutine(LogPositionAfterOneSecond(currentScene));
            }
        }

        // Tự động gắn và cập nhật PlayerTrophyHolder
        ChanTinhHeadManager trophyHolder = GetComponent<ChanTinhHeadManager>();
        if (trophyHolder != null) trophyHolder.CheckAndApplyTrophy();

        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        mainCam = Camera.main;

        if (anim != null)
            anim.applyRootMotion = false;

        // Đảm bảo thông số chuẩn cho CharacterController (đáy tại Y=0, cao 1.8m)
        if (controller != null)
        {
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.radius = 0.35f;
            controller.height = 1.8f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;
            controller.skinWidth = 0.08f;
        }

        // Đảm bảo CapsuleCollider không xung đột vật lý với CharacterController
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.center = new Vector3(0f, 0.9f, 0f);
            cap.radius = 0.35f;
            cap.height = 1.8f;
            cap.isTrigger = true;
        }

        // Khóa Rigidbody để CharacterController toàn quyền xử lý di chuyển
        Rigidbody rbComp = GetComponent<Rigidbody>();
        if (rbComp != null)
        {
            rbComp.isKinematic = true;
            rbComp.useGravity = false;
            rbComp.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Đặt lại local position của model con nếu bị lệch
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue;
            if (child.name.ToLower().Contains("meshy") || child.name.ToLower().Contains("wanderer") || child.name.ToLower().Contains("axe"))
            {
                if (child.localPosition.y != 0f || child.localPosition.x != 0f)
                {
                    child.localPosition = Vector3.zero;
                }
            }
        }

        // Tự động căn chỉnh chân nhân vật chạm sát mặt đất ngay khi vào cảnh (giới hạn chênh lệch < 1.0m, không kéo lên tầng khác)
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit initGroundHit, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (initGroundHit.collider.transform != transform && !initGroundHit.collider.transform.IsChildOf(transform) && initGroundHit.normal.y >= 0.65f)
            {
                float diff = Mathf.Abs(initGroundHit.point.y - transform.position.y);
                if (diff < 1.0f)
                {
                    Vector3 initPos = transform.position;
                    initPos.y = initGroundHit.point.y;
                    transform.position = initPos;
                }
            }
        }

        if (Time.timeScale > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private System.Collections.IEnumerator LogPositionAfterOneSecond(string sceneName)
    {
        yield return new WaitForSeconds(1f);
        float groundHeight = transform.position.y;
        if (Terrain.activeTerrain != null)
        {
            groundHeight = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
        }
        Debug.Log($"[Chapter2 Spawn Check +1s] Scene: {sceneName} | WorldPos after 1s: {transform.position} | GroundHeight: {groundHeight:F3}");
    }

    void Update()
    {
        // Khi game đang tạm dừng (ví dụ màn hình giới thiệu đang mở) thì không cập nhật di chuyển
        if (Time.timeScale <= 0f) return;

        // 1. KIỂM TRA TRẠNG THÁI TẤN CÔNG
        bool isAttacking = false;
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // ĐÃ THÊM TÊN CỦA 2 KHỐI R VÀ T VÀO ĐÂY ĐỂ TRÁNH TRƯỢT CHÂN
            if (stateInfo.IsName("chém") || stateInfo.IsName("Attack_R") || stateInfo.IsName("Attack_T") || stateInfo.IsName("đá"))
            {
                isAttacking = true;
            }
        }

        // 2. NHẬN NÚT DI CHUYỂN
        float moveX = 0f;
        float moveZ = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
        }

        if (moveX == 0f && moveZ == 0f)
        {
            try
            {
                moveX = Input.GetAxisRaw("Horizontal");
                moveZ = Input.GetAxisRaw("Vertical");
            }
            catch { }
        }

        // 3. TÍNH HƯỚNG DI CHUYỂN
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        Vector3 move = Vector3.zero;
        if (mainCam != null)
        {
            Vector3 camForward = mainCam.transform.forward;
            Vector3 camRight = mainCam.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            move = (camForward * moveZ + camRight * moveX).normalized;
        }
        else
        {
            Vector3 fwd = transform.forward;
            Vector3 rgt = transform.right;
            fwd.y = 0f;
            rgt.y = 0f;
            fwd.Normalize();
            rgt.Normalize();
            move = (fwd * moveZ + rgt * moveX).normalized;
        }

        // 4. KIỂM TRA MẶT ĐẤT CHÍNH XÁC, NHẢY & TRỌNG LỰC
        bool spacePressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
        if (!spacePressed)
        {
            try { spacePressed = Input.GetKeyDown(KeyCode.Space); } catch { }
        }

        // Đếm lùi thời gian bảo vệ cú nhảy (Grace Period) để nhân vật bay lên hoàn toàn mà không bị check nhầm chạm đất
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        bool isGrounded = false;
        Vector3 groundNormal = Vector3.up;

        // BẢO VỆ CÚ NHẢY: Khi vừa bấm nhảy hoặc vận tốc Y đang bay lên (> 0.05), TUYỆT ĐỐI không nhận là chạm đất
        if (jumpCooldownTimer > 0f || velocity.y > 0.05f)
        {
            isGrounded = false;
        }
        else
        {
            // Chỉ kiểm tra mặt đất khi đang rơi xuống hoặc đã hết đà nhảy lên
            isGrounded = controller.isGrounded;

            // Kiểm tra raycast chính xác sát đáy chân (khoảng cách 0.25m vừa đủ, không vươn quá xa gây nhận nhầm khi ở trên không)
            Vector3 rayOrigin = transform.position + Vector3.up * 0.15f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 0.25f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.collider != null && groundHit.collider.transform != transform && !groundHit.collider.transform.IsChildOf(transform))
                {
                    // Chỉ công nhận là mặt đất nếu góc nghiêng có thể đi được (slope <= ~48 độ)
                    if (groundHit.normal.y >= 0.65f)
                    {
                        isGrounded = true;
                        groundNormal = groundHit.normal;
                    }
                }
            }
        }

        if (isGrounded)
        {
            // Lực bám đất êm ái (-2f thay vì -5f để không bị giật khựng vị trí Y khi chạm đất)
            if (velocity.y < 0f) velocity.y = -2f;

            // Mỗi lần nhấn chỉ kích hoạt 1 lần nhảy duy nhất, có thời gian đệm chống spam phím
            if (!isAttacking && spacePressed && jumpCooldownTimer <= 0f)
            {
                velocity.y = jumpForce;
                jumpCooldownTimer = 0.25f; // Khóa 0.25s chống chạm đất sớm và chống nhảy liên tục ngoài ý muốn
                isGrounded = false;
                wasGroundedLastFrame = false;
                if (anim != null) anim.SetTrigger("JumpTrigger");
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
            if (velocity.y < -20f) velocity.y = -20f;
        }

        // 5. ĐIỀU CHỈNH TỐC ĐỘ Combat
        float currentSpeed = moveSpeed;
        if (isAttacking)
        {
            currentSpeed = moveSpeed * 0.35f;
        }

        if (move.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 6. ÁP DỤNG DI CHUYỂN
        Vector3 moveDir = move;
        // Chiếu vector di chuyển lên mặt dốc CHỈ KHI đang chạm đất ổn định
        if (isGrounded && groundNormal != Vector3.up && groundNormal.y >= 0.65f)
        {
            Vector3 projected = Vector3.ProjectOnPlane(move, groundNormal).normalized;
            if (projected.y <= 0.75f)
            {
                moveDir = projected;
            }
        }

        Vector3 finalMovement = moveDir * currentSpeed;
        if (isGrounded)
        {
            finalMovement.y += velocity.y;
        }
        else
        {
            // Khi đang ở trên không: Thành phần Y thuần túy do vận tốc nhảy và trọng lực điều khiển
            finalMovement.y = velocity.y;
        }
        controller.Move(finalMovement * Time.deltaTime);

        // Bám dốc khi chạy xuống địa hình dốc hang động (Downhill slope-snapping)
        // TUYỆT ĐỐI KHÔNG BÁM XUỐNG KHI ĐANG TRONG QUÁ TRÌNH NHẢY (jumpCooldownTimer > 0 hoặc velocity.y > 0)
        if (jumpCooldownTimer <= 0f && velocity.y <= 0f && wasGroundedLastFrame && !isGrounded && move.magnitude > 0.1f)
        {
            Vector3 snapOrigin = transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(snapOrigin, Vector3.down, out RaycastHit snapHit, 0.8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (snapHit.collider.transform != transform && !snapHit.collider.transform.IsChildOf(transform))
                {
                    float snapDist = snapHit.distance - 0.2f;
                    // Chỉ kéo xuống nếu độ hụt chân dương (nằm phía dưới) và góc mặt phẳng đi bộ được
                    if (snapDist > 0.02f && snapDist <= 0.5f && snapHit.normal.y >= 0.65f)
                    {
                        controller.Move(Vector3.down * snapDist);
                        isGrounded = true;
                    }
                }
            }
        }
        wasGroundedLastFrame = isGrounded;

        // An toàn tuyệt đối: Không để nhân vật lọt xuống dưới Terrain ở các cảnh ngoài trời
        // Tuyệt đối không áp dụng cho hang động (Chapter 6) vì hang động nằm ngầm dưới lòng đất/núi
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        bool isUndergroundCave = sceneName.Contains("chapter6") || sceneName.Contains("hang") || sceneName.Contains("cave");
        if (Terrain.activeTerrain != null && !isUndergroundCave)
        {
            float terrainHeight = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
            // Chỉ cứu nguy bằng CharacterController.Move nếu chân thực sự bị lọt hẳn xuống dưới đất khi đang rơi (không can thiệp khi đang nhảy lên)
            if (transform.position.y < (terrainHeight - 0.05f) && velocity.y <= 0f)
            {
                float penetration = terrainHeight - transform.position.y;
                controller.Move(Vector3.up * penetration);
                velocity.y = 0f;
                isGrounded = true;
            }
        }

        // 7. CẬP NHẬT ANIMATOR CHẠY
        if (anim != null)
        {
            if (!isAttacking)
            {
                anim.SetFloat("Speed", move.magnitude);
            }
            else
            {
                anim.SetFloat("Speed", 0f);
            }

            // === HỆ THỐNG ĐÁNH BẰNG PHÍM (E, R, T, F) ===
            if (!isAttacking)
            {
                // Phím E - Tung đòn tay 1 (Chém / Chặt cây)
                bool ePressed = false;
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ePressed = true;
                if (!ePressed) { try { ePressed = Input.GetKeyDown(KeyCode.E); } catch { } }
                if (ePressed)
                {
                    anim.SetTrigger("CutTrigger");
              
                }

                // Phím R - Tung đòn tay 2
                bool rPressed = false;
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) rPressed = true;
                if (!rPressed) { try { rPressed = Input.GetKeyDown(KeyCode.R); } catch { } }
                if (rPressed)
                {
                    anim.SetTrigger("Attack_R");
                }

                // Phím T - Tung đòn tay 3
                bool tPressed = false;
                if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) tPressed = true;
                if (!tPressed) { try { tPressed = Input.GetKeyDown(KeyCode.T); } catch { } }
                if (tPressed)
                {
                    anim.SetTrigger("Attack_T");
                }

                // Phím F - Tung đòn đá chân
                bool fPressed = false;
                if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
                if (!fPressed) { try { fPressed = Input.GetKeyDown(KeyCode.F); } catch { } }
                if (fPressed)
                {
                    anim.SetTrigger("HighKickTrigger");
                }
            }
        }

        // KIỂM TRA PHÁT ÂM THANH BƯỚC CHÂN
        // Dùng trục Horizontal (A/D) và Vertical (W/S) để biết người chơi có đang bấm nút đi lại hay không
        bool dangDiChuyen = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        if (dangDiChuyen && isGrounded)
        {
            // Nếu đang di chuyển mà loa chưa phát -> Bật loa lên
            if (loaDiChuyen != null && !loaDiChuyen.isPlaying)
            {
                loaDiChuyen.clip = tiengBuocChan;
                loaDiChuyen.Play();
            }
        }
        else
        {
            // Nếu buông phím ra đứng im -> Tắt loa ngay lập tức
            if (loaDiChuyen != null && loaDiChuyen.isPlaying)
            {
                loaDiChuyen.Stop();
            }
        }
    }

    // 8. HÀM TẤN CÔNG BẰNG TAY
    public void Attack()
    {
        if (attackPoint == null) return;
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider enemy in hitEnemies)
        {
            // 1. Chém trúng Thú/Quái
            if (enemy.CompareTag("Enemy"))
            {
                // [THÊM 1 DÒNG NÀY ĐỂ PHÁT TIẾNG CHÉM QUÁI]
                if (loaVuKhi != null && tiengChemQuai != null) loaVuKhi.PlayOneShot(tiengChemQuai);

                // Ưu tiên 1: Thử tìm máu của Boss Chằn Tinh
                MauChanTinh bossHealth = enemy.GetComponent<MauChanTinh>();
                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(attackDamage, transform.position);
                }

                // Ưu tiên 2: Thử tìm máu của Gà, Hươu, Ngựa, Hổ
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null)
                {
                    animalHealth.TakeDamage(attackDamage);
                }
            }
            // 2. Chém trúng Cây
            else if (enemy.CompareTag("Tree"))
            {
                // [THÊM 1 DÒNG NÀY ĐỂ PHÁT TIẾNG CHẶT CÂY]
                if (loaVuKhi != null && tiengChatCay != null) loaVuKhi.PlayOneShot(tiengChatCay);

                TreeHealth tree = enemy.GetComponent<TreeHealth>();
                if (tree != null)
                {
                    tree.TakeDamage(attackDamage);
                }
            }
        }
    }

    // 9. HÀM TẤN CÔNG BẰNG CHÂN
    public void KickAttack()
    {
        if (attackPoint == null) return;
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                // Tìm máu Boss Chằn Tinh
                MauChanTinh bossHealth = enemy.GetComponent<MauChanTinh>();
                if (bossHealth != null) bossHealth.TakeDamage(kickDamage, transform.position);

                // Tìm máu Thú rừng
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null) animalHealth.TakeDamage(kickDamage);
            }
        }
    }

    // (Hỗ trợ) Vẽ vòng tròn tầm đánh màu đỏ trong màn hình Scene để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}