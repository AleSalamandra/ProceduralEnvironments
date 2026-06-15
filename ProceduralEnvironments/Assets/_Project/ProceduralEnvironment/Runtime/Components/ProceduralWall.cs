using UnityEngine;

namespace ProceduralEnvironment
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralWall : ProceduralElement
    {
        [Header("Wall Data")]
        [SerializeField] private ProceduralStroke stroke = new();

        [Header("Wall Settings")]
        [SerializeField] private float height = 2.5f;
        [SerializeField] private float thickness = 0.35f;
        [SerializeField] private float textureScale = 1f;

        [Header("Output")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public ProceduralStroke Stroke => stroke;

        private void OnEnable()
        {
            EnsureComponents();
            Regenerate();
        }

        private void OnValidate()
        {
            EnsureComponents();

            height = Mathf.Max(0.01f, height);
            thickness = Mathf.Max(0.01f, thickness);
            textureScale = Mathf.Max(0.01f, textureScale);

            MarkDirty();
        }

        protected override void OnRegenerate()
        {
            if (meshFilter == null)
                return;

            Mesh mesh = WallMeshGenerator.Generate(
                stroke,
                transform,
                height,
                thickness,
                textureScale
            );

            meshFilter.sharedMesh = mesh;
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnDrawGizmos()
        {
            if (stroke == null || stroke.Count == 0)
                return;

            Gizmos.color = Color.yellow;

            for (int i = 0; i < stroke.Count; i++)
            {
                Vector3 point = stroke.GetPoint(i);
                Gizmos.DrawSphere(point, 0.12f);

                if (i < stroke.Count - 1)
                {
                    Vector3 nextPoint = stroke.GetPoint(i + 1);
                    Gizmos.DrawLine(point, nextPoint);
                }
            }
        }
    }
}