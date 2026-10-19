using UnityEngine;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class M4Chapter5Player : MonoBehaviour
    {
        public Camera viewCamera;
        public Animator characterAnimator;
        public M4Chapter5Quest quest;
        public Transform muzzle;
        public Transform bowGrip, bowHand;
        public GameObject arrowVisual;
        public Vector3 spawnPosition = new Vector3(0, .1f, -20);
        public float moveSpeed = 4, sprintSpeed = 7, arrowSpeed = 45, shotCooldown = .65f;
        public float cameraYaw, cameraPitch = -5, sensitivity = .13f;
        public bool useBuiltInInput = true;
        public bool InputLocked { get; private set; }
        public int ShotsFired { get; private set; }
        public Vector3 AimDirection => viewCamera != null ? viewCamera.transform.forward : transform.forward;
        CharacterController body;
        float verticalSpeed, nextShot;
        bool hasSpeed, hasJump, heldCursor;
        CursorLockMode previousCursor;
        bool previousVisibility;
        readonly RaycastHit[] cameraHits = new RaycastHit[32];

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (characterAnimator != null)
            {
                characterAnimator.applyRootMotion = false;
                foreach (var p in characterAnimator.parameters)
                { hasSpeed |= p.name == "Speed" && p.type == AnimatorControllerParameterType.Float; hasJump |= p.name == "JumpTrigger" && p.type == AnimatorControllerParameterType.Trigger; }
            }
        }
        void Start() { if (viewCamera == null) viewCamera = Camera.main; UpdateCamera(); }
        void Update()
        {
            var keyboard = useBuiltInInput ? Keyboard.current : null;
            var mouse = useBuiltInInput ? Mouse.current : null;
            if (InputLocked) { SetWalking(0); ReleaseCursor(); return; }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) { Respawn(); return; }
            bool aiming = mouse != null && mouse.rightButton.isPressed;
            if (aiming)
            {
                if (!heldCursor) { previousCursor = Cursor.lockState; previousVisibility = Cursor.visible; heldCursor = true; }
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                Vector2 delta = mouse.delta.ReadValue();
                cameraYaw += delta.x * sensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y * sensitivity, -32, 65);
            }
            else ReleaseCursor();
            Vector2 input = keyboard == null ? Vector2.zero : new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 movement = Quaternion.Euler(0, cameraYaw, 0) * new Vector3(input.x, 0, input.y);
            if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (body.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            { verticalSpeed = Mathf.Sqrt(40); if (hasJump) characterAnimator.SetTrigger("JumpTrigger"); }
            verticalSpeed += -20 * Time.deltaTime;
            if (movement.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 12 * Time.deltaTime);
            bool sprint = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            var flags = body.Move((movement * (sprint ? sprintSpeed : moveSpeed) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            SetWalking(input.magnitude);
            // Update orbit before aiming so this frame's mouse delta drives the same shot.
            UpdateCamera();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryShoot();
            if (transform.position.y < -10) Respawn();
        }
        void LateUpdate()
        {
            if (!InputLocked) UpdateCamera();
            if (bowGrip != null && bowHand != null)
            {
                bowGrip.position = bowHand.position;
                bowGrip.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            }
        }
        void UpdateCamera()
        {
            if (viewCamera == null) return;
            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
            Vector3 pivot = transform.position + Vector3.up * 1.65f;
            Vector3 offset = rotation * new Vector3(.65f, 0, -4.5f);
            float distance = offset.magnitude;
            int count = Physics.SphereCastNonAlloc(pivot, .18f, offset.normalized, cameraHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float safe = distance;
            for (int i = 0; i < count; i++) if (!cameraHits[i].transform.IsChildOf(transform)) safe = Mathf.Min(safe, Mathf.Max(.3f, cameraHits[i].distance - .05f));
            viewCamera.transform.SetPositionAndRotation(pivot + offset.normalized * safe, rotation);
        }
        public bool TryShoot()
        {
            if (InputLocked || quest == null || quest.State != M4Chapter5Quest.QuestState.HuntEagle || Time.time < nextShot || viewCamera == null) return false;
            nextShot = Time.time + shotCooldown;
            Vector3 start = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.35f + transform.forward * .6f;
            Vector3 aim = viewCamera.transform.position + AimDirection * 120;
            var hits = Physics.RaycastAll(viewCamera.transform.position, AimDirection, 120, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = 120;
            foreach (var hit in hits) if (!hit.transform.IsChildOf(transform) && hit.distance < nearest) { nearest = hit.distance; aim = hit.point; }
            Vector3 direction = (aim - start).normalized;
            var projectile = new GameObject("M4_C5_Arrow");
            projectile.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            if (arrowVisual != null)
            {
                var visual = Instantiate(arrowVisual, projectile.transform);
                visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
                foreach (var col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
            }
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.transform.SetParent(projectile.transform, false); visual.transform.localScale = new Vector3(.018f, .35f, .018f); visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Destroy(visual.GetComponent<Collider>());
            }
            projectile.AddComponent<M4Chapter5Arrow>().Initialize(this, quest, direction, arrowSpeed);
            ShotsFired++;
            return true;
        }
        public void SetInputLocked(bool value) { InputLocked = value; if (value) { SetWalking(0); ReleaseCursor(); } }
        public void ResetBow() { nextShot = 0; ShotsFired = 0; }
        public void Respawn()
        {
            bool wasEnabled = body.enabled; body.enabled = false;
            transform.position = quest != null ? quest.Checkpoint : spawnPosition;
            body.enabled = wasEnabled; verticalSpeed = 0; cameraYaw = 0; cameraPitch = -5; SetWalking(0); Physics.SyncTransforms(); UpdateCamera();
        }
        void SetWalking(float speed) { if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat("Speed", speed); }
        void ReleaseCursor() { if (!heldCursor) return; Cursor.lockState = previousCursor; Cursor.visible = previousVisibility; heldCursor = false; }
        void OnDisable() { ReleaseCursor(); SetWalking(0); }
    }
}
