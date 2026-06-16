using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    public enum BrickPatternType
    {
        Matrix,
        MatrixWithOffset,
        RandomHorizontal,
        HorizontalVerticalRows
    }

    public enum WallSurfaceSide
    {
        Front,
        Back,
        Both
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(ProceduralWall))]
    public class WallBricksFeature : ProceduralWallFeature
    {
        protected override string GeneratedRootName => "_GeneratedBricks";

        [Header("Brick Prefab")]
        [SerializeField] private GameObject brickPrefab;

        [Header("Brick Pattern")]
        [SerializeField] private BrickPatternType brickPattern = BrickPatternType.Matrix;
        [SerializeField] private WallSurfaceSide brickSide = WallSurfaceSide.Front;

        [Header("Brick Transform")]
        [SerializeField] private Vector3 brickPrefabScale = Vector3.one;
        [SerializeField] private float brickZOffset = 0.02f;

        [Header("Brick Size")]
        [SerializeField] private bool usePrefabBoundsForSpacing = true;
        [SerializeField] private float manualBrickLength = 0.5f;
        [SerializeField] private float manualBrickHeight = 0.25f;
        [SerializeField] private float horizontalGap = 0.02f;
        [SerializeField] private float verticalGap = 0.02f;

        [Header("Brick Distribution")]
        [SerializeField] private float randomHorizontalAmount = 0.05f;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private int maxBricks = 1000;

        protected override void OnValidate()
        {
            manualBrickLength = Mathf.Max(0.01f, manualBrickLength);
            manualBrickHeight = Mathf.Max(0.01f, manualBrickHeight);
            horizontalGap = Mathf.Max(0f, horizontalGap);
            verticalGap = Mathf.Max(0f, verticalGap);
            maxBricks = Mathf.Max(0, maxBricks);

            base.OnValidate();
        }

        protected override void OnRebuild(ProceduralWall wall)
        {
            if (brickPrefab == null)
                return;

            List<Vector3> centerPath = wall.GetGeneratedPathWorldPoints();

            if (centerPath == null || centerPath.Count < 2)
                return;

            Transform root = GetOrCreateGeneratedRoot();
            System.Random random = new System.Random(randomSeed);

            int brickCount = 0;

            if (brickSide == WallSurfaceSide.Front || brickSide == WallSurfaceSide.Both)
            {
                brickCount = GenerateBricksOnSurfacePath(
                    wall,
                    centerPath,
                    1f,
                    root,
                    random,
                    brickCount
                );
            }

            if (brickSide == WallSurfaceSide.Back || brickSide == WallSurfaceSide.Both)
            {
                brickCount = GenerateBricksOnSurfacePath(
                    wall,
                    centerPath,
                    -1f,
                    root,
                    random,
                    brickCount
                );
            }
        }

        private int GenerateBricksOnSurfacePath(
            ProceduralWall wall,
            List<Vector3> centerPath,
            float sideMultiplier,
            Transform parent,
            System.Random random,
            int brickCount)
        {
            List<Vector3> surfacePath = BuildSurfacePath(
                centerPath,
                wall.Thickness * 0.5f,
                sideMultiplier
            );

            if (surfacePath.Count < 2)
                return brickCount;

            List<float> distances = CalculateDistances(surfacePath);
            float totalLength = distances[distances.Count - 1];

            if (totalLength <= 0.001f)
                return brickCount;

            Vector2 brickSize = GetBrickSize();

            float brickLength = brickSize.x;
            float brickHeight = brickSize.y;

            float horizontalStep = brickLength + horizontalGap;
            float verticalStep = brickHeight + verticalGap;

            int rows = Mathf.FloorToInt(wall.Height / verticalStep);

            for (int y = 0; y < rows; y++)
            {
                float rowHeight = (y + 0.5f) * verticalStep;

                float rowStartOffset = brickLength * 0.5f;

                if (brickPattern == BrickPatternType.MatrixWithOffset && y % 2 == 1)
                    rowStartOffset += horizontalStep * 0.5f;

                float cursorDistance = rowStartOffset;

                while (cursorDistance <= totalLength)
                {
                    if (brickCount >= maxBricks)
                        return brickCount;

                    float sampledDistance = cursorDistance;

                    if (brickPattern == BrickPatternType.RandomHorizontal)
                    {
                        float randomOffset = Mathf.Lerp(
                            -randomHorizontalAmount,
                            randomHorizontalAmount,
                            (float)random.NextDouble()
                        );

                        sampledDistance += randomOffset;
                    }

                    sampledDistance = Mathf.Clamp(sampledDistance, 0f, totalLength);

                    if (!SamplePath(
                        surfacePath,
                        distances,
                        sampledDistance,
                        out Vector3 surfacePosition,
                        out Vector3 tangent))
                    {
                        cursorDistance += horizontalStep;
                        continue;
                    }

                    Vector3 surfaceNormal = Vector3.Cross(Vector3.up, tangent).normalized * sideMultiplier;

                    if (surfaceNormal.sqrMagnitude < 0.001f)
                    {
                        cursorDistance += horizontalStep;
                        continue;
                    }

                    Vector3 position =
                        surfacePosition +
                        Vector3.up * rowHeight +
                        surfaceNormal * brickZOffset;

                    Quaternion rotation = Quaternion.LookRotation(surfaceNormal, Vector3.up);

                    if (brickPattern == BrickPatternType.HorizontalVerticalRows && y % 2 == 1)
                        rotation *= Quaternion.Euler(0f, 0f, 90f);

                    GameObject brick = SpawnPrefab(
                        brickPrefab,
                        position,
                        rotation,
                        parent
                    );

                    if (brick != null)
                    {
                        brick.name = $"Brick_{brickCount}";
                        brick.transform.localScale = brickPrefabScale;
                        brickCount++;
                    }

                    cursorDistance += horizontalStep;
                }
            }

            return brickCount;
        }

        private static List<Vector3> BuildSurfacePath(
            List<Vector3> centerPath,
            float surfaceOffset,
            float sideMultiplier)
        {
            List<Vector3> surfacePath = new();

            for (int i = 0; i < centerPath.Count; i++)
            {
                Vector3 tangent = GetPathTangent(centerPath, i);

                if (tangent.sqrMagnitude < 0.001f)
                    continue;

                Vector3 normal = Vector3.Cross(Vector3.up, tangent).normalized * sideMultiplier;

                Vector3 surfacePoint = centerPath[i] + normal * surfaceOffset;

                surfacePath.Add(surfacePoint);
            }

            return surfacePath;
        }

        private static Vector3 GetPathTangent(List<Vector3> points, int index)
        {
            if (points == null || points.Count < 2)
                return Vector3.forward;

            if (index == 0)
                return (points[1] - points[0]).normalized;

            if (index == points.Count - 1)
                return (points[index] - points[index - 1]).normalized;

            Vector3 previous = (points[index] - points[index - 1]).normalized;
            Vector3 next = (points[index + 1] - points[index]).normalized;

            Vector3 tangent = (previous + next).normalized;

            if (tangent.sqrMagnitude < 0.001f)
                tangent = next;

            return tangent;
        }

        private Vector2 GetBrickSize()
        {
            if (!usePrefabBoundsForSpacing || brickPrefab == null)
            {
                return new Vector2(
                    manualBrickLength * Mathf.Abs(brickPrefabScale.x),
                    manualBrickHeight * Mathf.Abs(brickPrefabScale.y)
                );
            }

            Bounds localBounds = CalculatePrefabLocalBounds(brickPrefab);

            float length = localBounds.size.x * Mathf.Abs(brickPrefabScale.x);
            float height = localBounds.size.y * Mathf.Abs(brickPrefabScale.y);

            if (length <= 0.001f)
                length = manualBrickLength * Mathf.Abs(brickPrefabScale.x);

            if (height <= 0.001f)
                height = manualBrickHeight * Mathf.Abs(brickPrefabScale.y);

            return new Vector2(length, height);
        }

        private static Bounds CalculatePrefabLocalBounds(GameObject prefab)
        {
            MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();

            if (meshFilters == null || meshFilters.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            bool hasBounds = false;
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);

            Matrix4x4 rootWorldToLocal = prefab.transform.worldToLocalMatrix;

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Matrix4x4 meshToRoot = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;

                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;

                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, max.y, max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 transformedCorner = meshToRoot.MultiplyPoint3x4(corner);

                    if (!hasBounds)
                    {
                        combinedBounds = new Bounds(transformedCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(transformedCorner);
                    }
                }
            }

            return combinedBounds;
        }

        private static bool SamplePath(
            List<Vector3> points,
            List<float> distances,
            float targetDistance,
            out Vector3 position,
            out Vector3 tangent)
        {
            position = Vector3.zero;
            tangent = Vector3.forward;

            if (points == null || points.Count < 2)
                return false;

            targetDistance = Mathf.Clamp(targetDistance, 0f, distances[distances.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                float startDistance = distances[i];
                float endDistance = distances[i + 1];

                if (targetDistance > endDistance)
                    continue;

                float segmentLength = endDistance - startDistance;

                if (segmentLength <= 0.001f)
                    continue;

                float t = (targetDistance - startDistance) / segmentLength;

                position = Vector3.Lerp(points[i], points[i + 1], t);
                tangent = (points[i + 1] - points[i]).normalized;

                return true;
            }

            int last = points.Count - 1;

            position = points[last];
            tangent = (points[last] - points[last - 1]).normalized;

            return true;
        }

        private static List<float> CalculateDistances(List<Vector3> points)
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
    }
}