using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem;
   
     public class PlayerKick : MonoBehaviour
     {
        private Animator animator;
    
       void Start()
         {
            animator = GetComponent<Animator>();
        }

       void Update()
       {
             if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
               {
                   animator.SetTrigger("HighKickTrigger");
                }
        }
}
