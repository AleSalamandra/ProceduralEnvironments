using UnityEngine;

namespace ProceduralEnvironment
{
    public abstract class ProceduralWallFeature : MonoBehaviour
    {
        [Header("Feature")]
        [SerializeField] private bool generate = true;

        public bool Generate => generate;

        protected abstract string GeneratedRootName { get; }

        public void Rebuild(ProceduralWall wall)
        {
            ClearGeneratedContent();

            if (!generate)
                return;

            if (!isActiveAndEnabled)
                return;

            if (wall == null)
                return;

            OnRebuild(wall);
        }

        public void ClearGeneratedContent()
        {
            Transform root = transform.Find(GeneratedRootName);

            if (root == null)
                return;

            SafeDestroy(root.gameObject);
        }

        protected abstract void OnRebuild(ProceduralWall wall);

        protected Transform GetOrCreateGeneratedRoot()
        {
            Transform existingRoot = transform.Find(GeneratedRootName);

            if (existingRoot != null)
                return existingRoot;

            GameObject rootObject = new GameObject(GeneratedRootName);
            rootObject.transform.SetParent(transform);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            return rootObject.transform;
        }

        protected GameObject SpawnPrefab(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            if (prefab == null)
                return null;

            return Instantiate(prefab, position, rotation, parent);
        }

        protected void RequestWallRegenerate()
        {
            ProceduralWall wall = GetComponent<ProceduralWall>();

            if (wall != null)
                wall.RequestRegenerate();
        }

        protected virtual void OnValidate()
        {
            RequestWallRegenerate();
        }

        protected virtual void OnEnable()
        {
            RequestWallRegenerate();
        }

        protected virtual void OnDisable()
        {
            ClearGeneratedContent();
            RequestWallRegenerate();
        }

        private void SafeDestroy(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}