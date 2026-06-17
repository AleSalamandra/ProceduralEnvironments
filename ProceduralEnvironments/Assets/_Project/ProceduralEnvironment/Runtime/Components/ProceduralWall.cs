using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Header("Corner Rounding")]
        [SerializeField] private bool roundCorners = false;
        [SerializeField] private float cornerRadius = 0.5f;
        [SerializeField] private int cornerSegments = 5;

        [Header("Editor Update")]
        [SerializeField] private bool autoUpdateInEditor = true;

        [Header("Features")]
        [SerializeField] private bool regenerateFeatures = true;
        [SerializeField] private bool autoCollectFeatures = true;
        [SerializeField] private List<ProceduralWallFeature> wallFeatures = new();

        [Header("Output")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

#if UNITY_EDITOR
        private bool regenerateQueued;
        private int lastEditorHash;
#endif

        private Mesh generatedMesh;

        public ProceduralStroke Stroke => stroke;
        public float Height => height;
        public float Thickness => thickness;
        public float TextureScale => textureScale;
        public bool RoundCorners => roundCorners;
        public float CornerRadius => cornerRadius;
        public int CornerSegments => cornerSegments;

        private void Reset()
        {
            EnsureComponents();
            CollectFeatures();
            RequestRegenerate();
        }

        private void OnEnable()
        {
            EnsureComponents();

            if (autoCollectFeatures)
                CollectFeatures();

#if UNITY_EDITOR
            lastEditorHash = ComputeEditorHash();
#endif

            Regenerate();
        }

        private void OnValidate()
        {
            EnsureComponents();

            height = Mathf.Max(0.01f, height);
            thickness = Mathf.Max(0.01f, thickness);
            textureScale = Mathf.Max(0.01f, textureScale);

            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 16);

            if (autoCollectFeatures)
                CollectFeatures();

            RequestRegenerate();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            if (!autoUpdateInEditor)
                return;

            int currentHash = ComputeEditorHash();

            if (currentHash == lastEditorHash)
                return;

            lastEditorHash = currentHash;
            RequestRegenerate();
#endif
        }

        [ContextMenu("Force Regenerate Wall")]
        public void ForceRegenerateWall()
        {
            Regenerate();
        }

        [ContextMenu("Collect Wall Features")]
        public void ForceCollectFeatures()
        {
            CollectFeatures();
            RequestRegenerate();
        }

        [ContextMenu("Clear All Feature Content")]
        public void ClearAllFeatureContent()
        {
            ProceduralWallFeature[] allFeatures = GetComponents<ProceduralWallFeature>();

            foreach (ProceduralWallFeature feature in allFeatures)
            {
                if (feature == null)
                    continue;

                feature.ClearGeneratedContent();
            }
        }

        public void RequestRegenerate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (regenerateQueued)
                    return;

                regenerateQueued = true;

                EditorApplication.delayCall += () =>
                {
                    regenerateQueued = false;

                    if (this == null)
                        return;

                    Regenerate();
                };

                return;
            }
#endif

            MarkDirty();
        }

        protected override void OnRegenerate()
        {
            if (meshFilter == null)
                return;

            List<WallOpeningData> openings = GetOpeningData();

            generatedMesh = WallMeshGenerator.Generate(
                stroke,
                transform,
                height,
                thickness,
                textureScale,
                roundCorners,
                cornerRadius,
                cornerSegments,
                openings
            );

            generatedMesh.name = "Generated_Wall_Mesh";
            generatedMesh.hideFlags = HideFlags.DontSave;

            meshFilter.sharedMesh = generatedMesh;

            RegenerateAssignedFeatures();

#if UNITY_EDITOR
            lastEditorHash = ComputeEditorHash();
#endif
        }

        public List<Vector3> GetGeneratedPathWorldPoints()
        {
            List<Vector3> localPoints = WallMeshGenerator.BuildLocalPath(
                stroke,
                transform,
                roundCorners,
                cornerRadius,
                cornerSegments
            );

            List<Vector3> worldPoints = new();

            for (int i = 0; i < localPoints.Count; i++)
            {
                worldPoints.Add(transform.TransformPoint(localPoints[i]));
            }

            return worldPoints;
        }

        public List<WallOpeningData> GetOpeningData()
        {
            List<WallOpeningData> openings = new();

            WallOpeningMarker[] markers = GetComponentsInChildren<WallOpeningMarker>();

            if (markers == null || markers.Length == 0)
                return openings;

            List<Vector3> pathPoints = GetGeneratedPathWorldPoints();

            if (pathPoints == null || pathPoints.Count < 2)
                return openings;

            List<float> distances = CalculatePathDistances(pathPoints);

            foreach (WallOpeningMarker marker in markers)
            {
                if (marker == null || !marker.isActiveAndEnabled)
                    continue;

                if (!TryProjectPointOnPath(
                    marker.transform.position,
                    pathPoints,
                    distances,
                    out float distanceAlongWall,
                    out Vector3 projectedPosition))
                {
                    continue;
                }

                WallOpeningData opening = new WallOpeningData
                {
                    OpeningType = marker.OpeningType,
                    CenterDistance = distanceAlongWall,
                    Width = marker.Width,
                    BottomHeight = marker.BottomHeight,
                    Height = marker.Height,
                    WorldPosition = projectedPosition
                };

                openings.Add(opening);
            }

            return openings;
        }

        private void RegenerateAssignedFeatures()
        {
            CleanNullFeatures();

            ProceduralWallFeature[] allFeatures = GetComponents<ProceduralWallFeature>();

            foreach (ProceduralWallFeature feature in allFeatures)
            {
                if (feature == null)
                    continue;

                bool isAssigned = wallFeatures.Contains(feature);

                if (!regenerateFeatures || !isAssigned)
                    feature.ClearGeneratedContent();
            }

            if (!regenerateFeatures)
                return;

            foreach (ProceduralWallFeature feature in wallFeatures)
            {
                if (feature == null)
                    continue;

                feature.Rebuild(this);
            }
        }

        private void CollectFeatures()
        {
            wallFeatures.Clear();

            ProceduralWallFeature[] foundFeatures = GetComponents<ProceduralWallFeature>();

            foreach (ProceduralWallFeature feature in foundFeatures)
            {
                if (feature == null)
                    continue;

                if (!wallFeatures.Contains(feature))
                    wallFeatures.Add(feature);
            }
        }

        private void CleanNullFeatures()
        {
            for (int i = wallFeatures.Count - 1; i >= 0; i--)
            {
                if (wallFeatures[i] == null)
                    wallFeatures.RemoveAt(i);
            }
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }

        private static List<float> CalculatePathDistances(List<Vector3> points)
        {
            List<float> distances = new();
            float accumulatedDistance = 0f;

            distances.Add(0f);

            for (int i = 1; i < points.Count; i++)
            {
                accumulatedDistance += Vector3.Distance(points[i - 1], points[i]);
                distances.Add(accumulatedDistance);
            }

            return distances;
        }

        private static bool TryProjectPointOnPath(
            Vector3 point,
            List<Vector3> pathPoints,
            List<float> distances,
            out float distanceAlongPath,
            out Vector3 projectedPoint)
        {
            distanceAlongPath = 0f;
            projectedPoint = Vector3.zero;

            if (pathPoints == null || pathPoints.Count < 2)
                return false;

            float bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Vector3 a = pathPoints[i];
                Vector3 b = pathPoints[i + 1];

                Vector3 segment = b - a;
                float segmentLengthSqr = segment.sqrMagnitude;

                if (segmentLengthSqr <= 0.0001f)
                    continue;

                float t = Vector3.Dot(point - a, segment) / segmentLengthSqr;
                t = Mathf.Clamp01(t);

                Vector3 candidate = Vector3.Lerp(a, b, t);
                float distanceSqr = (point - candidate).sqrMagnitude;

                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    projectedPoint = candidate;

                    float segmentLength = Mathf.Sqrt(segmentLengthSqr);
                    distanceAlongPath = distances[i] + segmentLength * t;
                }
            }

            return bestDistanceSqr < float.MaxValue;
        }

#if UNITY_EDITOR
        private int ComputeEditorHash()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 + Mathf.RoundToInt(height * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(thickness * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(textureScale * 1000f);

                hash = hash * 31 + roundCorners.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(cornerRadius * 1000f);
                hash = hash * 31 + cornerSegments;

                hash = hash * 31 + regenerateFeatures.GetHashCode();
                hash = hash * 31 + autoCollectFeatures.GetHashCode();

                if (stroke != null)
                {
                    hash = hash * 31 + stroke.Count;

                    for (int i = 0; i < stroke.Count; i++)
                    {
                        Vector3 point = stroke.GetPoint(i);

                        hash = hash * 31 + Mathf.RoundToInt(point.x * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(point.y * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(point.z * 1000f);
                    }
                }

                if (wallFeatures != null)
                {
                    hash = hash * 31 + wallFeatures.Count;

                    foreach (ProceduralWallFeature feature in wallFeatures)
                    {
                        if (feature == null)
                            continue;

                        hash = hash * 31 + feature.GetInstanceID();
                        hash = hash * 31 + feature.Generate.GetHashCode();
                    }
                }

                return hash;
            }
        }
#endif

        private void OnDrawGizmos()
        {
            if (stroke != null && stroke.Count > 0)
            {
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

            DrawOpeningDebugGizmos();
        }

        private void DrawOpeningDebugGizmos()
        {
            List<WallOpeningData> openings = GetOpeningData();

            if (openings == null || openings.Count == 0)
                return;

            Gizmos.color = Color.magenta;

            foreach (WallOpeningData opening in openings)
            {
                Gizmos.DrawSphere(opening.WorldPosition, 0.15f);
            }
        }
    }
}