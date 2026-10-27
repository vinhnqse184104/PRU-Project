using UnityEngine;
namespace PRU.M4
{
    public sealed class M4CaveTrap : MonoBehaviour
    {
        public M4Chapter6Quest quest;
        public Transform spikes;
        public Renderer signal;
        public Vector3 halfExtents=new Vector3(1.5f,.7f,1);
        public float period=4,warningTime=1.2f,activeTime=1,damage=15,hitCooldown=.8f,phaseOffset;
        float nextHit;
        public int HitsApplied { get; private set; }
        public bool IsDangerous { get; private set; }
        void FixedUpdate()
        {
            float phase=Mathf.Repeat(Time.time+phaseOffset,Mathf.Max(1,period));
            IsDangerous=quest!=null && quest.IsActive && phase>=warningTime && phase<warningTime+activeTime;
            if(spikes!=null)spikes.localScale=new Vector3(1,IsDangerous?1:.06f,1);
            if(signal!=null)
            {
                var block=new MaterialPropertyBlock();signal.GetPropertyBlock(block);
                Color color=IsDangerous?Color.red:phase<warningTime?new Color(1,.65f,.08f):new Color(.15f,.7f,.35f);
                block.SetColor("_BaseColor",color);block.SetColor("_Color",color);signal.SetPropertyBlock(block);
            }
            if(!IsDangerous || Time.time<nextHit || quest.player==null || !quest.player.health.IsAlive)return;
            foreach(var hit in Physics.OverlapBox(transform.position+Vector3.up*.5f,halfExtents,transform.rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(hit.transform.IsChildOf(quest.player.transform))
                {
                    if(quest.player.health.TakeDamage(damage,gameObject)){nextHit=Time.time+hitCooldown;HitsApplied++;}break;
                }
        }
    }
}
