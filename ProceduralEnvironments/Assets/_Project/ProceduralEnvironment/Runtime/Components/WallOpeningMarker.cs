using UnityEngine;

namespace ProceduralEnvironment
{
    public enum WallOpeningType
    {
        Door,
        Window
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class WallOpeningMarker : MonoBehaviour
    {
        [Header("Opening Type")]
        [SerializeField] private WallOpeningType openingType = WallOpeningType.Door;

        [Header("Opening Size")]
        [SerializeField] private float width = 1f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float bottomHeight = 0f;

        [Header("Editor Display")]
        [SerializeField] private Color gizmoColor = new Color(0f, 0.7f, 1f, 0.25f);

        private BoxCollider boxCollider;

        public WallOpeningType OpeningType => openingType;
        public float Width => width;
        public float Height => height;
        public float BottomHeight => bottomHeight;
        public float TopHeight => bottomHeight + height;

        private void OnValidate()
        {
            width = Mathf.Max(0.01f, width);
            height = Mathf.Max(0.01f, height);
            bottomHeight = Mathf.Max(0f, bottomHeight);

            if (openingType == WallOpeningType.Door)
                bottomHeight = 0f;

            UpdateCollider();
            RequestWallRegenerate();
        }

        private void OnEnable()
        {
            UpdateCollider();
            RequestWallRegenerate();
        }

        private void OnDisable()
        {
            RequestWallRegenerate();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                RequestWallRegenerate();
#endif
        }

        private void UpdateCollider()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();

            if (boxCollider == null)
                return;

            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(width, height, 0.1f);
            boxCollider.center = new Vector3(0f, bottomHeight + height * 0.5f, 0f);
        }

        private void RequestWallRegenerate()
        {
            ProceduralWall wall = GetComponentInParent<ProceduralWall>();

            if (wall != null)
                wall.RequestRegenerate();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 center = new Vector3(0f, bottomHeight + height * 0.5f, 0f);
            Vector3 size = new Vector3(width, height, 0.08f);

            Gizmos.DrawCube(center, size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);

            Gizmos.matrix = previousMatrix;
        }
    }
}