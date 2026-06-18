using UnityEngine;

namespace ProceduralEnvironment
{
    public abstract class ProceduralBuildingFeature : MonoBehaviour
    {
        [Header("Feature")]
        [SerializeField] private bool generate = true;

        public bool Generate => generate;

        protected abstract string GeneratedRootName { get; }

        protected virtual bool ClearBeforeRebuild => true;

        public void Rebuild(ProceduralBuilding building)
        {
            if (ClearBeforeRebuild)
                ClearGeneratedContent();

            if (!generate)
                return;

            if (!isActiveAndEnabled)
                return;

            if (building == null)
                return;

            OnRebuild(building);
        }

        public void ClearGeneratedContent()
        {
            Transform root = transform.Find(GeneratedRootName);

            if (root == null)
                return;

            SafeDestroy(root.gameObject);
        }

        protected abstract void OnRebuild(ProceduralBuilding building);

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

        protected Transform GetBuildingGeneratedRoot(ProceduralBuilding building)
        {
            if (building == null)
                return null;

            return building.GetGeneratedRoot();
        }

        protected void RequestBuildingRegenerate()
        {
            ProceduralBuilding building = GetComponent<ProceduralBuilding>();

            if (building != null)
                building.RequestRegenerate();
        }

        protected virtual void OnValidate()
        {
            RequestBuildingRegenerate();
        }

        protected virtual void OnEnable()
        {
            RequestBuildingRegenerate();
        }

        protected virtual void OnDisable()
        {
            ClearGeneratedContent();
            RequestBuildingRegenerate();
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