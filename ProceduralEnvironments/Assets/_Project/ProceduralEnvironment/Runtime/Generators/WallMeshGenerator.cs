using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    public static class WallMeshGenerator
    {
        public static Mesh Generate(
            ProceduralStroke stroke,
            Transform owner,
            float height,
            float thickness,
            float textureScale,
            bool roundCorners = false,
            float cornerRadius = 0.5f,
            int cornerSegments = 5)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Generated_Wall_Mesh";

            List<Vector3> sourcePoints = BuildLocalPath(
                stroke,
                owner,
                roundCorners,
                cornerRadius,
                cornerSegments
            );

            if (sourcePoints == null || sourcePoints.Count < 2)
                return mesh;

            List<float> distances = CalculateDistances(sourcePoints);

            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            float halfThickness = thickness * 0.5f;

            for (int i = 0; i < sourcePoints.Count; i++)
            {
                Vector3 offset = CalculateWallOffset(sourcePoints, i, halfThickness);

                Vector3 leftBottom = sourcePoints[i] - offset;
                Vector3 rightBottom = sourcePoints[i] + offset;

                Vector3 leftTop = leftBottom + Vector3.up * height;
                Vector3 rightTop = rightBottom + Vector3.up * height;

                vertices.Add(leftBottom);
                vertices.Add(rightBottom);
                vertices.Add(leftTop);
                vertices.Add(rightTop);

                float u = distances[i] / textureScale;
                float vHeight = height / textureScale;
                float vThickness = thickness / textureScale;

                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, vThickness));
                uvs.Add(new Vector2(u, vHeight));
                uvs.Add(new Vector2(u, vHeight + vThickness));
            }

            for (int i = 0; i < sourcePoints.Count - 1; i++)
            {
                int current = i * 4;
                int next = (i + 1) * 4;

                int cLB = current + 0;
                int cRB = current + 1;
                int cLT = current + 2;
                int cRT = current + 3;

                int nLB = next + 0;
                int nRB = next + 1;
                int nLT = next + 2;
                int nRT = next + 3;

                // Left side
                AddQuad(triangles, cLB, nLB, nLT, cLT);

                // Right side
                AddQuad(triangles, nRB, cRB, cRT, nRT);

                // Top side
                AddQuad(triangles, cLT, nLT, nRT, cRT);
            }

            // Start cap
            AddQuad(triangles, 1, 0, 2, 3);

            int last = (sourcePoints.Count - 1) * 4;

            // End cap
            AddQuad(
                triangles,
                last + 0,
                last + 1,
                last + 3,
                last + 2
            );

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static List<Vector3> BuildLocalPath(
            ProceduralStroke stroke,
            Transform owner,
            bool roundCorners,
            float cornerRadius,
            int cornerSegments)
        {
            List<Vector3> sourcePoints = GetLocalPoints(stroke, owner);

            if (sourcePoints.Count < 2)
                return sourcePoints;

            if (roundCorners)
            {
                sourcePoints = BuildRoundedPath(
                    sourcePoints,
                    cornerRadius,
                    cornerSegments
                );
            }

            return sourcePoints;
        }

        private static List<Vector3> GetLocalPoints(
            ProceduralStroke stroke,
            Transform owner)
        {
            List<Vector3> points = new();

            if (stroke == null || owner == null)
                return points;

            for (int i = 0; i < stroke.Count; i++)
            {
                Vector3 localPoint = owner.InverseTransformPoint(stroke.GetPoint(i));

                if (points.Count > 0)
                {
                    if (Vector3.Distance(points[^1], localPoint) <= 0.001f)
                        continue;
                }

                points.Add(localPoint);
            }

            return points;
        }

        private static List<Vector3> BuildRoundedPath(
            List<Vector3> points,
            float radius,
            int segments)
        {
            if (points.Count < 3 || radius <= 0f)
                return points;

            segments = Mathf.Max(1, segments);

            List<Vector3> roundedPoints = new();

            AddPointIfValid(roundedPoints, points[0]);

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 previous = points[i - 1];
                Vector3 current = points[i];
                Vector3 next = points[i + 1];

                Vector3 incoming = (current - previous).normalized;
                Vector3 outgoing = (next - current).normalized;

                float dot = Vector3.Dot(incoming, outgoing);

                // Same direction or direct U-turn: keep hard point.
                if (dot > 0.999f || dot < -0.999f)
                {
                    AddPointIfValid(roundedPoints, current);
                    continue;
                }

                float previousLength = Vector3.Distance(previous, current);
                float nextLength = Vector3.Distance(current, next);

                float safeRadius = Mathf.Min(
                    radius,
                    previousLength * 0.49f,
                    nextLength * 0.49f
                );

                Vector3 curveStart = current - incoming * safeRadius;
                Vector3 curveEnd = current + outgoing * safeRadius;

                AddPointIfValid(roundedPoints, curveStart);

                for (int j = 1; j < segments; j++)
                {
                    float t = j / (float)segments;

                    Vector3 curvePoint = QuadraticBezier(
                        curveStart,
                        current,
                        curveEnd,
                        t
                    );

                    AddPointIfValid(roundedPoints, curvePoint);
                }

                AddPointIfValid(roundedPoints, curveEnd);
            }

            AddPointIfValid(roundedPoints, points[^1]);

            return roundedPoints;
        }

        private static Vector3 QuadraticBezier(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            float t)
        {
            float inverseT = 1f - t;

            return
                inverseT * inverseT * a +
                2f * inverseT * t * b +
                t * t * c;
        }

        private static void AddPointIfValid(
            List<Vector3> points,
            Vector3 point)
        {
            if (points.Count > 0)
            {
                if (Vector3.Distance(points[^1], point) <= 0.001f)
                    return;
            }

            points.Add(point);
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

        private static Vector3 CalculateWallOffset(
            List<Vector3> points,
            int index,
            float halfThickness)
        {
            Vector3 direction;

            if (index == 0)
            {
                direction = (points[1] - points[0]).normalized;
                return Vector3.Cross(Vector3.up, direction).normalized * halfThickness;
            }

            if (index == points.Count - 1)
            {
                direction = (points[index] - points[index - 1]).normalized;
                return Vector3.Cross(Vector3.up, direction).normalized * halfThickness;
            }

            Vector3 previousDirection = (points[index] - points[index - 1]).normalized;
            Vector3 nextDirection = (points[index + 1] - points[index]).normalized;

            Vector3 previousNormal = Vector3.Cross(Vector3.up, previousDirection).normalized;
            Vector3 nextNormal = Vector3.Cross(Vector3.up, nextDirection).normalized;

            Vector3 miter = (previousNormal + nextNormal).normalized;

            if (miter.sqrMagnitude < 0.001f)
                return nextNormal * halfThickness;

            float denominator = Vector3.Dot(miter, nextNormal);

            if (Mathf.Abs(denominator) < 0.001f)
                return nextNormal * halfThickness;

            float miterLength = halfThickness / denominator;
            miterLength = Mathf.Clamp(miterLength, halfThickness, halfThickness * 2.5f);

            return miter * miterLength;
        }

        private static void AddQuad(
            List<int> triangles,
            int a,
            int b,
            int c,
            int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
    }
}