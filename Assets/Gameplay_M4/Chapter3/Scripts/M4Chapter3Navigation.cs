using UnityEngine;
using UnityEngine.AI;

namespace PRU.M4
{
    // Register only the data belonging to this scene. Closing it leaves other scenes intact.
    [ExecuteAlways, DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class M4Chapter3Navigation : MonoBehaviour
    {
        public NavMeshData data;
        private NavMeshDataInstance instance;

        public void EnsureLoaded()
        {
            if (!isActiveAndEnabled || data == null || instance.valid) return;
            instance = NavMesh.AddNavMeshData(data, transform.position, transform.rotation);
            instance.owner = this;
        }

        private void OnEnable() => EnsureLoaded();
        private void OnDisable()
        {
            if (instance.valid) instance.Remove();
            instance = default;
        }
    }
}
