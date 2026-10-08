using UnityEngine;

public class CaveAreaVolumeTrigger : MonoBehaviour
{
    public GameObject caveVolume;

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.transform.root != null && (other.transform.root.CompareTag("Player") || other.transform.root.name.ToLower().Contains("thachsanh"))) return true;
        if (other.name.ToLower().Contains("thachsanh")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            if (caveVolume != null)
            {
                caveVolume.SetActive(true);
                Debug.Log("Player entered cave area - CaveVolume ENABLED");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            if (caveVolume != null)
            {
                caveVolume.SetActive(false);
                Debug.Log("Player left cave area - CaveVolume DISABLED");
            }
        }
    }
}
