using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    public static class WallMeshGenerator
    {
        private const float Epsilon = 0.001f;

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

            textureScale = Mathf.Max(0.0001f, textureScale);

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

            List<Vector3> stationPoints = new List<Vector3>();
            List<Vector3> stationTangents = new List<Vector3>();

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

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float halfThickness = thickness * 0.5f;

            for (int i = 0; i < stationPoints.Count - 1; i++)
            {
                float d0 = stationDistances[i];
                float d1 = stationDistances[i + 1];

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

                AddWallFacesForStationSegment(
                    left0,
                    right0,
                    left1,
                    right1,
                    d0,
                    d1,
                    height,
                    openings,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );

                if (!ShouldSkipTopFace(d0, d1, height, openings))
                {
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
            }

            AddOpeningBridges(
                sourcePoints,
                pathDistances,
                stationDistances,
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

        private static List<float> BuildStationDistances(
            List<float> pathDistances,
            IReadOnlyList<WallOpeningData> openings)
        {
            List<float> stations = new List<float>(pathDistances);

            if (pathDistances == null || pathDistances.Count == 0)
                return stations;

            float totalLength = pathDistances[pathDistances.Count - 1];

            if (openings != null)
            {
                foreach (WallOpeningData opening in openings)
                {
                    float start = Mathf.Clamp(opening.StartDistance, 0f, totalLength);
                    float end = Mathf.Clamp(opening.EndDistance, 0f, totalLength);

                    AddDistanceIfValid(stations, start);
                    AddDistanceIfValid(stations, end);

                    if (opening.RoundTopCorners && GetOpeningSafeRadius(opening) > Epsilon)
                    {
                        AddRoundedOpeningStations(stations, opening, totalLength);
                    }
                }
            }

            SortAndCleanDistances(stations);

            return stations;
        }

        private static void AddRoundedOpeningStations(
            List<float> stations,
            WallOpeningData opening,
            float totalLength)
        {
            float radius = GetOpeningSafeRadius(opening);

            if (radius <= Epsilon)
                return;

            int segments = Mathf.Clamp(opening.TopCornerSegments, 1, 32);

            float start = Mathf.Clamp(opening.StartDistance, 0f, totalLength);
            float end = Mathf.Clamp(opening.EndDistance, 0f, totalLength);

            float leftArcEnd = Mathf.Clamp(start + radius, 0f, totalLength);
            float rightArcStart = Mathf.Clamp(end - radius, 0f, totalLength);

            AddDistanceIfValid(stations, leftArcEnd);
            AddDistanceIfValid(stations, rightArcStart);

            for (int i = 1; i < segments; i++)
            {
                float t = i / (float)segments;

                AddDistanceIfValid(stations, Mathf.Lerp(start, leftArcEnd, t));
                AddDistanceIfValid(stations, Mathf.Lerp(rightArcStart, end, t));
            }
        }

        private static void AddWallFacesForStationSegment(
            Vector3 left0,
            Vector3 right0,
            Vector3 left1,
            Vector3 right1,
            float d0,
            float d1,
            float wallHeight,
            IReadOnlyList<WallOpeningData> openings,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (!TryGetOpeningForSegment(d0, d1, openings, out WallOpeningData opening))
            {
                AddVariableVerticalQuad(
                    left0,
                    left1,
                    0f,
                    0f,
                    wallHeight,
                    wallHeight,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );

                AddVariableVerticalQuadFlipped(
                    right0,
                    right1,
                    0f,
                    0f,
                    wallHeight,
                    wallHeight,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );

                return;
            }

            float cutBottom = Mathf.Clamp(opening.BottomHeight, 0f, wallHeight);
            float cutTop0 = GetOpeningCutTopAtDistance(opening, d0, wallHeight);
            float cutTop1 = GetOpeningCutTopAtDistance(opening, d1, wallHeight);

            if (cutBottom > Epsilon)
            {
                AddVariableVerticalQuad(
                    left0,
                    left1,
                    0f,
                    0f,
                    cutBottom,
                    cutBottom,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );

                AddVariableVerticalQuadFlipped(
                    right0,
                    right1,
                    0f,
                    0f,
                    cutBottom,
                    cutBottom,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );
            }

            if (cutTop0 < wallHeight - Epsilon || cutTop1 < wallHeight - Epsilon)
            {
                AddVariableVerticalQuad(
                    left0,
                    left1,
                    cutTop0,
                    cutTop1,
                    wallHeight,
                    wallHeight,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );

                AddVariableVerticalQuadFlipped(
                    right0,
                    right1,
                    cutTop0,
                    cutTop1,
                    wallHeight,
                    wallHeight,
                    d0,
                    d1,
                    textureScale,
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        private static void AddOpeningBridges(
            List<Vector3> pathPoints,
            List<float> pathDistances,
            List<float> stationDistances,
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
                float start = Mathf.Clamp(opening.StartDistance, 0f, totalLength);
                float end = Mathf.Clamp(opening.EndDistance, 0f, totalLength);

                float bottom = Mathf.Clamp(opening.BottomHeight, 0f, wallHeight);
                float top = Mathf.Clamp(opening.TopHeight, 0f, wallHeight);

                if (end <= start || top <= bottom)
                    continue;

                float radius = GetOpeningSafeRadius(opening);
                float sideTop = top - radius;

                AddVerticalOpeningBridge(
                    pathPoints,
                    pathDistances,
                    start,
                    bottom,
                    sideTop,
                    halfThickness,
                    textureScale,
                    true,
                    vertices,
                    triangles,
                    uvs
                );

                AddVerticalOpeningBridge(
                    pathPoints,
                    pathDistances,
                    end,
                    bottom,
                    sideTop,
                    halfThickness,
                    textureScale,
                    false,
                    vertices,
                    triangles,
                    uvs
                );

                if (bottom > Epsilon)
                {
                    AddSegmentedOpeningBridgeAlongPath(
                        pathPoints,
                        pathDistances,
                        stationDistances,
                        opening,
                        start,
                        end,
                        bottom,
                        true,
                        halfThickness,
                        wallHeight,
                        textureScale,
                        vertices,
                        triangles,
                        uvs
                    );
                }

                if (top < wallHeight - Epsilon)
                {
                    AddSegmentedOpeningBridgeAlongPath(
                        pathPoints,
                        pathDistances,
                        stationDistances,
                        opening,
                        start,
                        end,
                        top,
                        false,
                        halfThickness,
                        wallHeight,
                        textureScale,
                        vertices,
                        triangles,
                        uvs
                    );
                }
            }
        }

        private static void AddVerticalOpeningBridge(
            List<Vector3> pathPoints,
            List<float> pathDistances,
            float distance,
            float bottom,
            float top,
            float halfThickness,
            float textureScale,
            bool isStartSide,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (top <= bottom + Epsilon)
                return;

            SamplePath(
                pathPoints,
                pathDistances,
                distance,
                out _,
                out Vector3 tangent
            );

            Vector3 desiredNormal = isStartSide ? tangent : -tangent;

            Vector2 pointA = new Vector2(distance, bottom);
            Vector2 pointB = new Vector2(distance, top);

            AddOpeningBridgeQuad(
                pathPoints,
                pathDistances,
                pointA,
                pointB,
                halfThickness,
                textureScale,
                desiredNormal,
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddSegmentedOpeningBridgeAlongPath(
            List<Vector3> pathPoints,
            List<float> pathDistances,
            List<float> stationDistances,
            WallOpeningData opening,
            float start,
            float end,
            float fallbackHeight,
            bool isBottomBridge,
            float halfThickness,
            float wallHeight,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            List<float> bridgeStations = GetDistancesBetween(
                stationDistances,
                start,
                end
            );

            if (bridgeStations.Count < 2)
                return;

            for (int i = 0; i < bridgeStations.Count - 1; i++)
            {
                float d0 = bridgeStations[i];
                float d1 = bridgeStations[i + 1];

                float h0 = isBottomBridge
                    ? fallbackHeight
                    : GetOpeningCutTopAtDistance(opening, d0, wallHeight);

                float h1 = isBottomBridge
                    ? fallbackHeight
                    : GetOpeningCutTopAtDistance(opening, d1, wallHeight);

                Vector2 pointA = new Vector2(d0, h0);
                Vector2 pointB = new Vector2(d1, h1);

                Vector3 desiredNormal = isBottomBridge ? Vector3.up : Vector3.down;

                AddOpeningBridgeQuad(
                    pathPoints,
                    pathDistances,
                    pointA,
                    pointB,
                    halfThickness,
                    textureScale,
                    desiredNormal,
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        private static void AddOpeningBridgeQuad(
            List<Vector3> pathPoints,
            List<float> pathDistances,
            Vector2 pointA,
            Vector2 pointB,
            float halfThickness,
            float textureScale,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            float distanceA = pointA.x;
            float heightA = pointA.y;

            float distanceB = pointB.x;
            float heightB = pointB.y;

            if (Mathf.Abs(distanceA - distanceB) < Epsilon &&
                Mathf.Abs(heightA - heightB) < Epsilon)
            {
                return;
            }

            SamplePath(
                pathPoints,
                pathDistances,
                distanceA,
                out Vector3 centerA,
                out Vector3 tangentA
            );

            SamplePath(
                pathPoints,
                pathDistances,
                distanceB,
                out Vector3 centerB,
                out Vector3 tangentB
            );

            Vector3 normalA = Vector3.Cross(Vector3.up, tangentA).normalized;
            Vector3 normalB = Vector3.Cross(Vector3.up, tangentB).normalized;

            Vector3 leftA = centerA - normalA * halfThickness + Vector3.up * heightA;
            Vector3 rightA = centerA + normalA * halfThickness + Vector3.up * heightA;

            Vector3 leftB = centerB - normalB * halfThickness + Vector3.up * heightB;
            Vector3 rightB = centerB + normalB * halfThickness + Vector3.up * heightB;

            float bridgeLength = Vector2.Distance(pointA, pointB) / textureScale;
            float bridgeDepth = (halfThickness * 2f) / textureScale;

            AddQuadFacingNormal(
                rightA,
                rightB,
                leftB,
                leftA,
                new Vector2(0f, 0f),
                new Vector2(bridgeLength, 0f),
                new Vector2(bridgeLength, bridgeDepth),
                new Vector2(0f, bridgeDepth),
                desiredNormal,
                vertices,
                triangles,
                uvs
            );
        }

        private static List<float> GetDistancesBetween(
            List<float> allDistances,
            float start,
            float end)
        {
            List<float> result = new List<float>();

            AddDistanceIfValid(result, start);

            for (int i = 0; i < allDistances.Count; i++)
            {
                float distance = allDistances[i];

                if (distance > start + Epsilon && distance < end - Epsilon)
                    AddDistanceIfValid(result, distance);
            }

            AddDistanceIfValid(result, end);

            SortAndCleanDistances(result);

            return result;
        }

        private static bool TryGetOpeningForSegment(
            float d0,
            float d1,
            IReadOnlyList<WallOpeningData> openings,
            out WallOpeningData result)
        {
            result = default;

            if (openings == null || openings.Count == 0)
                return false;

            float mid = (d0 + d1) * 0.5f;

            foreach (WallOpeningData opening in openings)
            {
                bool overlaps =
                    d1 > opening.StartDistance &&
                    d0 < opening.EndDistance;

                bool midInside =
                    mid >= opening.StartDistance &&
                    mid <= opening.EndDistance;

                if (overlaps || midInside)
                {
                    result = opening;
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldSkipTopFace(
            float d0,
            float d1,
            float wallHeight,
            IReadOnlyList<WallOpeningData> openings)
        {
            if (!TryGetOpeningForSegment(d0, d1, openings, out WallOpeningData opening))
                return false;

            float mid = (d0 + d1) * 0.5f;
            float cutTop = GetOpeningCutTopAtDistance(opening, mid, wallHeight);

            return cutTop >= wallHeight - Epsilon;
        }

        private static float GetOpeningSafeRadius(WallOpeningData opening)
        {
            if (!opening.RoundTopCorners)
                return 0f;

            return Mathf.Min(
                opening.TopCornerRadius,
                opening.Width * 0.5f,
                opening.Height
            );
        }

        private static float GetOpeningCutTopAtDistance(
            WallOpeningData opening,
            float distance,
            float wallHeight)
        {
            float top = Mathf.Clamp(opening.TopHeight, 0f, wallHeight);
            float radius = GetOpeningSafeRadius(opening);

            if (radius <= Epsilon)
                return top;

            float start = opening.StartDistance;
            float end = opening.EndDistance;

            float leftCenterX = start + radius;
            float rightCenterX = end - radius;
            float arcCenterY = top - radius;

            if (distance < leftCenterX)
            {
                float dx = distance - leftCenterX;
                float y = arcCenterY + Mathf.Sqrt(Mathf.Max(0f, radius * radius - dx * dx));
                return Mathf.Clamp(y, opening.BottomHeight, top);
            }

            if (distance > rightCenterX)
            {
                float dx = distance - rightCenterX;
                float y = arcCenterY + Mathf.Sqrt(Mathf.Max(0f, radius * radius - dx * dx));
                return Mathf.Clamp(y, opening.BottomHeight, top);
            }

            return top;
        }

        private static void AddVariableVerticalQuad(
            Vector3 bottomA,
            Vector3 bottomB,
            float yBottomA,
            float yBottomB,
            float yTopA,
            float yTopB,
            float d0,
            float d1,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (yTopA <= yBottomA && yTopB <= yBottomB)
                return;

            Vector3 v0 = bottomA + Vector3.up * yBottomA;
            Vector3 v1 = bottomB + Vector3.up * yBottomB;
            Vector3 v2 = bottomB + Vector3.up * yTopB;
            Vector3 v3 = bottomA + Vector3.up * yTopA;

            AddQuad(
                v0,
                v1,
                v2,
                v3,
                new Vector2(d0 / textureScale, yBottomA / textureScale),
                new Vector2(d1 / textureScale, yBottomB / textureScale),
                new Vector2(d1 / textureScale, yTopB / textureScale),
                new Vector2(d0 / textureScale, yTopA / textureScale),
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddVariableVerticalQuadFlipped(
            Vector3 bottomA,
            Vector3 bottomB,
            float yBottomA,
            float yBottomB,
            float yTopA,
            float yTopB,
            float d0,
            float d1,
            float textureScale,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (yTopA <= yBottomA && yTopB <= yBottomB)
                return;

            Vector3 v0 = bottomB + Vector3.up * yBottomB;
            Vector3 v1 = bottomA + Vector3.up * yBottomA;
            Vector3 v2 = bottomA + Vector3.up * yTopA;
            Vector3 v3 = bottomB + Vector3.up * yTopB;

            AddQuad(
                v0,
                v1,
                v2,
                v3,
                new Vector2(d1 / textureScale, yBottomB / textureScale),
                new Vector2(d0 / textureScale, yBottomA / textureScale),
                new Vector2(d0 / textureScale, yTopA / textureScale),
                new Vector2(d1 / textureScale, yTopB / textureScale),
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
            List<Vector3> points = new List<Vector3>();

            if (stroke == null || owner == null)
                return points;

            for (int i = 0; i < stroke.Count; i++)
            {
                Vector3 localPoint = owner.InverseTransformPoint(stroke.GetPoint(i));

                if (points.Count > 0)
                {
                    if (Vector3.Distance(points[points.Count - 1], localPoint) <= Epsilon)
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

            List<Vector3> roundedPoints = new List<Vector3>();

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
                if (Vector3.Distance(points[points.Count - 1], point) <= Epsilon)
                    return;
            }

            points.Add(point);
        }

        private static List<float> CalculateDistances(List<Vector3> points)
        {
            List<float> distances = new List<float>();
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

                if (segmentLength <= Epsilon)
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

        private static void AddDistanceIfValid(List<float> distances, float value)
        {
            for (int i = 0; i < distances.Count; i++)
            {
                if (Mathf.Abs(distances[i] - value) < Epsilon)
                    return;
            }

            distances.Add(value);
        }

        private static void SortAndCleanDistances(List<float> distances)
        {
            distances.Sort();

            for (int i = distances.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(distances[i] - distances[i - 1]) < Epsilon)
                    distances.RemoveAt(i);
            }
        }

        private static void AddQuadFacingNormal(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (desiredNormal.sqrMagnitude <= Epsilon)
            {
                AddQuad(
                    v0,
                    v1,
                    v2,
                    v3,
                    uv0,
                    uv1,
                    uv2,
                    uv3,
                    vertices,
                    triangles,
                    uvs
                );

                return;
            }

            Vector3 currentNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            if (Vector3.Dot(currentNormal, desiredNormal.normalized) < 0f)
            {
                AddQuad(
                    v3,
                    v2,
                    v1,
                    v0,
                    uv3,
                    uv2,
                    uv1,
                    uv0,
                    vertices,
                    triangles,
                    uvs
                );
            }
            else
            {
                AddQuad(
                    v0,
                    v1,
                    v2,
                    v3,
                    uv0,
                    uv1,
                    uv2,
                    uv3,
                    vertices,
                    triangles,
                    uvs
                );
            }
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