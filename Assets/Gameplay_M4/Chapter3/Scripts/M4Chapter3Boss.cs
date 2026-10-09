using System;
using UnityEngine;
using UnityEngine.AI;

namespace PRU.M4
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class M4Chapter3Boss : MonoBehaviour
    {
        public enum BossState { Idle, Chase, Telegraph, Strike, Recovery, Dead }
        public NavMeshAgent agent;
        public Animator characterAnimator;
        public M4Chapter3Health health;
        public M4Chapter3Player player;
        public M4Chapter3Game game;
        public Vector3 spawnPosition = new Vector3(0, 0, 4);
        public float chaseSpeed = 2.8f, detectionRange = 14f, loseRange = 20f;
        public float attackRange = 2.3f, attackDamage = 20f, facingAngle = 120f;
        public float telegraphDuration = .9f, strikeDelay = .12f, strikeDuration = .25f, recoveryDuration = 1.2f;
        public float repathInterval = .25f, destinationSampleRadius = .8f;
        public bool useAutomaticTick = true;

        public BossState State { get; private set; } = BossState.Idle;
        public int AttackVariant { get; private set; }
        public string NavigationFailureReason { get; private set; } = "";
        public bool CombatPaused => combatPaused;
        public float StateTimeRemaining => Mathf.Max(0f, DurationFor(State) - stateElapsed);
        public event Action<M4Chapter3Boss> Changed;
        public event Action<M4Chapter3Boss> AttackTelegraphed;
        private float stateElapsed, repathTimer, pendingTime, stuckTime, spawnYaw;
        private bool combatPaused, strikeApplied, deathAnimated, hasSpeed, hasAttack, hasDeath, hasVariant;
        private int attackNumber;
        private Vector3 strikeDirection, lastPosition;
        private M4Chapter3Health subscribedHealth;
        private NavMeshPath path;
        private static readonly int SpeedId = Animator.StringToHash("Speed"), AttackId = Animator.StringToHash("Attack"),
            DeathId = Animator.StringToHash("Death"), VariantId = Animator.StringToHash("AttackVariant");
        private bool CanFight => !combatPaused && health != null && health.IsAlive && player != null &&
            player.health != null && player.health.IsAlive && (game == null || game.IsRoundActive);
        private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

        private void Awake() { EnsureReferences(); spawnYaw = transform.eulerAngles.y; lastPosition = transform.position; }
        private void OnEnable() { EnsureReferences(); SubscribeHealth(); }
        private void Start() { if (game == null) ResetCombat(); }
        private void Update() { if (useAutomaticTick) TickCombat(Time.deltaTime); }
        private void EnsureReferences()
        {
            if (path == null) path = new NavMeshPath();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (health == null) health = GetComponent<M4Chapter3Health>();
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            hasSpeed = hasAttack = hasDeath = hasVariant = false;
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            {
                characterAnimator.applyRootMotion = false;
                foreach (var parameter in characterAnimator.parameters)
                {
                    hasSpeed |= parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float;
                    hasAttack |= parameter.nameHash == AttackId && parameter.type == AnimatorControllerParameterType.Trigger;
                    hasDeath |= parameter.nameHash == DeathId && parameter.type == AnimatorControllerParameterType.Trigger;
                    hasVariant |= parameter.nameHash == VariantId && parameter.type == AnimatorControllerParameterType.Int;
                }
            }
            if (agent != null) { agent.speed = chaseSpeed; agent.updateRotation = false; }
        }
        private void SubscribeHealth()
        {
            if (subscribedHealth == health) return;
            if (subscribedHealth != null) subscribedHealth.Died -= OnDied;
            subscribedHealth = health;
            if (subscribedHealth != null) subscribedHealth.Died += OnDied;
        }
        private void OnDied(M4Chapter3Health sender, GameObject source)
        {
            if (sender == health && health != null && !health.IsAlive) EnterDead();
        }

        public void TickCombat(float deltaTime)
        {
            if (!isActiveAndEnabled || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (health != null && !health.IsAlive) { EnterDead(); return; }
            if (!CanFight) { StopAgent(false); return; }
            float dt = Mathf.Max(0f, deltaTime);
            float distance = Vector3.Distance(transform.position, player.transform.position);
            switch (State)
            {
                case BossState.Idle:
                    StopAgent(false);
                    if (distance <= detectionRange && HasSight()) SetState(BossState.Chase);
                    break;
                case BossState.Chase:
                    if (distance > loseRange) { StopAgent(true); NavigationFailureReason = ""; SetState(BossState.Idle); break; }
                    if (distance <= attackRange && HasSight()) { BeginTelegraph(); break; }
                    Chase(dt);
                    break;
                case BossState.Telegraph:
                    stateElapsed += dt;
                    if (stateElapsed >= telegraphDuration) { strikeApplied = false; SetState(BossState.Strike); }
                    break;
                case BossState.Strike:
                    stateElapsed += dt;
                    if (!strikeApplied && stateElapsed >= strikeDelay)
                    {
                        strikeApplied = true;
                        if (M4Chapter3Health.InAttackArc(transform, strikeDirection, player.health, attackRange, facingAngle) && HasSight())
                            player.health.TakeDamage(attackDamage, gameObject);
                    }
                    if (CanFight && stateElapsed >= strikeDuration) SetState(BossState.Recovery);
                    break;
                case BossState.Recovery:
                    stateElapsed += dt;
                    if (stateElapsed >= recoveryDuration)
                    {
                        if (distance > loseRange) NavigationFailureReason = "";
                        SetState(distance <= loseRange ? BossState.Chase : BossState.Idle);
                    }
                    break;
            }
        }

        private bool HasSight() => M4Chapter3Health.ClearSight(transform, player.health, transform.position + Vector3.up * 1.2f);
        private void BeginTelegraph()
        {
            StopAgent(true);
            NavigationFailureReason = "";
            strikeDirection = player.transform.position - transform.position;
            strikeDirection.y = 0f;
            if (strikeDirection.sqrMagnitude < .001f) strikeDirection = transform.forward;
            strikeDirection.Normalize();
            transform.rotation = Quaternion.LookRotation(strikeDirection);
            AttackVariant = attackNumber++ % 2;
            if (hasVariant && characterAnimator != null) characterAnimator.SetInteger(VariantId, AttackVariant);
            if (hasAttack && characterAnimator != null) characterAnimator.SetTrigger(AttackId);
            SetState(BossState.Telegraph);
            AttackTelegraphed?.Invoke(this);
        }

        private void Chase(float dt)
        {
            if (!AgentReady)
            {
                NavigationFailureReason = "Chằn Tinh chưa đứng trên vùng di chuyển.";
                SetSpeed(0); return;
            }
            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = Mathf.Max(.05f, repathInterval);
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit target, destinationSampleRadius, filter) ||
                    !agent.CalculatePath(target.position, path) || path.status != NavMeshPathStatus.PathComplete ||
                    path.corners.Length == 0 || Vector3.Distance(path.corners[path.corners.Length - 1], target.position) > .3f ||
                    !agent.SetPath(path))
                {
                    NavigationFailureReason = "Chằn Tinh đang chờ đường tới người chơi.";
                    StopAgent(true); return;
                }
                NavigationFailureReason = "";
                agent.isStopped = false;
            }
            if (agent.pathPending)
            {
                pendingTime += dt;
                if (pendingTime > 2f) { StopAgent(true); repathTimer = 0; }
                SetSpeed(0); return;
            }
            pendingTime = 0;
            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete) { SetSpeed(0); return; }
            if (Vector3.Distance(transform.position, lastPosition) < .01f && agent.remainingDistance > agent.stoppingDistance + .3f)
                stuckTime += dt;
            else stuckTime = 0;
            lastPosition = transform.position;
            if (stuckTime > 2f)
            {
                NavigationFailureReason = "Chằn Tinh đang tìm lại đường đi.";
                StopAgent(true); repathTimer = 0; stuckTime = 0; return;
            }
            Vector3 direction = agent.desiredVelocity; direction.y = 0;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-8f * dt));
            SetSpeed(Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(.1f, chaseSpeed)));
        }

        private void SetState(BossState state)
        {
            State = state; stateElapsed = 0;
            if (state != BossState.Chase) SetSpeed(0);
            else repathTimer = 0;
            Changed?.Invoke(this);
        }
        private float DurationFor(BossState state) => state == BossState.Telegraph ? telegraphDuration :
            state == BossState.Strike ? strikeDuration : state == BossState.Recovery ? recoveryDuration : 0f;
        private void SetSpeed(float speed) { if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat(SpeedId, speed); }
        private void StopAgent(bool clearPath)
        {
            if (AgentReady)
            {
                if (clearPath) agent.ResetPath();
                // ResetPath/Warp may clear the native stop flag. Set it last so a
                // new round cannot begin with a supposedly idle agent still moving.
                agent.isStopped = true;
            }
            SetSpeed(0);
        }
        private void EnterDead()
        {
            if (deathAnimated) return;
            deathAnimated = true; StopAgent(true);
            SetState(BossState.Dead);
            if (hasAttack && characterAnimator != null) characterAnimator.ResetTrigger(AttackId);
            if (hasDeath && characterAnimator != null) characterAnimator.SetTrigger(DeathId);
        }
        public void SetCombatPaused(bool paused)
        {
            combatPaused = paused;
            if (paused)
            {
                StopAgent(false);
                if (State == BossState.Telegraph || State == BossState.Strike)
                {
                    strikeApplied = true;
                    if (hasAttack && characterAnimator != null) characterAnimator.ResetTrigger(AttackId);
                    SetState(BossState.Recovery);
                }
            }
            else if (State == BossState.Chase) repathTimer = 0;
        }
        public void ResetCombat()
        {
            EnsureReferences(); SubscribeHealth();
            combatPaused = strikeApplied = deathAnimated = false;
            attackNumber = AttackVariant = 0; stateElapsed = repathTimer = pendingTime = stuckTime = 0;
            NavigationFailureReason = ""; StopAgent(true);
            if (agent != null && agent.enabled)
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit spawn, destinationSampleRadius, filter) && agent.Warp(spawn.position))
                {
                    transform.rotation = Quaternion.Euler(0, spawnYaw, 0);
                    StopAgent(true);
                }
                else NavigationFailureReason = "Chưa tìm được vị trí bắt đầu của Chằn Tinh trên vùng di chuyển.";
            }
            else NavigationFailureReason = "Vùng di chuyển của Chằn Tinh chưa được bật.";
            lastPosition = transform.position;
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            {
                characterAnimator.Rebind(); characterAnimator.Update(0);
                if (hasAttack) characterAnimator.ResetTrigger(AttackId);
                if (hasDeath) characterAnimator.ResetTrigger(DeathId);
                if (hasVariant) characterAnimator.SetInteger(VariantId, 0);
            }
            if (health != null) health.ResetHealth();
            SetState(BossState.Idle);
        }
        private void OnDisable()
        {
            StopAgent(false);
            if (subscribedHealth != null) subscribedHealth.Died -= OnDied;
            subscribedHealth = null;
        }
        private void OnValidate()
        {
            chaseSpeed = Mathf.Max(.1f, chaseSpeed); detectionRange = Mathf.Max(attackRange, detectionRange);
            loseRange = Mathf.Max(detectionRange, loseRange); attackRange = Mathf.Max(.2f, attackRange);
            attackDamage = Mathf.Max(1f, attackDamage); facingAngle = Mathf.Clamp(facingAngle, 1f, 180f);
            telegraphDuration = Mathf.Max(.2f, telegraphDuration); strikeDelay = Mathf.Max(.01f, strikeDelay);
            strikeDuration = Mathf.Max(strikeDelay + .02f, strikeDuration); recoveryDuration = Mathf.Max(.2f, recoveryDuration);
            destinationSampleRadius = Mathf.Clamp(destinationSampleRadius, .1f, 2f);
        }
    }
}
