using UnityEngine;

namespace PRU.M4
{
    public sealed class M4Chapter5Eagle : MonoBehaviour
    {
        public M4Chapter5Quest quest;
        public Transform escapePoint;
        public float patrolRadius = 2.2f, escapeSpeed = 12;
        Vector3 start;
        bool escaped;
        public bool HasEscaped => escaped;
        void Awake() { start = transform.position; }
        void Update()
        {
            if (quest == null || escaped) return;
            if (quest.State == M4Chapter5Quest.QuestState.HuntEagle)
            {
                float t = Time.time * .55f;
                transform.position = start + new Vector3(Mathf.Sin(t) * patrolRadius, Mathf.Sin(t * 1.7f) * .3f, Mathf.Cos(t) * .7f);
                transform.rotation = Quaternion.Euler(0, Mathf.Sin(t) * 18f, 0);
            }
            else if (escapePoint != null)
            {
                Vector3 direction = escapePoint.position - transform.position;
                if (direction.sqrMagnitude < 1) { escaped = true; gameObject.SetActive(false); return; }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 3 * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, escapePoint.position, escapeSpeed * Time.deltaTime);
            }
        }
        public void ResetFlight() { escaped = false; transform.position = start; transform.rotation = Quaternion.identity; gameObject.SetActive(true); }
    }
}
