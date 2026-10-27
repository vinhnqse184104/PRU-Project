using UnityEngine;
namespace PRU.M4
{
    public sealed class M4CaveInteractable : MonoBehaviour
    {
        public M4Chapter6Quest quest;
        public string label;
        public Renderer glow;
        public void SetLit(bool lit)
        {
            if(glow==null)return;
            var block=new MaterialPropertyBlock();glow.GetPropertyBlock(block);
            Color color=lit?new Color(.95f,.73f,.15f):new Color(.2f,.3f,.4f);
            block.SetColor("_BaseColor",color);block.SetColor("_Color",color);glow.SetPropertyBlock(block);
        }
    }
}
