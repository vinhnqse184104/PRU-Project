using UnityEngine;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    // This controller is only for walking through the M4 environment test scene.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class M4PlaytestController : MonoBehaviour
    {
        [Header("Character")]
        public Camera viewCamera;
        public Animator characterAnimator;
        public Vector3 spawnPosition = new Vector3(0f, 0.25f, 0f);
        public float moveSpeed = 3.5f;
        public float sprintSpeed = 6f;
        public float rotationSpeed = 12f;
        public float jumpHeight = 1f;
        public float gravity = -20f;

        [Header("Camera - hold right mouse button to orbit")]
        public float cameraDistance = 5f;
        public float cameraLookHeight = 1.3f;
        public float cameraPitch = 24f;
        public float mouseSensitivity = 0.12f;
        public float cameraSmooth = 10f;

        [Header("Return to start after entering the river")]
        public bool useRiverRespawn = true;
        public float riverMinX = -30f;
        public float riverMaxX = 30f;
        public float riverMinZ = 4f;
        public float riverMaxZ = 8f;
        public float riverSurfaceY = -0.15f;
        public float respawnBelowY = -3f;
        public bool showHelp = true;

        private CharacterController controller;
        private readonly RaycastHit[] cameraHits = new RaycastHit[32];
        private float verticalSpeed;
        private float cameraYaw;
        private float spawnYaw;
        private float noticeUntil;
        private bool hasSpeedParameter;
        private bool hasJumpParameter;
        private bool snapCamera;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int JumpId = Animator.StringToHash("JumpTrigger");

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (viewCamera == null)
            {
                var cameraObject = new GameObject("M4 Playtest Camera");
                cameraObject.tag = "MainCamera";
                viewCamera = cameraObject.AddComponent<Camera>();
            }

            if (characterAnimator != null)
            {
                characterAnimator.applyRootMotion = false;
                foreach (var parameter in characterAnimator.parameters)
                {
                    hasSpeedParameter |= parameter.nameHash == SpeedId &&
                                         parameter.type == AnimatorControllerParameterType.Float;
                    hasJumpParameter |= parameter.nameHash == JumpId &&
                                        parameter.type == AnimatorControllerParameterType.Trigger;
                }
            }
            spawnYaw = transform.eulerAngles.y;
            Respawn(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Respawn(true);
                return;
            }

            if (mouse != null && mouse.rightButton.isPressed)
            {
                // Mouse delta is already the displacement in this frame.
                Vector2 delta = mouse.delta.ReadValue();
                cameraYaw += delta.x * mouseSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y * mouseSensitivity, 5f, 65f);
            }

            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            Quaternion heading = Quaternion.Euler(0f, cameraYaw, 0f);
            Vector3 forward = viewCamera != null ? viewCamera.transform.forward : heading * Vector3.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 movement = forward * input.y + right * input.x;

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            if (controller.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalSpeed = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                if (hasJumpParameter)
                    characterAnimator.SetTrigger(JumpId);
            }
            verticalSpeed += gravity * Time.deltaTime;
            if (movement.sqrMagnitude > 0.001f)
            {
                float rotationBlend = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(movement), rotationBlend);
            }
            Vector3 velocity = movement * (sprint ? sprintSpeed : moveSpeed);
            velocity.y = verticalSpeed;
            CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
                verticalSpeed = 0f;
            if (hasSpeedParameter)
                characterAnimator.SetFloat(SpeedId, input.magnitude, 0.12f, Time.deltaTime);

            Vector3 position = transform.position;
            bool enteredRiver = useRiverRespawn && position.x >= riverMinX &&
                position.x <= riverMaxX && position.z >= riverMinZ &&
                position.z <= riverMaxZ && position.y <= riverSurfaceY + 0.05f;
            if (enteredRiver || position.y < respawnBelowY)
                Respawn(true);
        }

        private void LateUpdate()
        {
            if (viewCamera == null || controller == null)
                return;
            Vector3 target = transform.position + Vector3.up * cameraLookHeight;
            Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 desired = target - orbit * Vector3.forward * cameraDistance;
            float blend = 1f - Mathf.Exp(-cameraSmooth * Time.deltaTime);
            Vector3 candidate = snapCamera ? desired :
                Vector3.Lerp(viewCamera.transform.position, desired, blend);
            Vector3 offset = candidate - target;
            float distance = offset.magnitude;
            if (distance > 0.01f)
            {
                Vector3 direction = offset / distance;
                int hitCount = Physics.SphereCastNonAlloc(target, 0.18f, direction,
                    cameraHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float safeDistance = distance;
                for (int i = 0; i < hitCount; i++)
                {
                    Transform hitTransform = cameraHits[i].collider.transform;
                    if (hitTransform == transform || hitTransform.IsChildOf(transform))
                        continue;
                    safeDistance = Mathf.Min(safeDistance, Mathf.Max(0.05f, cameraHits[i].distance - 0.05f));
                }
                viewCamera.transform.position = target + direction * safeDistance;
                viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
            }
            snapCamera = false;
        }

        private void Respawn(bool showNotice)
        {
            // Disable the controller briefly so teleporting updates its collision volume.
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, spawnYaw, 0f));
            controller.enabled = true;
            verticalSpeed = 0f;
            cameraYaw = spawnYaw;
            snapCamera = true;
            if (hasSpeedParameter)
                characterAnimator.SetFloat(SpeedId, 0f);
            if (hasJumpParameter)
                characterAnimator.ResetTrigger(JumpId);
            if (showNotice)
                noticeUntil = Time.unscaledTime + 3f;
        }

        private void OnGUI()
        {
            if (!showHelp)
                return;
            GUILayout.BeginArea(new Rect(12f, 12f, Mathf.Min(360f, Screen.width - 24f), 175f), GUI.skin.box);
            GUILayout.Label("Thử cầu và đường làng");
            GUILayout.Label("WASD: di chuyển | Shift: chạy | Space: nhảy");
            GUILayout.Label("Giữ chuột phải và kéo: xoay góc nhìn");
            GUILayout.Label("R: về điểm bắt đầu | Qua suối bằng cầu");
            if (Time.unscaledTime < noticeUntil)
                GUILayout.Label("Đã về điểm bắt đầu. Thử đi qua cầu nhé!");
            GUILayout.EndArea();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
            jumpHeight = Mathf.Max(0.1f, jumpHeight);
            gravity = -Mathf.Max(0.1f, Mathf.Abs(gravity));
            cameraDistance = Mathf.Max(1f, cameraDistance);
            cameraPitch = Mathf.Clamp(cameraPitch, 5f, 65f);
            cameraSmooth = Mathf.Max(0.1f, cameraSmooth);
        }
    }
}
