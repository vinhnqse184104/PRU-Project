using UnityEngine;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4QuestGiver : MonoBehaviour
    {
        public M4WoodQuest quest;
        public Transform interactionPoint;
        public float interactionRange = 2.5f;
        public Vector3 InteractionPosition => interactionPoint != null ?
            interactionPoint.position : transform.position + Vector3.up;

        public bool TryAcceptQuest(M4Woodcutter actor)
        {
            return actor != null && actor.TryAcceptQuest(this);
        }
    }
}
