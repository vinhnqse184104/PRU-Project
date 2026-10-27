using UnityEngine;
namespace PRU.M4
{
    public sealed class M4CaveGuardianVisual : MonoBehaviour
    {
        public M4Chapter3Boss brain;
        public Transform visual;
        public Collider body;
        Vector3 rest;
        void Start(){if(visual!=null)rest=visual.localPosition;}
        void Update()
        {
            if(brain==null||brain.health==null)return;
            if(body!=null)body.enabled=brain.health.IsAlive;
            if(visual!=null)
            {
                visual.gameObject.SetActive(brain.health.IsAlive);
                visual.localPosition=rest+Vector3.up*(.12f+Mathf.Sin(Time.time*3)*.1f);
                if(brain.health.IsAlive)visual.Rotate(0,35*Time.deltaTime,0);
            }
        }
        void OnGUI()
        {
            if(brain==null||brain.health==null||!brain.health.IsAlive||brain.player==null||brain.player.viewCamera==null)return;
            var camera=brain.player.viewCamera;var point=camera.WorldToScreenPoint(transform.position+Vector3.up*2);
            if(point.z<0 || Vector3.Distance(transform.position,brain.player.transform.position)>12)return;
            var box=new Rect(point.x-100,Screen.height-point.y,200,52);
            M4QuestDrawing.Panel(box,new Color(.04f,.06f,.07f,.9f));
            bool warning=brain.State==M4Chapter3Boss.BossState.Telegraph||brain.State==M4Chapter3Boss.BossState.Strike;
            GUI.Label(box,(warning?"SẮP ĐÁNH — NÉ!":"LINH THẠCH (MẪU TẠM)")+"\nHP "+Mathf.CeilToInt(brain.health.CurrentHealth),M4QuestDrawing.Text(15,warning?Color.yellow:Color.white,true));
        }
    }
}
