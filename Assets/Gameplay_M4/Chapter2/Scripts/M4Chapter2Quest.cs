using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4Chapter2Quest : MonoBehaviour
    {
        public enum Chapter2State { WaitingForInteraction, OpeningDialogue, Following, WaitingAtHome, FinalDialogue, Completed, PathBlocked }
        [Header("References")]
        public Transform player, homePoint;
        public M4Chapter2HomeTrigger homeTrigger;
        public MonoBehaviour playerMovement;
        public NavMeshAgent npcAgent;
        public Animator npcAnimator;
        public Transform[] waypoints;
        [Header("Interaction and following")]
        public float interactionRange = 3f;
        public float waypointRadius = 0.7f;
        public float playerArrivalRadius = 3f;
        public float pauseDistance = 8f;
        public float resumeDistance = 5f;
        public float stuckTimeout = 4f;
        [Header("M5 can replace dialogue and the temporary HUD")]
        public string[] openingDialogue = {
            "LÝ THÔNG:\nTrời sắp tối rồi. Nếu chú không chê, hãy theo ta về nhà nghỉ tạm.",
            "THẠCH SANH:\nTôi xin đa tạ huynh. Tôi sẽ đi cùng huynh.",
            "LÝ THÔNG:\nNhà ta ở ngay phía trước. Chú cứ đi theo ta nhé." };
        public string[] finalDialogue = {
            "LÝ THÔNG:\nĐã đến nhà rồi. Từ nay chúng ta kết nghĩa anh em, cùng giúp đỡ nhau nhé!",
            "THẠCH SANH:\nTôi xin kết nghĩa cùng huynh. Cảm ơn huynh đã cho tôi một mái nhà." };
        public bool showTemporaryHud = true;
        public bool showDemoLabel = true;
        // Turn this off when M5's UI/input calls the public interaction methods.
        public bool useBuiltInInput = true;
        public Font hudFont;

        [SerializeField] private Chapter2State state;
        private readonly List<Transform> route = new List<Transform>();
        private readonly RaycastHit[] sightHits = new RaycastHit[32];
        private NavMeshPath checkedPath;
        private int dialogueIndex, waypointIndex, recoveryAttempts;
        private float stuckTime, pendingTime;
        private Vector3 previousNpcPosition;
        private bool destinationRequested, npcPaused, completedRaised, hasSpeedParameter;
        private MonoBehaviour lockedMovement;
        private bool movementWasEnabled;
        private Animator pausedPlayerAnimator;
        private float previousPlayerAnimationSpeed;
        private GUIStyle titleStyle, textStyle, promptStyle;
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        public Chapter2State State => state;
        public int DialogueIndex => dialogueIndex;
        public int WaypointIndex => waypointIndex;
        public bool NpcPaused => npcPaused;
        public bool DestinationRequested => destinationRequested;
        public string PathFailureReason { get; private set; }
        public event Action<M4Chapter2Quest> Changed;
        public event Action<string> DialogueRequested;
        public event Action DialogueClosed;
        public event Action Completed;
        private bool IsDialogue => state == Chapter2State.OpeningDialogue || state == Chapter2State.FinalDialogue;
        private string[] Lines => state == Chapter2State.FinalDialogue ? finalDialogue : openingDialogue;
        public string CurrentDialogue => IsDialogue && Lines != null && dialogueIndex < Lines.Length ?
            Lines[dialogueIndex] : IsDialogue ? "LÝ THÔNG:\nChúng ta cùng giúp đỡ nhau nhé." : "";
        public string StatusMessage => state switch {
            Chapter2State.WaitingForInteraction => "Đến gần Lý Thông và nhấn E để nói chuyện.",
            Chapter2State.OpeningDialogue => "Nói chuyện với Lý Thông.",
            Chapter2State.Following => npcPaused ? "Lý Thông đang chờ. Đến gần để cùng đi tiếp." : "Đi theo Lý Thông về nhà.",
            Chapter2State.WaitingAtHome => "Lý Thông đã đến nhà. Hãy đến sân nhà gặp anh ấy.",
            Chapter2State.FinalDialogue => "Kết nghĩa anh em với Lý Thông.",
            Chapter2State.Completed => "Đã hoàn thành chương 2: Gặp Lý Thông.",
            _ => "Lý Thông chưa đi tiếp được. Đến gần và nhấn E để thử lại." };

        private void Awake()
        {
            checkedPath = new NavMeshPath();
        }

        private void Start()
        {
            if (npcAnimator == null && npcAgent != null) npcAnimator = npcAgent.GetComponentInChildren<Animator>();
            if (npcAnimator != null && npcAnimator.runtimeAnimatorController != null)
                foreach (var parameter in npcAnimator.parameters)
                    hasSpeedParameter |= parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float;
            StopNpc();
        }

        private void Update()
        {
            bool pressedE = useBuiltInInput && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (useBuiltInInput && IsDialogue)
            {
                bool advance = pressedE || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                if (advance) AdvanceDialogue();
            }
            else if (pressedE) TryInteract();
            TickFollowing(Time.deltaTime);
        }

        public bool TryInteract()
        {
            if (!isActiveAndEnabled) return false;
            if (state == Chapter2State.PathBlocked)
            {
                if (!CanTalkToNpc()) return false;
                recoveryAttempts = 0;
                return route.Count == 0 ? BeginFollowing() : RequestDestination();
            }
            return TryBeginDialogue();
        }

        public bool TryBeginDialogue()
        {
            if (!isActiveAndEnabled || !CanTalkToNpc()) return false;
            Chapter2State expectedState = state;
            bool first = expectedState == Chapter2State.WaitingForInteraction;
            bool final = expectedState == Chapter2State.WaitingAtHome && PlayerAtHome();
            if ((!first && !final) || state != expectedState) return false;
            StopNpc();
            dialogueIndex = 0;
            LockMovement();
            SetState(final ? Chapter2State.FinalDialogue : Chapter2State.OpeningDialogue);
            DialogueRequested?.Invoke(CurrentDialogue);
            return true;
        }

        public bool AdvanceDialogue()
        {
            if (!isActiveAndEnabled || !IsDialogue) return false;
            if (++dialogueIndex < Mathf.Max(1, Lines != null ? Lines.Length : 0))
            {
                DialogueRequested?.Invoke(CurrentDialogue);
                Changed?.Invoke(this);
                return true;
            }
            bool final = state == Chapter2State.FinalDialogue;
            RestoreMovement();
            if (final)
            {
                SetState(Chapter2State.Completed);
                DialogueClosed?.Invoke();
                if (!completedRaised) { completedRaised = true; Completed?.Invoke(); }
            }
            else { BeginFollowing(); DialogueClosed?.Invoke(); }
            return true;
        }

        public void TickFollowing(float deltaTime)
        {
            if (!isActiveAndEnabled || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (state == Chapter2State.WaitingAtHome) { TryBeginDialogue(); return; }
            if (state != Chapter2State.Following) return;
            if (player == null || npcAgent == null || !npcAgent.enabled || !npcAgent.isOnNavMesh)
            { BlockPath("Player or NPC navigation is unavailable."); return; }
            float distance = Vector3.Distance(player.position, npcAgent.transform.position);
            bool shouldPause = npcPaused ? distance > resumeDistance : distance > pauseDistance;
            if (shouldPause != npcPaused)
            {
                npcPaused = shouldPause;
                npcAgent.isStopped = npcPaused;
                stuckTime = pendingTime = 0f;
                previousNpcPosition = npcAgent.transform.position;
                Changed?.Invoke(this);
            }
            if (npcPaused) { SetAnimationSpeed(0f); return; }
            float dt = Mathf.Max(0f, deltaTime);
            if (!destinationRequested) { RequestDestination(); return; }
            if (npcAgent.pathPending)
            {
                pendingTime += dt;
                if (pendingTime > 5f) BlockPath("Path calculation timed out.");
                return;
            }
            pendingTime = 0f;
            Transform target = waypointIndex < route.Count ? route[waypointIndex] : null;
            if (target == null) { BlockPath("The route destination is unavailable."); return; }
            if (npcAgent.pathStatus != NavMeshPathStatus.PathComplete)
            { BlockPath("The requested path is not complete."); return; }
            if (Vector3.Distance(npcAgent.transform.position, target.position) <= waypointRadius)
            {
                destinationRequested = false;
                recoveryAttempts = 0;
                if (++waypointIndex < route.Count) RequestDestination();
                else { StopNpc(); SetState(Chapter2State.WaitingAtHome); TryBeginDialogue(); }
                return;
            }
            if (!npcAgent.hasPath)
            { BlockPath("The requested path is not complete."); return; }
            SetAnimationSpeed(npcAgent.velocity.magnitude / Mathf.Max(0.1f, npcAgent.speed));
            if ((npcAgent.transform.position - previousNpcPosition).sqrMagnitude >= 0.000225f) stuckTime = 0f;
            else stuckTime += dt;
            previousNpcPosition = npcAgent.transform.position;
            if (stuckTime >= Mathf.Max(1f, stuckTimeout))
            {
                if (recoveryAttempts++ == 0) RequestDestination();
                else BlockPath("NPC movement is blocked. Retry the current waypoint.");
            }
        }

        private bool BeginFollowing()
        {
            route.Clear();
            if (waypoints != null)
                foreach (var waypoint in waypoints)
                    if (waypoint != null && waypoint != homePoint && !route.Contains(waypoint)) route.Add(waypoint);
            if (homePoint != null) route.Add(homePoint);
            waypointIndex = 0;
            recoveryAttempts = 0;
            if (homePoint == null) { BlockPath("A home arrival marker is required."); return false; }
            return RequestDestination();
        }

        private bool RequestDestination()
        {
            destinationRequested = false;
            if (npcAgent == null || !npcAgent.enabled || !npcAgent.isOnNavMesh || waypointIndex >= route.Count || route[waypointIndex] == null)
            { BlockPath("NPC or route is not on a valid NavMesh."); return false; }
            Vector3 target = route[waypointIndex].position;
            float markerTolerance = Mathf.Min(0.5f, Mathf.Max(0.1f, waypointRadius * 0.5f));
            if (!NavMesh.SamplePosition(target, out NavMeshHit destination, markerTolerance, npcAgent.areaMask))
            { BlockPath("The waypoint is outside the walkable surface."); return false; }
            if (!npcAgent.CalculatePath(destination.position, checkedPath) || checkedPath.status != NavMeshPathStatus.PathComplete)
            { BlockPath("No complete path to the current waypoint."); return false; }
            Vector3[] corners = checkedPath.corners;
            bool endpointValid = corners.Length > 0 ? Vector3.Distance(corners[corners.Length - 1], target) <= waypointRadius :
                Vector3.Distance(npcAgent.transform.position, target) <= waypointRadius;
            if (!endpointValid) { BlockPath("The path endpoint does not reach the waypoint."); return false; }
            npcAgent.stoppingDistance = Mathf.Min(npcAgent.stoppingDistance, waypointRadius * 0.8f);
            if (!npcAgent.SetDestination(destination.position)) { BlockPath("The destination request was rejected."); return false; }
            destinationRequested = true;
            npcPaused = false;
            npcAgent.isStopped = false;
            previousNpcPosition = npcAgent.transform.position;
            stuckTime = pendingTime = 0f;
            PathFailureReason = "";
            SetState(Chapter2State.Following);
            return true;
        }

        private bool PlayerAtHome() => player != null && homePoint != null &&
            Vector3.Distance(player.position, homePoint.position) <= playerArrivalRadius &&
            (homeTrigger == null || homeTrigger.ContainsPlayer);

        private bool CanTalkToNpc()
        {
            if (player == null || npcAgent == null || !npcAgent.gameObject.activeInHierarchy ||
                Vector3.Distance(player.position, npcAgent.transform.position) > interactionRange) return false;
            Vector3 origin = player.position + Vector3.up;
            Vector3 ray = npcAgent.transform.position + Vector3.up - origin;
            if (ray.magnitude < 0.01f) return true;
            int count = Physics.RaycastNonAlloc(origin, ray.normalized, sightHits, ray.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = sightHits[i].collider.transform;
                if (hit == player || hit.IsChildOf(player) || hit == npcAgent.transform || hit.IsChildOf(npcAgent.transform)) continue;
                if (sightHits[i].distance < ray.magnitude - 0.04f) return false;
            }
            return true;
        }

        private void LockMovement()
        {
            if (lockedMovement != null || playerMovement == null || playerMovement == this) return;
            lockedMovement = playerMovement;
            movementWasEnabled = lockedMovement.enabled;
            lockedMovement.enabled = false;
            Animator animator = player != null ? player.GetComponentInChildren<Animator>() : null;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash != SpeedId || parameter.type != AnimatorControllerParameterType.Float) continue;
                pausedPlayerAnimator = animator;
                previousPlayerAnimationSpeed = animator.GetFloat(SpeedId);
                animator.SetFloat(SpeedId, 0f);
                break;
            }
        }
        private void RestoreMovement()
        {
            if (lockedMovement != null) lockedMovement.enabled = movementWasEnabled;
            lockedMovement = null;
            if (pausedPlayerAnimator != null) pausedPlayerAnimator.SetFloat(SpeedId, previousPlayerAnimationSpeed);
            pausedPlayerAnimator = null;
        }
        private void StopNpc()
        {
            if (npcAgent != null && npcAgent.enabled && npcAgent.isOnNavMesh) npcAgent.isStopped = true;
            SetAnimationSpeed(0f);
        }
        private void SetAnimationSpeed(float speed)
        {
            if (npcAnimator != null && hasSpeedParameter) npcAnimator.SetFloat(SpeedId, speed);
        }
        private void BlockPath(string reason)
        {
            destinationRequested = false;
            PathFailureReason = reason;
            StopNpc();
            RestoreMovement();
            SetState(Chapter2State.PathBlocked);
        }
        private void SetState(Chapter2State next)
        {
            if (state == next) return;
            state = next;
            Changed?.Invoke(this);
        }
        private void OnDisable()
        {
            bool wasDialogue = IsDialogue;
            RestoreMovement();
            StopNpc();
            if (wasDialogue) DialogueClosed?.Invoke();
        }
        private void OnEnable()
        {
            if (IsDialogue)
            {
                LockMovement();
                DialogueRequested?.Invoke(CurrentDialogue);
                return;
            }
            if (state != Chapter2State.Following || !destinationRequested || npcAgent == null ||
                !npcAgent.enabled || !npcAgent.isOnNavMesh) return;
            if (player != null)
            {
                float distance = Vector3.Distance(player.position, npcAgent.transform.position);
                npcPaused = npcPaused ? distance > resumeDistance : distance > pauseDistance;
            }
            npcAgent.isStopped = npcPaused;
            previousNpcPosition = npcAgent.transform.position;
            stuckTime = pendingTime = 0f;
        }

        private void OnGUI()
        {
            if (!showTemporaryHud) return;
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                if (hudFont != null) textStyle.font = hudFont;
                textStyle.normal.textColor = new Color(0.95f, 0.95f, 0.86f);
                titleStyle = new GUIStyle(textStyle) { fontSize = 22, fontStyle = FontStyle.Bold };
                promptStyle = new GUIStyle(textStyle) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float scale = Mathf.Clamp(Screen.width / 1280f, 0.65f, 1.4f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            Panel(new Rect(20f, 20f, 370f, showDemoLabel ? 173f : 147f));
            GUI.Label(new Rect(38f, 32f, 334f, 32f), "CHƯƠNG 2 · GẶP LÝ THÔNG", titleStyle);
            if (showDemoLabel)
                GUI.Label(new Rect(38f, 67f, 334f, 25f), "CẢNH THỬ RIÊNG · GAMEPLAY M4", promptStyle);
            GUI.Label(new Rect(38f, showDemoLabel ? 102f : 76f, 334f, 70f), StatusMessage, textStyle);
            if (IsDialogue)
            {
                float panelWidth = Mathf.Min(820f, width - 40f);
                float left = (width - panelWidth) / 2f;
                Panel(new Rect(left, height - 216f, panelWidth, 185f));
                GUI.Label(new Rect(left + 24f, height - 196f, panelWidth - 48f, 118f), CurrentDialogue, textStyle);
                GUI.Label(new Rect(left + 24f, height - 75f, panelWidth - 48f, 30f), "E / Space / Chuột trái: tiếp tục", promptStyle);
            }
            else GUI.Label(new Rect(20f, height - 46f, width - 40f, 30f),
                "WASD: di chuyển · Giữ chuột phải: xoay góc nhìn · E: tương tác", promptStyle);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
        private static void Panel(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.10f, 0.13f, 0.09f, 0.94f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.76f, 0.78f, 0.38f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.2f, interactionRange);
            waypointRadius = Mathf.Max(0.2f, waypointRadius);
            playerArrivalRadius = Mathf.Max(0.2f, playerArrivalRadius);
            resumeDistance = Mathf.Max(interactionRange, resumeDistance);
            pauseDistance = Mathf.Max(resumeDistance + 0.5f, pauseDistance);
            stuckTimeout = Mathf.Max(1f, stuckTimeout);
        }
    }
}
