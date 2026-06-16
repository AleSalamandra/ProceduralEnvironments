using UnityEngine;

namespace ProceduralEnvironment
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(ProceduralWall))]
    public class WallCornersFeature : ProceduralWallFeature
    {
        protected override string GeneratedRootName => "_GeneratedCorners";

        [Header("Corner Prefab")]
        [SerializeField] private GameObject cornerPrefab;

        [Header("Corner Transform")]
        [SerializeField] private Vector3 cornerScale = Vector3.one;
        [SerializeField] private float cornerYOffset = 0f;
        [SerializeField] private bool flipCornerNormal = false;

        [Header("Validation")]
        [SerializeField] private float minSegmentLength = 0.001f;

        protected override void OnRebuild(ProceduralWall wall)
        {
            if (cornerPrefab == null)
                return;

            ProceduralStroke stroke = wall.Stroke;

            if (stroke == null || stroke.Count < 3)
                return;

            Transform root = GetOrCreateGeneratedRoot();

            for (int i = 1; i < stroke.Count - 1; i++)
            {
                Vector3 previous = stroke.GetPoint(i - 1);
                Vector3 current = stroke.GetPoint(i);
                Vector3 next = stroke.GetPoint(i + 1);

                bool hasPreviousSegment = Vector3.Distance(previous, current) > minSegmentLength;
                bool hasNextSegment = Vector3.Distance(current, next) > minSegmentLength;

                if (!hasPreviousSegment || !hasNextSegment)
                    continue;

                Vector3 toPrevious = (previous - current).normalized;
                Vector3 toNext = (next - current).normalized;

                Vector3 bisector = (toPrevious + toNext).normalized;

                if (bisector.sqrMagnitude < 0.001f)
                    continue;

                if (flipCornerNormal)
                    bisector *= -1f;

                Vector3 position = current + Vector3.up * cornerYOffset;
                Quaternion rotation = Quaternion.LookRotation(bisector, Vector3.up);

                GameObject corner = SpawnPrefab(
                    cornerPrefab,
                    position,
                    rotation,
                    root
                );

                if (corner == null)
                    continue;

                corner.name = $"Corner_{i}";
                corner.transform.localScale = cornerScale;
            }
        }

        private void OnDrawGizmosSelected()
        {
            ProceduralWall wall = GetComponent<ProceduralWall>();

            if (wall == null || wall.Stroke == null || wall.Stroke.Count < 3)
                return;

            ProceduralStroke stroke = wall.Stroke;

            Gizmos.color = Color.cyan;

            for (int i = 1; i < stroke.Count - 1; i++)
            {
                Vector3 previous = stroke.GetPoint(i - 1);
                Vector3 current = stroke.GetPoint(i);
                Vector3 next = stroke.GetPoint(i + 1);

                if (Vector3.Distance(previous, current) <= minSegmentLength)
                    continue;

                if (Vector3.Distance(current, next) <= minSegmentLength)
                    continue;

                Vector3 toPrevious = (previous - current).normalized;
                Vector3 toNext = (next - current).normalized;

                Vector3 bisector = (toPrevious + toNext).normalized;

                if (bisector.sqrMagnitude < 0.001f)
                    continue;

                if (flipCornerNormal)
                    bisector *= -1f;

                Gizmos.DrawLine(current, current + bisector * 0.75f);
            }
        }
    }
}