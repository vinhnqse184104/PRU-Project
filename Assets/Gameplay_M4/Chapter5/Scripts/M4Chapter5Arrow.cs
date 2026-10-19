using UnityEngine;

namespace PRU.M4
{
    public sealed class M4Chapter5Arrow : MonoBehaviour
    {
        M4Chapter5Player owner;
        M4Chapter5Quest quest;
        Vector3 velocity;
        float remaining = 4;
        bool spent;
        public void Initialize(M4Chapter5Player shooter, M4Chapter5Quest targetQuest, Vector3 direction, float speed)
        { owner = shooter; quest = targetQuest; velocity = direction.normalized * speed; }
        public void Cancel() { spent = true; Destroy(gameObject); }
        void Update()
        {
            if (spent || owner == null || quest == null) { Destroy(gameObject); return; }
            remaining -= Time.deltaTime;
            if (remaining <= 0) { Destroy(gameObject); return; }
            Vector3 delta = velocity * Time.deltaTime;
            var hits = Physics.SphereCastAll(transform.position, .08f, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(owner.transform) || hit.transform.IsChildOf(transform)) continue;
                var eagle = hit.transform.GetComponentInParent<M4Chapter5Eagle>();
                if (eagle != null) quest.RegisterEagleHit(eagle);
                spent = true; Destroy(gameObject); return;
            }
            transform.position += delta;
        }
    }
}
