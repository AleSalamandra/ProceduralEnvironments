using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralEnvironment
{
    public enum BuildingFootprintType
    {
        Rectangle,
        Circle,
        Hexagon
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class ProceduralBuilding : MonoBehaviour
    {
        [Header("Footprint")]
        [SerializeField] private BuildingFootprintType footprintType = BuildingFootprintType.Rectangle;
        [SerializeField] private float width = 6f;
        [SerializeField] private float depth = 4f;

        [Header("Circle")]
        [SerializeField] private int circleSegments = 24;

        [Header("Rounded Corners")]
        [SerializeField] private bool roundCorners = false;
        [SerializeField] private float cornerRadius = 0.5f;
        [SerializeField] private int cornerSegments = 5;

        [Header("Wall")]
        [SerializeField] private ProceduralWall wallPrefab;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.35f;
        [SerializeField] private float wallTextureScale = 1f;

        [Header("Generated Objects")]
        [SerializeField] private string generatedRootName = "_GeneratedBuilding";
        [SerializeField] private string generatedWallName = "Building_Walls";

        [Header("Editor Update")]
        [SerializeField] private bool autoUpdateInEditor = true;

        [Header("Features")]
        [SerializeField] private bool regenerateFeatures = true;
        [SerializeField] private bool autoCollectFeatures = true;
        [SerializeField] private List<ProceduralBuildingFeature> buildingFeatures = new List<ProceduralBuildingFeature>();

#if UNITY_EDITOR
        private bool regenerateQueued;
        private int lastEditorHash;
#endif

        public BuildingFootprintType FootprintType => footprintType;
        public float Width => width;
        public float Depth => depth;
        public float WallHeight => wallHeight;
        public float WallThickness => wallThickness;
        public float WallTextureScale => wallTextureScale;
        public int CircleSegments => circleSegments;

        public bool SupportsRoundedCorners =>
            footprintType == BuildingFootprintType.Rectangle ||
            footprintType == BuildingFootprintType.Hexagon;

        private void Reset()
        {
            CollectFeatures();
            RequestRegenerate();
        }

        private void OnEnable()
        {
            if (autoCollectFeatures)
                CollectFeatures();

#if UNITY_EDITOR
            lastEditorHash = ComputeEditorHash();
#endif

            RequestRegenerate();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.01f, width);
            depth = Mathf.Max(0.01f, depth);

            circleSegments = Mathf.Clamp(circleSegments, 8, 128);

            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 32);

            wallHeight = Mathf.Max(0.01f, wallHeight);
            wallThickness = Mathf.Max(0.01f, wallThickness);
            wallTextureScale = Mathf.Max(0.0001f, wallTextureScale);

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

        [ContextMenu("Force Regenerate Building")]
        public void ForceRegenerateBuilding()
        {
            Regenerate();
        }

        [ContextMenu("Collect Building Features")]
        public void ForceCollectFeatures()
        {
            CollectFeatures();
            RequestRegenerate();
        }

        [ContextMenu("Clear Building Feature Content")]
        public void ClearFeatureContent()
        {
            ProceduralBuildingFeature[] features = GetComponents<ProceduralBuildingFeature>();

            foreach (ProceduralBuildingFeature feature in features)
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
            UpdateGeneratedWall();
            RegenerateAssignedFeatures();

#if UNITY_EDITOR
            lastEditorHash = ComputeEditorHash();
#endif
        }

        public List<Vector3> GetFootprintWorldPoints(bool closed)
        {
            List<Vector3> localPoints = BuildLocalFootprintPoints();
            List<Vector3> worldPoints = new List<Vector3>();

            for (int i = 0; i < localPoints.Count; i++)
                worldPoints.Add(transform.TransformPoint(localPoints[i]));

            if (closed && worldPoints.Count > 0)
                worldPoints.Add(worldPoints[0]);

            return worldPoints;
        }

        public Transform GetGeneratedRoot()
        {
            Transform existingRoot = transform.Find(generatedRootName);

            if (existingRoot != null)
                return existingRoot;

            GameObject rootObject = new GameObject(generatedRootName);
            rootObject.transform.SetParent(transform);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            return rootObject.transform;
        }

        private void UpdateGeneratedWall()
        {
            Transform root = GetGeneratedRoot();
            ProceduralWall wall = GetOrCreateGeneratedWall(root);

            if (wall == null)
                return;

            List<Vector3> footprintPoints = GetFootprintWorldPoints(true);

            bool useRoundedCorners =
                SupportsRoundedCorners &&
                roundCorners &&
                cornerRadius > 0.001f;

            wall.name = generatedWallName;
            wall.transform.SetParent(root);
            wall.transform.localPosition = Vector3.zero;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one;

            wall.SetClosedLoop(true);
            wall.SetStrokeWorldPoints(footprintPoints);
            wall.SetWallSettings(wallHeight, wallThickness, wallTextureScale);
            wall.SetCornerSettings(useRoundedCorners, cornerRadius, cornerSegments);
            wall.ForceRegenerateWall();
        }

        private ProceduralWall GetOrCreateGeneratedWall(Transform root)
        {
            Transform existingWallTransform = root.Find(generatedWallName);

            if (existingWallTransform != null)
            {
                ProceduralWall existingWall = existingWallTransform.GetComponent<ProceduralWall>();

                if (existingWall != null)
                    return existingWall;
            }

            ProceduralWall newWall;

            if (wallPrefab != null)
            {
                newWall = Instantiate(wallPrefab, root);
                newWall.name = generatedWallName;
            }
            else
            {
                GameObject wallObject = new GameObject(generatedWallName);
                wallObject.transform.SetParent(root);
                wallObject.transform.localPosition = Vector3.zero;
                wallObject.transform.localRotation = Quaternion.identity;
                wallObject.transform.localScale = Vector3.one;

                newWall = wallObject.AddComponent<ProceduralWall>();
            }

            return newWall;
        }

        private List<Vector3> BuildLocalFootprintPoints()
        {
            switch (footprintType)
            {
                case BuildingFootprintType.Circle:
                    return BuildCircleFootprint();

                case BuildingFootprintType.Hexagon:
                    return BuildHexagonFootprint();

                case BuildingFootprintType.Rectangle:
                default:
                    return BuildRectangleFootprint();
            }
        }

        private List<Vector3> BuildRectangleFootprint()
        {
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;

            return new List<Vector3>
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f,  halfDepth),
                new Vector3( halfWidth, 0f,  halfDepth),
                new Vector3( halfWidth, 0f, -halfDepth)
            };
        }

        private List<Vector3> BuildCircleFootprint()
        {
            return BuildRegularPolygonFootprint(circleSegments);
        }

        private List<Vector3> BuildHexagonFootprint()
        {
            return BuildRegularPolygonFootprint(6);
        }

        private List<Vector3> BuildRegularPolygonFootprint(int segments)
        {
            List<Vector3> points = new List<Vector3>();

            float radiusX = width * 0.5f;
            float radiusZ = depth * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = -t * Mathf.PI * 2f + Mathf.PI * 0.5f;

                float x = Mathf.Cos(angle) * radiusX;
                float z = Mathf.Sin(angle) * radiusZ;

                points.Add(new Vector3(x, 0f, z));
            }

            return points;
        }

        private void RegenerateAssignedFeatures()
        {
            CleanNullFeatures();

            ProceduralBuildingFeature[] allFeatures = GetComponents<ProceduralBuildingFeature>();

            foreach (ProceduralBuildingFeature feature in allFeatures)
            {
                if (feature == null)
                    continue;

                bool isAssigned = buildingFeatures.Contains(feature);

                if (!regenerateFeatures || !isAssigned)
                    feature.ClearGeneratedContent();
            }

            if (!regenerateFeatures)
                return;

            foreach (ProceduralBuildingFeature feature in buildingFeatures)
            {
                if (feature == null)
                    continue;

                feature.Rebuild(this);
            }
        }

        private void CollectFeatures()
        {
            buildingFeatures.Clear();

            ProceduralBuildingFeature[] foundFeatures = GetComponents<ProceduralBuildingFeature>();

            foreach (ProceduralBuildingFeature feature in foundFeatures)
            {
                if (feature == null)
                    continue;

                if (!buildingFeatures.Contains(feature))
                    buildingFeatures.Add(feature);
            }
        }

        private void CleanNullFeatures()
        {
            for (int i = buildingFeatures.Count - 1; i >= 0; i--)
            {
                if (buildingFeatures[i] == null)
                    buildingFeatures.RemoveAt(i);
            }
        }

#if UNITY_EDITOR
        private int ComputeEditorHash()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 + footprintType.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(width * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(depth * 1000f);
                hash = hash * 31 + circleSegments;

                hash = hash * 31 + roundCorners.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(cornerRadius * 1000f);
                hash = hash * 31 + cornerSegments;

                hash = hash * 31 + Mathf.RoundToInt(wallHeight * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(wallThickness * 1000f);
                hash = hash * 31 + Mathf.RoundToInt(wallTextureScale * 1000f);

                hash = hash * 31 + regenerateFeatures.GetHashCode();
                hash = hash * 31 + autoCollectFeatures.GetHashCode();

                if (wallPrefab != null)
                    hash = hash * 31 + wallPrefab.GetInstanceID();

                if (buildingFeatures != null)
                {
                    hash = hash * 31 + buildingFeatures.Count;

                    foreach (ProceduralBuildingFeature feature in buildingFeatures)
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
            List<Vector3> points = GetFootprintWorldPoints(true);

            if (points == null || points.Count < 2)
                return;

            Gizmos.color = Color.green;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
                Gizmos.DrawSphere(points[i], 0.06f);
            }
        }
    }
}