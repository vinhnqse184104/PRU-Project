using System;
using UnityEngine;

namespace PRU.M4
{
    public interface IM4Chapter3Damageable
    {
        bool IsAlive { get; }
        bool TakeDamage(float amount, GameObject source = null);
    }

    [DisallowMultipleComponent]
    public sealed class M4Chapter3Health : MonoBehaviour, IM4Chapter3Damageable
    {
        public float maxHealth = 100f;
        public Transform hitPoint;
        [SerializeField] private float currentHealth;
        private bool damageEnabled = true;
        private uint resetRevision;
        private static readonly RaycastHit[] sightHits = new RaycastHit[64];

        public float CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0f;
        public bool DamageEnabled => damageEnabled;
        public Vector3 HitPosition => hitPoint != null ? hitPoint.position : transform.position + Vector3.up;
        public event Action<M4Chapter3Health> Changed;
        public event Action<M4Chapter3Health, GameObject> Damaged;
        public event Action<M4Chapter3Health, GameObject> Died;

        private void Awake() => ResetHealth();

        public bool TakeDamage(float amount, GameObject source = null)
        {
            if (!isActiveAndEnabled || !damageEnabled || !IsAlive || amount <= 0f ||
                float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            bool killed = currentHealth <= 0f;
            uint revision = resetRevision;
            Changed?.Invoke(this);
            // A UI result callback may immediately restart the round. Its new HP must
            // not receive notifications belonging to the hit that ended the old round.
            if (revision != resetRevision) return true;
            Damaged?.Invoke(this, source);
            if (killed && revision == resetRevision && !IsAlive) Died?.Invoke(this, source);
            return true;
        }

        public void ResetHealth()
        {
            resetRevision++;
            if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth)) maxHealth = 100f;
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = maxHealth;
            damageEnabled = true;
            Changed?.Invoke(this);
        }

        public void SetDamageEnabled(bool enabled) => damageEnabled = enabled;

        public static bool InAttackArc(Transform attacker, Vector3 direction, M4Chapter3Health target,
            float range, float facingAngle)
        {
            if (attacker == null || target == null || !target.isActiveAndEnabled || !target.IsAlive ||
                !target.gameObject.activeInHierarchy || Vector3.Distance(attacker.position, target.transform.position) > range) return false;
            Vector3 delta = target.transform.position - attacker.position;
            delta.y = direction.y = 0f;
            return delta.sqrMagnitude < .0001f || Vector3.Angle(direction, delta) <= facingAngle * .5f;
        }

        public static bool ClearSight(Transform attacker, M4Chapter3Health target, Vector3 origin)
        {
            Vector3 delta = target.HitPosition - origin;
            if (delta.magnitude < .01f) return true;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, sightHits, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            // A saturated result set cannot prove there is no blocker beyond it.
            if (count == sightHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = sightHits[i].collider.transform;
                if (hit == attacker || hit.IsChildOf(attacker) || hit == target.transform || hit.IsChildOf(target.transform)) continue;
                if (sightHits[i].distance < delta.magnitude - .04f) return false;
            }
            return true;
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
    }
}
