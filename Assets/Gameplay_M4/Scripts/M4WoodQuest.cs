using System;
using System.Collections.Generic;
using UnityEngine;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4WoodQuest : MonoBehaviour
    {
        public enum QuestState { NotStarted, Active, Completed }

        public int targetWood = 5;
        public M4QuestGiver questGiver;
        [SerializeField] private QuestState state;
        [SerializeField] private int collectedWood;
        private readonly HashSet<M4WoodPickup> collectedPickups = new HashSet<M4WoodPickup>();

        public QuestState State => state;
        public int CollectedWood => collectedWood;
        public int TargetWood => Mathf.Max(1, targetWood);
        public bool CanChop => state == QuestState.Active && collectedWood < TargetWood;

        // Changed fires once per accepted quest / collected log. WoodCollected carries
        // the amount added (always one); Inventory can subscribe without polling the HUD.
        public event Action<M4WoodQuest> Changed;
        public event Action<int> WoodCollected;
        public event Action Completed;

        public bool TryAcceptQuest(M4Woodcutter actor)
        {
            return actor != null && actor.quest == this && actor.TryAcceptQuest(questGiver);
        }

        internal bool Accept(M4Woodcutter actor, M4QuestGiver giver)
        {
            if (state != QuestState.NotStarted || actor == null || actor.quest != this ||
                giver == null || giver != questGiver || giver.quest != this)
                return false;
            state = QuestState.Active;
            Changed?.Invoke(this);
            return true;
        }

        internal bool Collect(M4Woodcutter actor, M4WoodPickup pickup)
        {
            if (!CanChop || actor == null || actor.quest != this || pickup == null ||
                pickup.quest != this || !collectedPickups.Add(pickup))
                return false;
            collectedWood++;
            bool finished = collectedWood == TargetWood;
            if (finished)
                state = QuestState.Completed;
            WoodCollected?.Invoke(1);
            Changed?.Invoke(this);
            if (finished)
                Completed?.Invoke();
            return true;
        }

        private void OnValidate()
        {
            targetWood = Mathf.Max(1, targetWood);
        }
    }
}
