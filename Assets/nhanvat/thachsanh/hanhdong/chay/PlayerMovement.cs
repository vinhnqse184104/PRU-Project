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
        }
        else if (currentScene == "Chapter3_MieuChanTinh")
        {
            spawnPoint = GameObject.Find("Chapter3_StartPoint") ?? GameObject.Find("Chapter3StartPoint") ?? GameObject.Find("Chapter3SpawnPoint");
        }
        else if (currentScene == "Chapter6_HangDaiBang" || currentScene.ToLower().Contains("chapter6"))
        {
            spawnPoint = GameObject.Find("Chapter6_StartPoint") ?? GameObject.Find("Chapter6StartPoint") ?? GameObject.Find("ThachSanh_SpawnPoint");
        }

        // Áp dụng spawnPoint nếu có (cho Chapter 2 quay về hoặc Chapter 3)
        if (spawnPoint != null)
        {
            if (controller != null) controller.enabled = false;
            Vector3 pos = spawnPoint.transform.position;
            float groundHeight = pos.y;
            if (Terrain.activeTerrain != null)
            {
                groundHeight = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
                if (groundHeight > pos.y) pos.y = groundHeight + 0.05f;
            }
            else if (Physics.Raycast(new Vector3(pos.x, pos.y + 2f, pos.z), Vector3.down, out RaycastHit hit, 5f))
            {
                groundHeight = hit.point.y;
                pos.y = hit.point.y + 0.05f;
            }
            transform.position = pos;
            transform.rotation = spawnPoint.transform.rotation;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            if (controller != null) controller.enabled = true;

            if (currentScene == "Chapter2_LyThong")
            {
                Debug.Log($"[Chapter2 Return Spawn Check] Scene: {currentScene} | Start WorldPos: {transform.position} | GroundHeight: {groundHeight:F3}");
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        // 1. KIỂM TRA TRẠNG THÁI TẤN CÔNG
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

        // 4. TRỌNG LỰC VÀ NHẢY
        bool spacePressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
        if (!spacePressed)
        {
            try { spacePressed = Input.GetKeyDown(KeyCode.Space); } catch { }
        }

        if (controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;
            if (!isAttacking && spacePressed)
            {
                velocity.y = jumpForce;
                if (anim != null) anim.SetTrigger("JumpTrigger");
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
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
        Vector3 finalMovement = move * currentSpeed;
        finalMovement.y = velocity.y;
        controller.Move(finalMovement * Time.deltaTime);

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
                // Ưu tiên 1: Thử tìm máu của Boss Chằn Tinh
                MauChanTinh bossHealth = enemy.GetComponent<MauChanTinh>();
                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(attackDamage, transform.position);
                }

                // Ưu tiên 2: Thử tìm máu của Gà, Hươu, Ngựa, Hổ (Thường dùng EnemyHealth)
                EnemyHealth animalHealth = enemy.GetComponent<EnemyHealth>();
                if (animalHealth != null)
                {
                    // Nếu EnemyHealth của bạn chưa có chức năng văng lùi, chỉ cần truyền sát thương:
                    animalHealth.TakeDamage(attackDamage);

                    // LƯU Ý: Nếu script EnemyHealth của bạn CÓ chức năng văng lùi giống Chằn Tinh,
                    // hãy thêm dấu // tắt dòng trên và bật dòng dưới lên:
                    // animalHealth.TakeDamage(attackDamage, transform.position);
                }
            }
            // 2. Chém trúng Cây
            else if (enemy.CompareTag("Tree"))
            {
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