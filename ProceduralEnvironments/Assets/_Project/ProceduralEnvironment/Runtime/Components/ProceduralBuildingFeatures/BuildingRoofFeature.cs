using UnityEngine;

namespace ProceduralEnvironment
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(ProceduralBuilding))]
    public class BuildingRoofFeature : ProceduralBuildingFeature
    {
        protected override string GeneratedRootName => "_GeneratedRoof";

        protected override bool ClearBeforeRebuild => false;

        [Header("Roof Shape")]
        [SerializeField] private float roofHeight = 1.25f;
        [SerializeField] private float overhang = 0.35f;
        [SerializeField] private float roofThickness = 0.18f;

        [Header("Rendering")]
        [SerializeField] private Material roofMaterial;

        public float RoofHeight => roofHeight;
        public float Overhang => overhang;
        public float RoofThickness => roofThickness;

        protected override void OnRebuild(ProceduralBuilding building)
        {
            roofHeight = Mathf.Max(0.01f, roofHeight);
            overhang = Mathf.Max(0f, overhang);
            roofThickness = Mathf.Max(0.01f, roofThickness);

            Transform root = GetOrCreateGeneratedRoot();
            GameObject roofObject = GetOrCreateRoofObject(root);

            MeshFilter meshFilter = roofObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = roofObject.GetComponent<MeshRenderer>();

            Mesh roofMesh = RoofMeshGenerator.Generate(
                building.FootprintType,
                building.Width,
                building.Depth,
                building.WallHeight,
                roofHeight,
                overhang,
                roofThickness,
                building.CircleSegments
            );

            meshFilter.sharedMesh = roofMesh;

            if (roofMaterial != null)
                meshRenderer.sharedMaterial = roofMaterial;
        }

        private GameObject GetOrCreateRoofObject(Transform root)
        {
            Transform existing = root.Find("Roof_Mesh");

            if (existing != null)
            {
                GameObject existingObject = existing.gameObject;

                if (existingObject.GetComponent<MeshFilter>() == null)
                    existingObject.AddComponent<MeshFilter>();

                if (existingObject.GetComponent<MeshRenderer>() == null)
                    existingObject.AddComponent<MeshRenderer>();

                existingObject.transform.localPosition = Vector3.zero;
                existingObject.transform.localRotation = Quaternion.identity;
                existingObject.transform.localScale = Vector3.one;

                return existingObject;
            }

            GameObject roofObject = new GameObject("Roof_Mesh");
            roofObject.transform.SetParent(root);
            roofObject.transform.localPosition = Vector3.zero;
            roofObject.transform.localRotation = Quaternion.identity;
            roofObject.transform.localScale = Vector3.one;

            roofObject.AddComponent<MeshFilter>();
            roofObject.AddComponent<MeshRenderer>();

            return roofObject;
        }

        protected override void OnValidate()
        {
            roofHeight = Mathf.Max(0.01f, roofHeight);
            overhang = Mathf.Max(0f, overhang);
            roofThickness = Mathf.Max(0.01f, roofThickness);

            base.OnValidate();
        }
    }
}