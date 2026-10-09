using UnityEngine;

public class PlayerChopper : MonoBehaviour
{
    public float range = 3f;
    public float chestHeight = 1f;     // độ cao tia bắn ra từ Player
    public float radius = 0.5f;        // độ "dày" của tia, dễ trúng cây hơn
    public LayerMask hitLayers = ~0;   // mặc định: mọi layer

    void Update()
    {
        Vector3 origin = transform.position + Vector3.up * chestHeight;
        Vector3 dir = transform.forward;

        Debug.DrawRay(origin, dir * range, Color.red);

        if (Input.GetMouseButtonDown(0))
        {
            // SphereCast: tia có bề dày, dễ trúng thân cây hơn Raycast
            if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, range,
                                   hitLayers, QueryTriggerInteraction.Ignore))
            {
                Tree tree = hit.collider.GetComponentInParent<Tree>();
                if (tree != null) tree.Chop();
            }
        }
    }
}