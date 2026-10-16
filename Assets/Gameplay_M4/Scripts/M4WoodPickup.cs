using UnityEngine;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4WoodPickup : MonoBehaviour
    {
        public M4WoodQuest quest;
        public Transform interactionPoint;
        private bool collected;
        public bool IsCollected => collected;
        public Vector3 InteractionPosition => interactionPoint != null ?
            interactionPoint.position : transform.position + Vector3.up * 0.25f;

        public bool TryPickup(M4Woodcutter actor)
        {
            return actor != null && actor.TryPickup(this);
        }

        internal bool Collect(M4Woodcutter actor)
        {
            if (collected || !isActiveAndEnabled || quest == null)
                return false;
            // Mark first: event subscribers cannot collect the same object again.
            collected = true;
            if (!quest.Collect(actor, this))
            {
                collected = false;
                return false;
            }
            gameObject.SetActive(false);
            return true;
        }
    }
}
