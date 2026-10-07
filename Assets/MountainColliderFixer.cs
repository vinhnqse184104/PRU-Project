using UnityEngine;

[ExecuteAlways]
public class MountainColliderFixer : MonoBehaviour
{
    private void Awake()
    {
        FixColliders();
    }

    private void Start()
    {
        FixColliders();
    }

    [ContextMenu("Fix Mountain Colliders Now")]
    public void FixColliders()
    {
        // 1. Remove CapsuleColliders, BoxColliders, and SphereColliders on Mountain and its children
        Collider[] oldColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in oldColliders)
        {
            if (col is MeshCollider || col is TerrainCollider) continue;

            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        // 2. Remove Rigidbody if any
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            if (Application.isPlaying) Destroy(rb);
            else DestroyImmediate(rb);
        }

        // 3. Find all MeshFilters in root and children
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.GetComponent<Terrain>() != null || mf.GetComponent<TerrainCollider>() != null) continue;

            GameObject go = mf.gameObject;
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
            }

            mc.sharedMesh = mf.sharedMesh;
            mc.isTrigger = false;
            mc.convex = false; // Concave mesh collider so cave entrance and interior are passable!
        }
    }
}
