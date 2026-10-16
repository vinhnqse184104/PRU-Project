using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCut : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        bool ePressed = false;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ePressed = true;
        if (!ePressed) { try { ePressed = Input.GetKeyDown(KeyCode.E); } catch { } }

        if (ePressed && anim != null)
        {
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("chém") && !anim.IsInTransition(0))
            {
                anim.SetTrigger("CutTrigger");
            }
        }
    }
}