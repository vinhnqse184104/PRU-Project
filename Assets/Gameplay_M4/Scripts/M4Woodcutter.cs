using UnityEngine;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4Woodcutter : MonoBehaviour
    {
        public M4WoodQuest quest;
        public Animator characterAnimator;
        public Transform heldAxe;
        public float interactionRange = 2.5f;
        public float chopCooldown = 0.45f;
        public bool showHud = true;
        public Font hudFont;

        private readonly Collider[] nearby = new Collider[128];
        private readonly RaycastHit[] sightHits = new RaycastHit[64];
        private M4QuestGiver nearbyGiver;
        private M4WoodPickup nearbyPickup;
        private M4ChoppableTree nearbyTree;
        private float nextChopTime;
        private float swingStarted = -10f;
        private Quaternion axeRestRotation;
        private bool axeInitialized;
        private bool hasCutTrigger;
        private bool hasCutState;
        private string notice;
        private float noticeUntil;
        private GUIStyle titleStyle, textStyle, smallStyle, promptStyle;
        private static readonly int CutTriggerId = Animator.StringToHash("CutTrigger");
        private static readonly int CutStateId = Animator.StringToHash("Base Layer.chém");

        private void Start()
        {
            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            {
                foreach (var parameter in characterAnimator.parameters)
                    hasCutTrigger |= parameter.nameHash == CutTriggerId &&
                                     parameter.type == AnimatorControllerParameterType.Trigger;
                hasCutState = characterAnimator.HasState(0, CutStateId);
            }
            if (heldAxe != null)
            {
                axeRestRotation = heldAxe.localRotation;
                axeInitialized = true;
            }
        }

        private void Update()
        {
            FindTargets();
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                if (nearbyPickup != null && quest != null && quest.CanChop)
                    TryPickup(nearbyPickup);
                else if (nearbyGiver != null)
                    TryAcceptQuest(nearbyGiver);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && nearbyTree != null)
                TryChop(nearbyTree, Time.time);
        }

        private void LateUpdate()
        {
            if (heldAxe == null || !axeInitialized)
                return;
            float elapsed = Time.time - swingStarted;
            float duration = Mathf.Min(0.4f, Mathf.Max(0.1f, chopCooldown));
            float arc = elapsed >= 0f && elapsed <= duration ?
                Mathf.Sin(elapsed / duration * Mathf.PI) * -75f : 0f;
            heldAxe.localRotation = axeRestRotation * Quaternion.Euler(arc, 0f, 0f);
        }

        public bool TryAcceptQuest(M4QuestGiver giver)
        {
            if (quest == null || giver == null || !giver.isActiveAndEnabled ||
                giver != quest.questGiver || giver.quest != quest ||
                !CanReach(giver, giver.InteractionPosition, Mathf.Min(interactionRange, giver.interactionRange)))
                return false;
            if (!quest.Accept(this, giver))
                return false;
            ShowNotice("Đã nhận nhiệm vụ! Qua cầu để tìm cây đốn củi.");
            return true;
        }

        // Both normal input and automated checks use these methods. timeNow is the
        // gameplay clock, so tests can advance time without bypassing the cooldown.
        public bool TryChop(M4ChoppableTree tree, float timeNow)
        {
            if (quest == null || !quest.CanChop || tree == null || tree.quest != quest ||
                tree.IsChopped || !tree.isActiveAndEnabled || float.IsNaN(timeNow) ||
                float.IsInfinity(timeNow) || timeNow < 0f || timeNow < nextChopTime ||
                !CanReach(tree, tree.InteractionPosition, interactionRange))
                return false;
            if (!tree.ReceiveHit(this))
                return false;
            nextChopTime = timeNow + Mathf.Max(0.1f, chopCooldown);
            swingStarted = Time.time;
            if (characterAnimator != null)
            {
                // The shared animator only transitions from idle to its cut state.
                // Crossfade also permits chopping just after walking up to a tree.
                if (hasCutState)
                    characterAnimator.CrossFadeInFixedTime(CutStateId, 0.05f, 0, 0f);
                else if (hasCutTrigger)
                    characterAnimator.SetTrigger(CutTriggerId);
            }
            if (tree.IsChopped)
                ShowNotice("Cây đã đổ. Đến gần bó củi và nhấn E để nhặt.");
            return true;
        }

        public bool TryPickup(M4WoodPickup pickup)
        {
            if (quest == null || !quest.CanChop || pickup == null || pickup.quest != quest ||
                pickup.IsCollected || !pickup.isActiveAndEnabled ||
                !CanReach(pickup, pickup.InteractionPosition, interactionRange))
                return false;
            if (!pickup.Collect(this))
                return false;
            ShowNotice(quest.State == M4WoodQuest.QuestState.Completed ?
                "Hoàn thành Đốn củi! Bạn đã nhặt đủ " + quest.TargetWood + " bó củi." :
                "Đã nhặt củi: " + quest.CollectedWood + "/" + quest.TargetWood);
            return true;
        }

        public bool CanReach(MonoBehaviour target, Vector3 interactionPosition, float range)
        {
            if (!isActiveAndEnabled || target == null || !target.isActiveAndEnabled ||
                Vector3.Distance(transform.position, target.transform.position) > Mathf.Max(0f, range))
                return false;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 ray = interactionPosition - origin;
            float distance = ray.magnitude;
            if (distance < 0.01f)
                return true;
            int count = Physics.RaycastNonAlloc(origin, ray / distance, sightHits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = sightHits[i].collider.transform;
                if (hit == transform || hit.IsChildOf(transform) ||
                    hit == target.transform || hit.IsChildOf(target.transform))
                    continue;
                if (sightHits[i].distance < distance - 0.04f)
                    return false;
            }
            return true;
        }

        private void FindTargets()
        {
            nearbyGiver = null;
            nearbyPickup = null;
            nearbyTree = null;
            float bestGiver = float.PositiveInfinity, bestPickup = bestGiver, bestTree = bestGiver;
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.6f,
                Mathf.Max(0.1f, interactionRange) + 1f, nearby, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = nearby[i];
                M4QuestGiver giver = candidate.GetComponentInParent<M4QuestGiver>();
                if (giver != null && giver.quest == quest &&
                    CanReach(giver, giver.InteractionPosition, Mathf.Min(interactionRange, giver.interactionRange)))
                {
                    float score = TargetScore(giver.transform);
                    if (score < bestGiver) { nearbyGiver = giver; bestGiver = score; }
                }
                M4WoodPickup pickup = candidate.GetComponentInParent<M4WoodPickup>();
                if (pickup != null && pickup.quest == quest && !pickup.IsCollected &&
                    CanReach(pickup, pickup.InteractionPosition, interactionRange))
                {
                    float score = TargetScore(pickup.transform);
                    if (score < bestPickup) { nearbyPickup = pickup; bestPickup = score; }
                }
                M4ChoppableTree tree = candidate.GetComponentInParent<M4ChoppableTree>();
                if (tree != null && tree.quest == quest && !tree.IsChopped &&
                    CanReach(tree, tree.InteractionPosition, interactionRange))
                {
                    float score = TargetScore(tree.transform);
                    if (score < bestTree) { nearbyTree = tree; bestTree = score; }
                }
            }
        }

        private float TargetScore(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            return direction.magnitude - Vector3.Dot(transform.forward, direction.normalized) * 0.35f;
        }

        private void ShowNotice(string message)
        {
            notice = message;
            noticeUntil = Time.unscaledTime + 3.5f;
        }

        private string TargetPrompt()
        {
            if (quest.State == M4WoodQuest.QuestState.Completed)
                return "ĐỐN CỦI HOÀN THÀNH · " + quest.CollectedWood + "/" + quest.TargetWood;
            if (quest.State == M4WoodQuest.QuestState.NotStarted)
                return nearbyGiver != null ? "[E] Nhận nhiệm vụ Đốn củi" : "Đến bảng nhiệm vụ ở đầu đường làng";
            if (nearbyPickup != null)
                return "[E] Nhặt bó củi";
            if (nearbyTree != null)
                return "[CHUỘT TRÁI] Chặt cây · " + nearbyTree.Hits + "/" + nearbyTree.HitsRequired + " nhát";
            return "Qua cầu, tìm cây trong khu đốn củi";
        }

        private void OnGUI()
        {
            if (!showHud || quest == null)
                return;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            float scale = Mathf.Clamp(Screen.width / 1280f, 0.65f, 1.4f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            DrawPanel(new Rect(20f, 20f, 365f, 224f));
            GUI.Label(new Rect(38f, 32f, 330f, 30f), "THẠCH SANH · ĐỐN CỦI", titleStyle);
            string stateLabel = quest.State == M4WoodQuest.QuestState.NotStarted ? "CHƯA NHẬN NHIỆM VỤ" :
                quest.State == M4WoodQuest.QuestState.Completed ? "ĐÃ HOÀN THÀNH" : "ĐANG THỰC HIỆN";
            GUI.Label(new Rect(38f, 66f, 330f, 24f), stateLabel, smallStyle);
            GUI.Label(new Rect(38f, 95f, 330f, 28f), "Củi đã nhặt   " + quest.CollectedWood + " / " + quest.TargetWood, textStyle);
            DrawRect(new Rect(38f, 130f, 328f, 9f), new Color(0.25f, 0.31f, 0.26f));
            DrawRect(new Rect(38f, 130f, 328f * quest.CollectedWood / quest.TargetWood, 9f),
                new Color(0.69f, 0.82f, 0.38f));
            string objective = quest.State == M4WoodQuest.QuestState.NotStarted ?
                "Đến bảng nhiệm vụ và nhấn E để bắt đầu." :
                quest.State == M4WoodQuest.QuestState.Completed ?
                "Bạn đã giúp làng nhặt đủ củi. Cảm ơn bạn!" :
                "Chặt cây 3 nhát, rồi nhấn E để nhặt củi.\nNhặt đủ " + quest.TargetWood + " bó để hoàn thành.";
            GUI.Label(new Rect(38f, 150f, 328f, 72f), objective, textStyle);

            float promptWidth = Mathf.Min(650f, width - 40f);
            DrawPanel(new Rect((width - promptWidth) / 2f, height - 114f, promptWidth, 52f));
            GUI.Label(new Rect((width - promptWidth) / 2f + 14f, height - 108f,
                promptWidth - 28f, 40f), TargetPrompt(), promptStyle);
            GUI.Label(new Rect(20f, height - 43f, width - 40f, 30f),
                "WASD: di chuyển   ·   Shift: chạy   ·   Space: nhảy   ·   Giữ chuột phải: xoay góc nhìn   ·   R: về đầu đường",
                smallStyle);
            if (Time.unscaledTime < noticeUntil)
            {
                DrawPanel(new Rect((width - promptWidth) / 2f, height - 184f, promptWidth, 54f));
                GUI.Label(new Rect((width - promptWidth) / 2f + 14f, height - 178f,
                    promptWidth - 28f, 42f), notice, promptStyle);
            }
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            if (hudFont != null)
                textStyle.font = hudFont;
            textStyle.normal.textColor = new Color(0.94f, 0.95f, 0.88f);
            titleStyle = new GUIStyle(textStyle) { fontSize = 21, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(textStyle) { fontSize = 13 };
            smallStyle.normal.textColor = new Color(0.74f, 0.82f, 0.68f);
            promptStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        private static void DrawPanel(Rect rect)
        {
            DrawRect(rect, new Color(0.08f, 0.13f, 0.10f, 0.94f));
            DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(0.69f, 0.82f, 0.38f));
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnDisable()
        {
            if (heldAxe != null && axeInitialized)
                heldAxe.localRotation = axeRestRotation;
        }

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.1f, interactionRange);
            chopCooldown = Mathf.Max(0.1f, chopCooldown);
        }
    }
}
