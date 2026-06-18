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
    public class ProceduralWall : MonoBehaviour
    {
        [Header("Stroke")]
        [SerializeField] private ProceduralStroke stroke = new ProceduralStroke();
        [SerializeField] private bool closedLoop;

        [Header("Wall Shape")]
        [SerializeField] private float height = 3f;
        [SerializeField] private float thickness = 0.35f;
        [SerializeField] private float textureScale = 1f;

        [Header("Rounded Wall Corners")]
        [SerializeField] private bool roundCorners;
        [SerializeField] private float cornerRadius = 0.5f;
        [SerializeField] private int cornerSegments = 5;

        [Header("Rendering")]
        [SerializeField] private Material wallMaterial;

        [Header("Editor Update")]
        [SerializeField] private bool autoUpdateInEditor = true;

        [Header("Features")]
        [SerializeField] private bool generateFeatures = true;
        [SerializeField] private bool autoCollectFeatures = true;
        [SerializeField] private List<ProceduralWallFeature> wallFeatures = new List<ProceduralWallFeature>();

        [HideInInspector][SerializeField] private MeshFilter meshFilter;
        [HideInInspector][SerializeField] private MeshRenderer meshRenderer;

#if UNITY_EDITOR
        private bool regenerateQueued;
        private int lastEditorHash;
#endif

        public ProceduralStroke Stroke => stroke;
        public bool ClosedLoop => closedLoop;

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

            RequestRegenerate();
        }

        private void OnValidate()
        {
            height = Mathf.Max(0.01f, height);
            thickness = Mathf.Max(0.01f, thickness);
            textureScale = Mathf.Max(0.0001f, textureScale);

            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 32);

            EnsureComponents();

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

            Regenerate();
        }

        public void Regenerate()
        {
            EnsureComponents();

            Mesh mesh = WallMeshGenerator.Generate(
                stroke,
                transform,
                height,
                thickness,
                textureScale,
                roundCorners,
                cornerRadius,
                cornerSegments,
                closedLoop,
                GetOpeningData()
            );

            meshFilter.sharedMesh = mesh;

            if (wallMaterial != null)
                meshRenderer.sharedMaterial = wallMaterial;

            RegenerateAssignedFeatures();

#if UNITY_EDITOR
            lastEditorHash = ComputeEditorHash();
#endif
        }

        public void SetClosedLoop(bool value)
        {
            closedLoop = value;
            RequestRegenerate();
        }

        public void SetWallSettings(float newHeight, float newThickness)
        {
            SetWallSettings(newHeight, newThickness, textureScale);
        }

        public void SetWallSettings(float newHeight, float newThickness, float newTextureScale)
        {
            height = Mathf.Max(0.01f, newHeight);
            thickness = Mathf.Max(0.01f, newThickness);
            textureScale = Mathf.Max(0.0001f, newTextureScale);

            RequestRegenerate();
        }

        public void SetStrokeWorldPoints(List<Vector3> worldPoints)
        {
            if (stroke == null)
                stroke = new ProceduralStroke();

            stroke.SetWorldPoints(worldPoints);

            RequestRegenerate();
        }

        public List<Vector3> GetGeneratedPathWorldPoints()
        {
            List<Vector3> localPath = WallMeshGenerator.BuildLocalPath(
                stroke,
                transform,
                roundCorners,
                cornerRadius,
                cornerSegments,
                closedLoop
            );

            List<Vector3> worldPath = new List<Vector3>();

            if (localPath == null)
                return worldPath;

            for (int i = 0; i < localPath.Count; i++)
                worldPath.Add(transform.TransformPoint(localPath[i]));

            return worldPath;
        }

        public List<WallOpeningData> GetOpeningData()
        {
            List<WallOpeningData> result = new List<WallOpeningData>();

            WallOpeningMarker[] markers = GetComponentsInChildren<WallOpeningMarker>(true);

            if (markers == null || markers.Length == 0)
                return result;

            List<Vector3> path = GetGeneratedPathWorldPoints();

            if (path == null || path.Count < 2)
                return result;

            List<float> distances = CalculateWorldDistances(path);

            for (int i = 0; i < markers.Length; i++)
            {
                WallOpeningMarker marker = markers[i];

                if (marker == null)
                    continue;

                if (!TryProjectPointOnPath(
                    marker.transform.position,
                    path,
                    distances,
                    out float projectedDistance,
                    out Vector3 projectedPoint))
                {
                    continue;
                }

                WallOpeningData data = new WallOpeningData
                {
                    OpeningType = marker.OpeningType,
                    CenterDistance = projectedDistance,
                    Width = marker.Width,
                    BottomHeight = marker.BottomHeight,
                    Height = marker.Height,
                    RoundTopCorners = marker.RoundTopCorners,
                    TopCornerRadius = marker.TopCornerRadius,
                    TopCornerSegments = marker.TopCornerSegments,
                    WorldPosition = projectedPoint
                };

                result.Add(data);
            }

            result.Sort((a, b) => a.CenterDistance.CompareTo(b.CenterDistance));

            return result;
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
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

                if (!generateFeatures || !isAssigned)
                    feature.ClearGeneratedContent();
            }

            if (!generateFeatures)
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

        private static List<float> CalculateWorldDistances(List<Vector3> points)
        {
            List<float> distances = new List<float>();
            float accumulated = 0f;

            distances.Add(0f);

            for (int i = 1; i < points.Count; i++)
            {
                accumulated += Vector3.Distance(points[i - 1], points[i]);
                distances.Add(accumulated);
            }

            return distances;
        }

        private static bool TryProjectPointOnPath(
            Vector3 point,
            List<Vector3> path,
            List<float> distances,
            out float projectedDistance,
            out Vector3 projectedPoint)
        {
            projectedDistance = 0f;
            projectedPoint = Vector3.zero;

            if (path == null || path.Count < 2)
                return false;

            float bestSqrDistance = float.MaxValue;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];

                Vector3 segment = b - a;
                float segmentLengthSqr = segment.sqrMagnitude;

                if (segmentLengthSqr <= 0.000001f)
                    continue;

                float t = Vector3.Dot(point - a, segment) / segmentLengthSqr;
                t = Mathf.Clamp01(t);

                Vector3 candidate = a + segment * t;
                float sqrDistance = (point - candidate).sqrMagnitude;

                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    projectedPoint = candidate;

                    float segmentLength = Mathf.Sqrt(segmentLengthSqr);
                    projectedDistance = distances[i] + segmentLength * t;
                }
            }

            return bestSqrDistance < float.MaxValue;
        }

#if UNITY_EDITOR
        private int ComputeEditorHash()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 + closedLoop.GetHashCode();

                hash = hash * 31 + Mathf.RoundToInt(height * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(thickness * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(textureScale * 1000f);

                hash = hash * 31 + roundCorners.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(cornerRadius * 1000f);
                hash = hash * 31 + cornerSegments;

                hash = hash * 31 + generateFeatures.GetHashCode();
                hash = hash * 31 + autoCollectFeatures.GetHashCode();

                if (wallMaterial != null)
                    hash = hash * 31 + wallMaterial.GetInstanceID();

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

                WallOpeningMarker[] markers = GetComponentsInChildren<WallOpeningMarker>(true);

                if (markers != null)
                {
                    hash = hash * 31 + markers.Length;

                    foreach (WallOpeningMarker marker in markers)
                    {
                        if (marker == null)
                            continue;

                        Vector3 position = marker.transform.position;

                        hash = hash * 31 + marker.GetInstanceID();
                        hash = hash * 31 + Mathf.RoundToInt(position.x * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(position.y * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(position.z * 1000f);

                        hash = hash * 31 + marker.OpeningType.GetHashCode();
                        hash = hash * 31 + Mathf.RoundToInt(marker.Width * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(marker.Height * 1000f);
                        hash = hash * 31 + Mathf.RoundToInt(marker.BottomHeight * 1000f);

                        hash = hash * 31 + marker.RoundTopCorners.GetHashCode();
                        hash = hash * 31 + Mathf.RoundToInt(marker.TopCornerRadius * 1000f);
                        hash = hash * 31 + marker.TopCornerSegments;
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
            List<Vector3> points = GetGeneratedPathWorldPoints();

            if (points == null || points.Count < 2)
                return;

            Gizmos.color = Color.yellow;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
                Gizmos.DrawSphere(points[i], 0.05f);
            }

            List<WallOpeningData> openings = GetOpeningData();

            Gizmos.color = Color.magenta;

            for (int i = 0; i < openings.Count; i++)
                Gizmos.DrawSphere(openings[i].WorldPosition, 0.08f);
        }
    }
}