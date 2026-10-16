using UnityEngine;
using UnityEngine.AI;

namespace PRU.M4
{
    // Own only this scene's data instance; never remove navigation owned by another scene.
    [ExecuteAlways, DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class M4Chapter2Navigation : MonoBehaviour
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
