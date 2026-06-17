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
            int cornerSegments = 5,
            IReadOnlyList<WallOpeningData> openings = null)
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

            List<float> pathDistances = CalculateDistances(sourcePoints);
            List<float> stationDistances = BuildStationDistances(pathDistances, openings);

            List<Vector3> stationPoints = new();
            List<Vector3> stationTangents = new();

            for (int i = 0; i < stationDistances.Count; i++)
            {
                SamplePath(
                    sourcePoints,
                    pathDistances,
                    stationDistances[i],
                    out Vector3 position,
                    out Vector3 tangent
                );

                stationPoints.Add(position);
                stationTangents.Add(tangent);
            }

            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            float halfThickness = thickness * 0.5f;

            for (int i = 0; i < stationPoints.Count - 1; i++)
            {
                float d0 = stationDistances[i];
                float d1 = stationDistances[i + 1];
                float midDistance = (d0 + d1) * 0.5f;

                Vector3 p0 = stationPoints[i];
                Vector3 p1 = stationPoints[i + 1];

                Vector3 tangent0 = stationTangents[i];
                Vector3 tangent1 = stationTangents[i + 1];

                Vector3 normal0 = Vector3.Cross(Vector3.up, tangent0).normalized;
                Vector3 normal1 = Vector3.Cross(Vector3.up, tangent1).normalized;

                Vector3 left0 = p0 - normal0 * halfThickness;
                Vector3 right0 = p0 + normal0 * halfThickness;
                Vector3 left1 = p1 - normal1 * halfThickness;
                Vector3 right1 = p1 + normal1 * halfThickness;

                List<Vector2> solidBands = GetSolidVerticalBands(
                    midDistance,
                    height,
                    openings
                );

                foreach (Vector2 band in solidBands)
                {
                    float y0 = band.x;
                    float y1 = band.y;

                    if (y1 <= y0)
                        continue;

                    AddVerticalQuad(
                        left0,
                        left1,
                        y0,
                        y1,
                        d0,
                        d1,
                        textureScale,
                        vertices,
                        triangles,
                        uvs
                    );

                    AddVerticalQuadFlipped(
                        right0,
                        right1,
                        y0,
                        y1,
                        d0,
                        d1,
                        textureScale,
                        vertices,
                        triangles,
                        uvs
                    );
                }

                AddTopQuad(
                    left0,
                    right0,
                    left1,
                    right1,
                    height,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );
            }

            AddOpeningReveals(
                sourcePoints,
                pathDistances,
                openings,
                halfThickness,
                height,
                textureScale,
                vertices,
                triangles,
                uvs
            );

            AddEndCap(
                stationPoints[0],
                stationTangents[0],
                halfThickness,
                height,
                textureScale,
                vertices,
                triangles,
                uvs,
                true
            );

            int lastIndex = stationPoints.Count - 1;

            AddEndCap(
                stationPoints[lastIndex],
                stationTangents[lastIndex],
                halfThickness,
                height,
                textureScale,
                vertices,
                triangles,
                uvs,
                false
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

        private static void AddOpeningReveals(
            List<Vector3> pathPoints,
            List<float> pathDistances,
            IReadOnlyList<WallOpeningData> openings,
            float halfThickness,
            float wallHeight,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (openings == null || openings.Count == 0)
                return;

            float totalLength = pathDistances[pathDistances.Count - 1];

            foreach (WallOpeningData opening in openings)
            {
                float startDistance = Mathf.Clamp(opening.StartDistance, 0f, totalLength);
                float endDistance = Mathf.Clamp(opening.EndDistance, 0f, totalLength);

                if (endDistance <= startDistance)
                    continue;

                float bottom = Mathf.Clamp(opening.BottomHeight, 0f, wallHeight);
                float top = Mathf.Clamp(opening.TopHeight, 0f, wallHeight);

                if (top <= bottom)
                    continue;

                SamplePath(
                    pathPoints,
                    pathDistances,
                    startDistance,
                    out Vector3 startCenter,
                    out Vector3 startTangent
                );

                SamplePath(
                    pathPoints,
                    pathDistances,
                    endDistance,
                    out Vector3 endCenter,
                    out Vector3 endTangent
                );

                Vector3 startNormal = Vector3.Cross(Vector3.up, startTangent).normalized;
                Vector3 endNormal = Vector3.Cross(Vector3.up, endTangent).normalized;

                AddSideReveal(
                    startCenter,
                    startNormal,
                    halfThickness,
                    bottom,
                    top,
                    textureScale,
                    true,
                    vertices,
                    triangles,
                    uvs
                );

                AddSideReveal(
                    endCenter,
                    endNormal,
                    halfThickness,
                    bottom,
                    top,
                    textureScale,
                    false,
                    vertices,
                    triangles,
                    uvs
                );

                if (top < wallHeight - 0.001f)
                {
                    AddHorizontalReveal(
                        startCenter,
                        startNormal,
                        endCenter,
                        endNormal,
                        halfThickness,
                        top,
                        textureScale,
                        false,
                        vertices,
                        triangles,
                        uvs
                    );
                }

                if (bottom > 0.001f)
                {
                    AddHorizontalReveal(
                        startCenter,
                        startNormal,
                        endCenter,
                        endNormal,
                        halfThickness,
                        bottom,
                        textureScale,
                        true,
                        vertices,
                        triangles,
                        uvs
                    );
                }
            }
        }

        private static void AddSideReveal(
            Vector3 center,
            Vector3 normal,
            float halfThickness,
            float bottom,
            float top,
            float textureScale,
            bool isStartSide,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 leftBottom = center - normal * halfThickness + Vector3.up * bottom;
            Vector3 rightBottom = center + normal * halfThickness + Vector3.up * bottom;
            Vector3 leftTop = center - normal * halfThickness + Vector3.up * top;
            Vector3 rightTop = center + normal * halfThickness + Vector3.up * top;

            float thicknessUv = (halfThickness * 2f) / textureScale;
            float heightUv = (top - bottom) / textureScale;

            if (isStartSide)
            {
                AddQuad(
                    leftBottom,
                    rightBottom,
                    rightTop,
                    leftTop,
                    new Vector2(0f, 0f),
                    new Vector2(thicknessUv, 0f),
                    new Vector2(thicknessUv, heightUv),
                    new Vector2(0f, heightUv),
                    vertices,
                    triangles,
                    uvs
                );
            }
            else
            {
                AddQuad(
                    rightBottom,
                    leftBottom,
                    leftTop,
                    rightTop,
                    new Vector2(0f, 0f),
                    new Vector2(thicknessUv, 0f),
                    new Vector2(thicknessUv, heightUv),
                    new Vector2(0f, heightUv),
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        private static void AddHorizontalReveal(
            Vector3 startCenter,
            Vector3 startNormal,
            Vector3 endCenter,
            Vector3 endNormal,
            float halfThickness,
            float y,
            float textureScale,
            bool isBottomReveal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 startLeft = startCenter - startNormal * halfThickness + Vector3.up * y;
            Vector3 startRight = startCenter + startNormal * halfThickness + Vector3.up * y;
            Vector3 endLeft = endCenter - endNormal * halfThickness + Vector3.up * y;
            Vector3 endRight = endCenter + endNormal * halfThickness + Vector3.up * y;

            float lengthUv = Vector3.Distance(startCenter, endCenter) / textureScale;
            float thicknessUv = (halfThickness * 2f) / textureScale;

            if (isBottomReveal)
            {
                AddQuad(
                    startLeft,
                    endLeft,
                    endRight,
                    startRight,
                    new Vector2(0f, 0f),
                    new Vector2(lengthUv, 0f),
                    new Vector2(lengthUv, thicknessUv),
                    new Vector2(0f, thicknessUv),
                    vertices,
                    triangles,
                    uvs
                );
            }
            else
            {
                AddQuad(
                    startRight,
                    endRight,
                    endLeft,
                    startLeft,
                    new Vector2(0f, 0f),
                    new Vector2(lengthUv, 0f),
                    new Vector2(lengthUv, thicknessUv),
                    new Vector2(0f, thicknessUv),
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        private static List<float> BuildStationDistances(
            List<float> pathDistances,
            IReadOnlyList<WallOpeningData> openings)
        {
            List<float> stations = new(pathDistances);

            if (pathDistances == null || pathDistances.Count == 0)
                return stations;

            float totalLength = pathDistances[pathDistances.Count - 1];

            if (openings != null)
            {
                foreach (WallOpeningData opening in openings)
                {
                    AddDistanceIfValid(stations, Mathf.Clamp(opening.StartDistance, 0f, totalLength));
                    AddDistanceIfValid(stations, Mathf.Clamp(opening.EndDistance, 0f, totalLength));
                }
            }

            stations.Sort();

            for (int i = stations.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(stations[i] - stations[i - 1]) < 0.001f)
                    stations.RemoveAt(i);
            }

            return stations;
        }

        private static void AddDistanceIfValid(List<float> distances, float value)
        {
            for (int i = 0; i < distances.Count; i++)
            {
                if (Mathf.Abs(distances[i] - value) < 0.001f)
                    return;
            }

            distances.Add(value);
        }

        private static List<Vector2> GetSolidVerticalBands(
            float distance,
            float wallHeight,
            IReadOnlyList<WallOpeningData> openings)
        {
            List<Vector2> bands = new();
            bands.Add(new Vector2(0f, wallHeight));

            if (openings == null)
                return bands;

            foreach (WallOpeningData opening in openings)
            {
                bool insideOpeningHorizontally =
                    distance >= opening.StartDistance &&
                    distance <= opening.EndDistance;

                if (!insideOpeningHorizontally)
                    continue;

                float cutBottom = Mathf.Clamp(opening.BottomHeight, 0f, wallHeight);
                float cutTop = Mathf.Clamp(opening.TopHeight, 0f, wallHeight);

                if (cutTop <= cutBottom)
                    continue;

                bands = SubtractBand(bands, cutBottom, cutTop);
            }

            return bands;
        }

        private static List<Vector2> SubtractBand(
            List<Vector2> bands,
            float cutBottom,
            float cutTop)
        {
            List<Vector2> result = new();

            foreach (Vector2 band in bands)
            {
                float bandBottom = band.x;
                float bandTop = band.y;

                if (cutTop <= bandBottom || cutBottom >= bandTop)
                {
                    result.Add(band);
                    continue;
                }

                if (cutBottom > bandBottom)
                    result.Add(new Vector2(bandBottom, cutBottom));

                if (cutTop < bandTop)
                    result.Add(new Vector2(cutTop, bandTop));
            }

            return result;
        }

        private static void AddVerticalQuad(
            Vector3 bottomA,
            Vector3 bottomB,
            float y0,
            float y1,
            float d0,
            float d1,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 v0 = bottomA + Vector3.up * y0;
            Vector3 v1 = bottomB + Vector3.up * y0;
            Vector3 v2 = bottomB + Vector3.up * y1;
            Vector3 v3 = bottomA + Vector3.up * y1;

            AddQuad(
                v0, v1, v2, v3,
                new Vector2(d0 / textureScale, y0 / textureScale),
                new Vector2(d1 / textureScale, y0 / textureScale),
                new Vector2(d1 / textureScale, y1 / textureScale),
                new Vector2(d0 / textureScale, y1 / textureScale),
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddVerticalQuadFlipped(
            Vector3 bottomA,
            Vector3 bottomB,
            float y0,
            float y1,
            float d0,
            float d1,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 v0 = bottomB + Vector3.up * y0;
            Vector3 v1 = bottomA + Vector3.up * y0;
            Vector3 v2 = bottomA + Vector3.up * y1;
            Vector3 v3 = bottomB + Vector3.up * y1;

            AddQuad(
                v0, v1, v2, v3,
                new Vector2(d1 / textureScale, y0 / textureScale),
                new Vector2(d0 / textureScale, y0 / textureScale),
                new Vector2(d0 / textureScale, y1 / textureScale),
                new Vector2(d1 / textureScale, y1 / textureScale),
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddTopQuad(
            Vector3 left0,
            Vector3 right0,
            Vector3 left1,
            Vector3 right1,
            float height,
            float d0,
            float d1,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            AddQuad(
                left0 + Vector3.up * height,
                left1 + Vector3.up * height,
                right1 + Vector3.up * height,
                right0 + Vector3.up * height,
                new Vector2(d0 / textureScale, 0f),
                new Vector2(d1 / textureScale, 0f),
                new Vector2(d1 / textureScale, 1f),
                new Vector2(d0 / textureScale, 1f),
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddEndCap(
            Vector3 center,
            Vector3 tangent,
            float halfThickness,
            float height,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            bool startCap)
        {
            Vector3 normal = Vector3.Cross(Vector3.up, tangent).normalized;

            Vector3 leftBottom = center - normal * halfThickness;
            Vector3 rightBottom = center + normal * halfThickness;
            Vector3 leftTop = leftBottom + Vector3.up * height;
            Vector3 rightTop = rightBottom + Vector3.up * height;

            if (startCap)
            {
                AddQuad(
                    rightBottom,
                    leftBottom,
                    leftTop,
                    rightTop,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, height / textureScale),
                    new Vector2(0f, height / textureScale),
                    vertices,
                    triangles,
                    uvs
                );
            }
            else
            {
                AddQuad(
                    leftBottom,
                    rightBottom,
                    rightTop,
                    leftTop,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, height / textureScale),
                    new Vector2(0f, height / textureScale),
                    vertices,
                    triangles,
                    uvs
                );
            }
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
                    if (Vector3.Distance(points[points.Count - 1], localPoint) <= 0.001f)
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

            AddPointIfValid(roundedPoints, points[points.Count - 1]);

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
                if (Vector3.Distance(points[points.Count - 1], point) <= 0.001f)
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

        private static void AddQuad(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            int startIndex = vertices.Count;

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            uvs.Add(uv0);
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);

            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);

            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }
    }
}