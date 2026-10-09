using UnityEngine;

[RequireComponent(typeof(CaveFadeTeleport))]
public class CaveExitTrigger : MonoBehaviour
{
    private void Awake()
    {
        CaveFadeTeleport fadeTeleport = GetComponent<CaveFadeTeleport>();
        if (fadeTeleport != null)
        {
            fadeTeleport.isEnteringCave = false;
        }
    }
}
