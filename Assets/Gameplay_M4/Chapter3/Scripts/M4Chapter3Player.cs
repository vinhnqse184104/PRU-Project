using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class M4Chapter3Player : MonoBehaviour
    {
        public Camera viewCamera;
        public Animator characterAnimator;
        public M4Chapter3Health health;
        public M4Chapter3Game game;
        public GameObject heldAxe;
        public Vector3 spawnPosition = new Vector3(0, .1f, -4);
        public float moveSpeed = 3.5f, sprintSpeed = 6f, rotationSpeed = 12f, jumpHeight = 1f, gravity = -20f;
        public float cameraDistance = 5f, cameraLookHeight = 1.3f, cameraPitch = 24f, mouseSensitivity = .12f, cameraSmooth = 10f;
        public float attackRange = 2.8f, attackDamage = 35f, attackHitDelay = .25f, attackDuration = .6f, attackCooldown = .8f, facingAngle = 110f;
        public float respawnBelowY = -5f;
        public bool useBuiltInInput = true, useAutomaticTick = true;

        private CharacterController controller;
        private float verticalSpeed, cameraYaw, spawnYaw, attackElapsed, cooldown;
        private bool inputLocked, attacking, hitApplied, snapCamera;
        private bool hasSpeed, hasJump, hasAttack;
        private uint swingRevision;
        private Vector3 swingDirection;
        private readonly Collider[] overlapHits = new Collider[128];
        private readonly RaycastHit[] cameraHits = new RaycastHit[64];
        private readonly HashSet<M4Chapter3Health> swingTargets = new HashSet<M4Chapter3Health>();
        private static readonly int SpeedId = Animator.StringToHash("Speed"), JumpId = Animator.StringToHash("Jump"), AttackId = Animator.StringToHash("Attack");
        public bool InputLocked => inputLocked;
        public bool IsAttacking => attacking;
        public float AttackCooldownRemaining => cooldown;
        public float AttackElapsed => attackElapsed;
        public int SwingId { get; private set; }
        public event Action<M4Chapter3Player> AttackStarted;
        public event Action<M4Chapter3Health> TargetHit;
        private bool CanAct => !inputLocked && health != null && health.IsAlive && (game == null || game.IsRoundActive);

        private void Awake()
        {
            EnsureReferences();
            spawnYaw = transform.eulerAngles.y;
            cameraYaw = spawnYaw;
        }
        private void EnsureReferences()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (health == null) health = GetComponent<M4Chapter3Health>();
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            if (viewCamera == null) viewCamera = Camera.main;
            hasSpeed = hasJump = hasAttack = false;
            if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null) return;
            characterAnimator.applyRootMotion = false;
            foreach (var parameter in characterAnimator.parameters)
            {
                hasSpeed |= parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float;
                hasJump |= parameter.nameHash == JumpId && parameter.type == AnimatorControllerParameterType.Trigger;
                hasAttack |= parameter.nameHash == AttackId && parameter.type == AnimatorControllerParameterType.Trigger;
            }
        }

        private void Start() { if (game == null) ResetCombat(); }
        private void Update()
        {
            if (useAutomaticTick) TickCombat(Time.deltaTime);
            if (!CanAct) return;
            if (useBuiltInInput && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryAttack();
            MovePlayer(Time.deltaTime);
            if (transform.position.y < respawnBelowY)
            {
                if (game != null) health.TakeDamage(health.CurrentHealth, game.gameObject);
                else ResetCombat();
            }
        }

        private void MovePlayer(float deltaTime)
        {
            if (controller == null || !controller.enabled) return;
            var keyboard = useBuiltInInput ? Keyboard.current : null;
            var mouse = useBuiltInInput ? Mouse.current : null;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                cameraYaw += delta.x * mouseSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y * mouseSensitivity, 5f, 65f);
            }
            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (!attacking && keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 forward = viewCamera != null ? viewCamera.transform.forward : Quaternion.Euler(0, cameraYaw, 0) * Vector3.forward;
            forward.y = 0; forward.Normalize();
            Vector3 movement = forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            if (!attacking && controller.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalSpeed = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                if (hasJump && characterAnimator != null) characterAnimator.SetTrigger(JumpId);
            }
            verticalSpeed += gravity * deltaTime;
            if (movement.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 1f - Mathf.Exp(-rotationSpeed * deltaTime));
            Vector3 velocity = movement * (sprint ? sprintSpeed : moveSpeed); velocity.y = verticalSpeed;
            CollisionFlags collision = controller.Move(velocity * deltaTime);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat(SpeedId, input.magnitude, .1f, deltaTime);
        }

        public bool TryAttack()
        {
            if (!isActiveAndEnabled || !CanAct || attacking || cooldown > 0f) return false;
            attacking = true; hitApplied = false; attackElapsed = 0;
            swingRevision++;
            cooldown = attackCooldown; swingDirection = transform.forward;
            swingTargets.Clear(); SwingId++;
            if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat(SpeedId, 0f);
            if (hasAttack && characterAnimator != null) characterAnimator.SetTrigger(AttackId);
            AttackStarted?.Invoke(this);
            return true;
        }

        public void TickCombat(float deltaTime)
        {
            if (!isActiveAndEnabled || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (!CanAct) { CancelAttack(); return; }
            float dt = Mathf.Max(0f, deltaTime);
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (!attacking) return;
            attackElapsed += dt;
            if (!hitApplied && attackElapsed >= attackHitDelay)
            {
                hitApplied = true;
                ApplySwingHit();
            }
            if (attackElapsed >= attackDuration) attacking = false;
        }

        private void ApplySwingHit()
        {
            uint revision = swingRevision;
            Vector3 origin = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, attackRange, overlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider[] hits = count == overlapHits.Length ? Physics.OverlapSphere(origin, attackRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) : overlapHits;
            if (hits != overlapHits) count = hits.Length;
            for (int i = 0; i < count; i++)
            {
                if (!CanAct || !attacking || revision != swingRevision) break;
                var target = hits[i].GetComponentInParent<M4Chapter3Health>();
                if (target == null || target == health || !swingTargets.Add(target)) continue;
                if (!M4Chapter3Health.InAttackArc(transform, swingDirection, target, attackRange, facingAngle) ||
                    !M4Chapter3Health.ClearSight(transform, target, origin)) continue;
                if (target.TakeDamage(attackDamage, gameObject)) TargetHit?.Invoke(target);
            }
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (!locked) return;
            CancelAttack(); verticalSpeed = 0;
            if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat(SpeedId, 0f);
        }

        public void ResetCombat()
        {
            EnsureReferences();
            CancelAttack(); cooldown = verticalSpeed = 0; SwingId = 0;
            inputLocked = false;
            bool enabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0, spawnYaw, 0));
            controller.enabled = enabled;
            cameraYaw = spawnYaw; snapCamera = true;
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            {
                characterAnimator.Rebind(); characterAnimator.Update(0f);
                if (hasSpeed) characterAnimator.SetFloat(SpeedId, 0);
                if (hasJump) characterAnimator.ResetTrigger(JumpId);
                if (hasAttack) characterAnimator.ResetTrigger(AttackId);
            }
            if (health != null) health.ResetHealth();
        }

        private void CancelAttack()
        {
            swingRevision++;
            attacking = false; hitApplied = false; attackElapsed = 0;
            swingTargets.Clear();
            if (hasAttack && characterAnimator != null) characterAnimator.ResetTrigger(AttackId);
        }

        private void LateUpdate()
        {
            if (viewCamera == null) return;
            Vector3 target = transform.position + Vector3.up * cameraLookHeight;
            Vector3 desired = target - Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.forward * cameraDistance;
            Vector3 candidate = snapCamera ? desired : Vector3.Lerp(viewCamera.transform.position, desired, 1f - Mathf.Exp(-cameraSmooth * Time.deltaTime));
            Vector3 offset = candidate - target;
            if (offset.magnitude > .01f)
            {
                float distance = offset.magnitude;
                int count = Physics.SphereCastNonAlloc(target, .18f, offset.normalized, cameraHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Transform hit = cameraHits[i].collider.transform;
                    if (hit == transform || hit.IsChildOf(transform)) continue;
                    distance = Mathf.Min(distance, Mathf.Max(.05f, cameraHits[i].distance - .05f));
                }
                viewCamera.transform.position = target + offset.normalized * distance;
                viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
            }
            snapCamera = false;
        }

        private void OnDisable() { CancelAttack(); if (hasSpeed && characterAnimator != null) characterAnimator.SetFloat(SpeedId, 0); }
        private void OnValidate()
        {
            moveSpeed = Mathf.Max(.1f, moveSpeed); sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
            gravity = -Mathf.Max(.1f, Mathf.Abs(gravity)); jumpHeight = Mathf.Max(.1f, jumpHeight);
            attackRange = Mathf.Max(.2f, attackRange); attackDamage = Mathf.Max(1f, attackDamage);
            attackHitDelay = Mathf.Max(.01f, attackHitDelay); attackDuration = Mathf.Max(attackHitDelay + .05f, attackDuration);
            attackCooldown = Mathf.Max(attackDuration, attackCooldown); facingAngle = Mathf.Clamp(facingAngle, 1f, 180f);
            cameraDistance = Mathf.Max(1f, cameraDistance); cameraSmooth = Mathf.Max(.1f, cameraSmooth);
        }
    }
}
