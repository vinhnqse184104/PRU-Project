using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKick : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        bool fPressed = false;
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
        if (!fPressed) { try { fPressed = Input.GetKeyDown(KeyCode.F); } catch { } }

        if (fPressed && animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("đá") && !animator.IsInTransition(0))
            {
                animator.SetTrigger("HighKickTrigger");
            }
        }
    }
}

