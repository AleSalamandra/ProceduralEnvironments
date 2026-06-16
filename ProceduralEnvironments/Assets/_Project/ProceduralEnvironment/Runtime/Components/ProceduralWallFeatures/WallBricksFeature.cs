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

        [Header("Brick Distribution")]
        [SerializeField] private float brickSpacingX = 0.5f;
        [SerializeField] private float brickSpacingY = 0.25f;
        [SerializeField] private float randomHorizontalAmount = 0.15f;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private int maxBricks = 1000;

        protected override void OnValidate()
        {
            brickSpacingX = Mathf.Max(0.01f, brickSpacingX);
            brickSpacingY = Mathf.Max(0.01f, brickSpacingY);
            maxBricks = Mathf.Max(0, maxBricks);

            base.OnValidate();
        }

        protected override void OnRebuild(ProceduralWall wall)
        {
            if (brickPrefab == null)
                return;

            List<Vector3> pathPoints = wall.GetGeneratedPathWorldPoints();

            if (pathPoints == null || pathPoints.Count < 2)
                return;

            List<float> distances = CalculateDistances(pathPoints);
            float totalLength = distances[^1];

            if (totalLength <= 0.001f)
                return;

            Transform root = GetOrCreateGeneratedRoot();
            System.Random random = new System.Random(randomSeed);

            int brickCount = 0;

            if (brickSide == WallSurfaceSide.Front || brickSide == WallSurfaceSide.Both)
            {
                brickCount = GenerateBricksOnPath(
                    wall,
                    pathPoints,
                    distances,
                    totalLength,
                    1f,
                    root,
                    random,
                    brickCount
                );
            }

            if (brickSide == WallSurfaceSide.Back || brickSide == WallSurfaceSide.Both)
            {
                brickCount = GenerateBricksOnPath(
                    wall,
                    pathPoints,
                    distances,
                    totalLength,
                    -1f,
                    root,
                    random,
                    brickCount
                );
            }
        }

        private int GenerateBricksOnPath(
            ProceduralWall wall,
            List<Vector3> pathPoints,
            List<float> distances,
            float totalLength,
            float sideMultiplier,
            Transform parent,
            System.Random random,
            int brickCount)
        {
            int rows = Mathf.FloorToInt(wall.Height / brickSpacingY);
            int columns = Mathf.FloorToInt(totalLength / brickSpacingX);

            for (int y = 0; y < rows; y++)
            {
                float rowHeight = (y + 0.5f) * brickSpacingY;
                float rowOffset = GetRowOffset(y);

                for (int x = 0; x < columns; x++)
                {
                    if (brickCount >= maxBricks)
                        return brickCount;

                    float distanceOnPath = (x + 0.5f) * brickSpacingX + rowOffset;

                    if (brickPattern == BrickPatternType.RandomHorizontal)
                    {
                        float randomOffset = Mathf.Lerp(
                            -randomHorizontalAmount,
                            randomHorizontalAmount,
                            (float)random.NextDouble()
                        );

                        distanceOnPath += randomOffset;
                    }

                    if (distanceOnPath > totalLength)
                        continue;

                    if (!SamplePath(
                        pathPoints,
                        distances,
                        distanceOnPath,
                        out Vector3 pathPosition,
                        out Vector3 tangent))
                    {
                        continue;
                    }

                    Vector3 surfaceNormal = Vector3.Cross(Vector3.up, tangent).normalized * sideMultiplier;

                    if (surfaceNormal.sqrMagnitude < 0.001f)
                        continue;

                    Vector3 position =
                        pathPosition +
                        Vector3.up * rowHeight +
                        surfaceNormal * ((wall.Thickness * 0.5f) + brickZOffset);

                    Quaternion rotation = Quaternion.LookRotation(surfaceNormal, Vector3.up);

                    if (brickPattern == BrickPatternType.HorizontalVerticalRows && y % 2 == 1)
                        rotation *= Quaternion.Euler(0f, 0f, 90f);

                    GameObject brick = SpawnPrefab(
                        brickPrefab,
                        position,
                        rotation,
                        parent
                    );

                    if (brick == null)
                        continue;

                    brick.name = $"Brick_{brickCount}";
                    brick.transform.localScale = brickPrefabScale;

                    brickCount++;
                }
            }

            return brickCount;
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

            targetDistance = Mathf.Clamp(targetDistance, 0f, distances[^1]);

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

            position = points[^1];
            tangent = (points[^1] - points[^2]).normalized;

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

        private float GetRowOffset(int rowIndex)
        {
            if (brickPattern == BrickPatternType.MatrixWithOffset && rowIndex % 2 == 1)
                return brickSpacingX * 0.5f;

            return 0f;
        }
    }
}