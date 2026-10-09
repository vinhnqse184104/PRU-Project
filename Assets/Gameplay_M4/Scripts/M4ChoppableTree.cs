using UnityEngine;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4ChoppableTree : MonoBehaviour
    {
        public M4WoodQuest quest;
        public GameObject standingVisual;
        public GameObject stumpVisual;
        public Collider[] standingColliders;
        public GameObject firewoodPrefab;
        public Transform woodSpawn;
        public Transform interactionPoint;
        public int hitsRequired = 3;
        private int hits;
        private bool chopped;

        public int Hits => hits;
        public int HitsRequired => Mathf.Max(1, hitsRequired);
        public bool IsChopped => chopped;
        public M4WoodPickup DroppedWood { get; private set; }
        public Vector3 InteractionPosition => interactionPoint != null ?
            interactionPoint.position : transform.position + Vector3.up;

        private void Start()
        {
            if (stumpVisual != null && !chopped)
                stumpVisual.SetActive(false);
        }

        public bool TryChop(M4Woodcutter actor, float timeNow)
        {
            return actor != null && actor.TryChop(this, timeNow);
        }

        internal bool ReceiveHit(M4Woodcutter actor)
        {
            if (chopped || !isActiveAndEnabled || quest == null || !quest.CanChop ||
                actor == null || actor.quest != quest || firewoodPrefab == null ||
                firewoodPrefab.GetComponent<M4WoodPickup>() == null)
                return false;
            hits++;
            if (hits < HitsRequired)
                return true;

            chopped = true;
            Collider[] colliders = standingColliders;
            if (colliders == null || colliders.Length == 0)
                colliders = standingVisual != null ? standingVisual.GetComponentsInChildren<Collider>() :
                    GetComponentsInChildren<Collider>();
            foreach (Collider treeCollider in colliders)
            {
                if (treeCollider == null || (stumpVisual != null &&
                    treeCollider.transform.IsChildOf(stumpVisual.transform)))
                    continue;
                treeCollider.enabled = false;
            }
            if (standingVisual != null && standingVisual != gameObject)
                standingVisual.SetActive(false);
            else
            {
                foreach (Renderer treeRenderer in GetComponentsInChildren<Renderer>())
                    if (stumpVisual == null || !treeRenderer.transform.IsChildOf(stumpVisual.transform))
                        treeRenderer.enabled = false;
            }
            if (stumpVisual != null)
                stumpVisual.SetActive(true);

            Vector3 position = woodSpawn != null ? woodSpawn.position :
                transform.position + new Vector3(0.8f, 0.15f, 0f);
            Quaternion rotation = woodSpawn != null ? woodSpawn.rotation : Quaternion.identity;
            GameObject droppedObject = Instantiate(firewoodPrefab, position, rotation);
            droppedObject.name = "Củi - " + name;
            DroppedWood = droppedObject.GetComponent<M4WoodPickup>();
            DroppedWood.quest = quest;
            droppedObject.SetActive(true);
            return true;
        }

        private void OnValidate()
        {
            hitsRequired = Mathf.Max(1, hitsRequired);
        }
    }
}
