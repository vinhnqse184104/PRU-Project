using System;
using UnityEngine;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class M4Chapter2HomeTrigger : MonoBehaviour
    {
        public M4Chapter2Quest quest;
        public event Action Entered;
        public event Action Exited;

        private BoxCollider zone;
        private bool playerInside;

        // The geometric check also handles spawning inside, editor warps and
        // re-enabled character controllers before the next physics callback.
        public bool ContainsPlayer
        {
            get { RefreshOccupancy(); return playerInside; }
        }

        private void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        private void FixedUpdate() => RefreshOccupancy();

        private bool BelongsToPlayer(Collider other)
        {
            if (quest == null || quest.player == null || other == null) return false;
            Transform actor = other.transform;
            return actor == quest.player || actor.IsChildOf(quest.player);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (BelongsToPlayer(other)) RefreshOccupancy();
        }

        private void OnTriggerStay(Collider other)
        {
            if (BelongsToPlayer(other)) RefreshOccupancy();
        }

        private void OnTriggerExit(Collider other)
        {
            if (BelongsToPlayer(other)) RefreshOccupancy();
        }

        private void RefreshOccupancy()
        {
            if (zone == null) zone = GetComponent<BoxCollider>();
            bool inside = false;
            if (isActiveAndEnabled && zone != null && zone.enabled && quest != null &&
                quest.player != null && quest.player.gameObject.activeInHierarchy)
            {
                Transform player = quest.player;
                CharacterController controller = player.GetComponentInChildren<CharacterController>();
                Vector3 center = controller != null ? controller.transform.TransformPoint(controller.center) : player.position;
                Vector3 offset = transform.InverseTransformPoint(center) - zone.center;
                Vector3 half = zone.size * 0.5f;
                inside = Mathf.Abs(offset.x) <= Mathf.Abs(half.x) + 0.001f &&
                         Mathf.Abs(offset.y) <= Mathf.Abs(half.y) + 0.001f &&
                         Mathf.Abs(offset.z) <= Mathf.Abs(half.z) + 0.001f;
            }
            SetOccupancy(inside);
        }

        private void SetOccupancy(bool inside)
        {
            if (inside == playerInside) return;
            playerInside = inside;
            if (inside) Entered?.Invoke();
            else Exited?.Invoke();
        }

        private void OnDisable() => SetOccupancy(false);

        private void OnValidate()
        {
            var collider = GetComponent<BoxCollider>();
            if (collider != null) collider.isTrigger = true;
        }
    }
}
