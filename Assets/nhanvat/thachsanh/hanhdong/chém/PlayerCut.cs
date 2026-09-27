using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCut : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Nhấn phím E để đấm (Bạn có thể đổi chữ tKey sang phím khác tùy ý)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (anim != null)
            {
                anim.SetTrigger("CutTrigger");
            }
        }
    }
}