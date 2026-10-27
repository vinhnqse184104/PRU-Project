using UnityEngine;

namespace PRU.M4
{
    // A cradle pose layered after locomotion; no shared animation clips are edited.
    [DefaultExecutionOrder(1000), DisallowMultipleComponent]
    public sealed class M4CaveCarryPose : MonoBehaviour
    {
        public Animator characterAnimator;
        public Transform carriedVisual;
        Transform leftUpper, leftLower, leftHand, rightUpper, rightLower, rightHand;
        Renderer[] renderers;
        bool initialized;

        void Initialize()
        {
            if (initialized || characterAnimator == null || !characterAnimator.isHuman || carriedVisual == null) return;
            leftUpper=characterAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLower=characterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand=characterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpper=characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLower=characterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand=characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            renderers=carriedVisual.GetComponentsInChildren<Renderer>(true);
            carriedVisual.localRotation=Quaternion.Euler(0,90,180);
            initialized=true;
        }
        void LateUpdate()
        {
            Initialize();
            if(!initialized || !carriedVisual.gameObject.activeInHierarchy || leftUpper==null || rightUpper==null)return;
            carriedVisual.localRotation=Quaternion.Euler(0,90,180);
            float shoulderHeight=(leftUpper.position.y+rightUpper.position.y)*.5f-transform.position.y;
            float height=shoulderHeight-.23f;
            Vector3 center=transform.TransformPoint(new Vector3(0,height,.63f));
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;
                for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                carriedVisual.position+=center-bounds.center;
            }
            SolveArm(leftUpper,leftLower,leftHand,transform.TransformPoint(new Vector3(-.33f,height-.07f,.30f)),transform.TransformPoint(new Vector3(-.55f,height-.42f,.10f)));
            SolveArm(rightUpper,rightLower,rightHand,transform.TransformPoint(new Vector3(.33f,height-.07f,.30f)),transform.TransformPoint(new Vector3(.55f,height-.42f,.10f)));
        }
        static void SolveArm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 hint)
        {
            if(upper==null || lower==null || hand==null)return;
            Vector3 origin=upper.position;
            float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
            Vector3 delta=target-origin;if(a<.001f||b<.001f||delta.sqrMagnitude<.0001f)return;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
            Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(hint-origin,direction).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(direction,Vector3.right).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            Vector3 elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            Vector3 end=origin+direction*distance;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
        }
    }
}
