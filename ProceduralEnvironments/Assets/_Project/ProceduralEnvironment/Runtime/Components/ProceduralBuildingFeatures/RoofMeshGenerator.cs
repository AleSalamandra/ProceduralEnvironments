using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    public static class RoofMeshGenerator
    {
        private const float Epsilon = 0.001f;

        public static Mesh Generate(
            BuildingFootprintType footprintType,
            float width,
            float depth,
            float wallHeight,
            float roofHeight,
            float overhang,
            float roofThickness,
            int circleSegments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Generated_Roof_Mesh";

            width = Mathf.Max(0.01f, width);
            depth = Mathf.Max(0.01f, depth);
            wallHeight = Mathf.Max(0f, wallHeight);
            roofHeight = Mathf.Max(0.01f, roofHeight);
            overhang = Mathf.Max(0f, overhang);
            roofThickness = Mathf.Max(0.01f, roofThickness);
            circleSegments = Mathf.Clamp(circleSegments, 8, 128);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            switch (footprintType)
            {
                case BuildingFootprintType.Circle:
                    BuildConicalRoof(
                        width,
                        depth,
                        wallHeight,
                        roofHeight,
                        overhang,
                        roofThickness,
                        circleSegments,
                        vertices,
                        triangles,
                        uvs
                    );
                    break;

                case BuildingFootprintType.Hexagon:
                    BuildHexagonalRoof(
                        width,
                        depth,
                        wallHeight,
                        roofHeight,
                        overhang,
                        roofThickness,
                        vertices,
                        triangles,
                        uvs
                    );
                    break;

                case BuildingFootprintType.Rectangle:
                default:
                    BuildHipRoof(
                        width,
                        depth,
                        wallHeight,
                        roofHeight,
                        overhang,
                        roofThickness,
                        vertices,
                        triangles,
                        uvs
                    );
                    break;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void BuildHipRoof(
            float width,
            float depth,
            float wallHeight,
            float roofHeight,
            float overhang,
            float roofThickness,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            float halfWidth = width * 0.5f + overhang;
            float halfDepth = depth * 0.5f + overhang;

            float baseY = wallHeight;
            float ridgeY = wallHeight + roofHeight;

            Vector3 roofCenter = new Vector3(0f, baseY, 0f);

            Vector3 northWest = new Vector3(-halfWidth, baseY, halfDepth);
            Vector3 northEast = new Vector3(halfWidth, baseY, halfDepth);
            Vector3 southEast = new Vector3(halfWidth, baseY, -halfDepth);
            Vector3 southWest = new Vector3(-halfWidth, baseY, -halfDepth);

            List<Vector3> outerRing = new List<Vector3>
            {
                northWest,
                northEast,
                southEast,
                southWest
            };

            if (halfWidth >= halfDepth)
            {
                float ridgeHalfLength = Mathf.Max(0f, halfWidth - halfDepth);

                if (ridgeHalfLength <= Epsilon)
                {
                    Vector3 apex = new Vector3(0f, ridgeY, 0f);

                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northWest, northEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northEast, southEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southEast, southWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southWest, northWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
                else
                {
                    Vector3 ridgeLeft = new Vector3(-ridgeHalfLength, ridgeY, 0f);
                    Vector3 ridgeRight = new Vector3(ridgeHalfLength, ridgeY, 0f);

                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northWest, northEast, ridgeRight, ridgeLeft }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northEast, southEast, ridgeRight }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southEast, southWest, ridgeLeft, ridgeRight }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southWest, northWest, ridgeLeft }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
            }
            else
            {
                float ridgeHalfLength = Mathf.Max(0f, halfDepth - halfWidth);

                if (ridgeHalfLength <= Epsilon)
                {
                    Vector3 apex = new Vector3(0f, ridgeY, 0f);

                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northWest, northEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northEast, southEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southEast, southWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southWest, northWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
                else
                {
                    Vector3 ridgeBack = new Vector3(0f, ridgeY, -ridgeHalfLength);
                    Vector3 ridgeFront = new Vector3(0f, ridgeY, ridgeHalfLength);

                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northWest, northEast, ridgeFront }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { northEast, southEast, ridgeBack, ridgeFront }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southEast, southWest, ridgeBack }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofFaceWithVerticalThickness(new List<Vector3> { southWest, northWest, ridgeFront, ridgeBack }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
            }

            AddOuterFascia(
                outerRing,
                roofThickness,
                vertices,
                triangles,
                uvs
            );
        }

        private static void BuildHexagonalRoof(
            float width,
            float depth,
            float wallHeight,
            float roofHeight,
            float overhang,
            float roofThickness,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            int segments = 6;

            float radiusX = width * 0.5f + overhang;
            float radiusZ = depth * 0.5f + overhang;

            float baseY = wallHeight;
            float apexY = wallHeight + roofHeight;

            Vector3 roofCenter = new Vector3(0f, baseY, 0f);
            Vector3 apex = new Vector3(0f, apexY, 0f);

            List<Vector3> ring = BuildRing(segments, radiusX, radiusZ, baseY);

            for (int i = 0; i < segments; i++)
            {
                Vector3 a = ring[i];
                Vector3 b = ring[(i + 1) % segments];

                AddRoofFaceWithVerticalThickness(
                    new List<Vector3> { a, b, apex },
                    roofCenter,
                    roofThickness,
                    vertices,
                    triangles,
                    uvs
                );
            }

            AddOuterFascia(
                ring,
                roofThickness,
                vertices,
                triangles,
                uvs
            );
        }

        private static void BuildConicalRoof(
            float width,
            float depth,
            float wallHeight,
            float roofHeight,
            float overhang,
            float roofThickness,
            int segments,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            float radiusX = width * 0.5f + overhang;
            float radiusZ = depth * 0.5f + overhang;

            float baseY = wallHeight;
            float apexY = wallHeight + roofHeight;

            Vector3 roofCenter = new Vector3(0f, baseY, 0f);

            Vector3 topApex = new Vector3(0f, apexY, 0f);
            Vector3 bottomApex = topApex - Vector3.up * roofThickness;

            List<Vector3> topRing = BuildRing(segments, radiusX, radiusZ, baseY);
            List<Vector3> bottomRing = new List<Vector3>();

            for (int i = 0; i < topRing.Count; i++)
                bottomRing.Add(topRing[i] - Vector3.up * roofThickness);

            int topApexIndex = AddVertex(topApex, new Vector2(0.5f, 0.5f), vertices, uvs);

            List<int> topRingIndices = new List<int>();

            for (int i = 0; i < topRing.Count; i++)
            {
                Vector2 uv = CircularUv(topRing[i], radiusX, radiusZ);
                topRingIndices.Add(AddVertex(topRing[i], uv, vertices, uvs));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = topRingIndices[i];
                int b = topRingIndices[(i + 1) % segments];

                Vector3 desiredNormal = GetOutwardRoofNormal(
                    new List<Vector3> { vertices[a], vertices[b], topApex },
                    roofCenter
                );

                AddTriangleIndicesFacingNormal(
                    a,
                    b,
                    topApexIndex,
                    desiredNormal,
                    vertices,
                    triangles
                );
            }

            int bottomApexIndex = AddVertex(bottomApex, new Vector2(0.5f, 0.5f), vertices, uvs);

            List<int> bottomRingIndices = new List<int>();

            for (int i = 0; i < bottomRing.Count; i++)
            {
                Vector2 uv = CircularUv(bottomRing[i], radiusX, radiusZ);
                bottomRingIndices.Add(AddVertex(bottomRing[i], uv, vertices, uvs));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = bottomRingIndices[i];
                int b = bottomRingIndices[(i + 1) % segments];

                Vector3 desiredNormal = -GetOutwardRoofNormal(
                    new List<Vector3> { topRing[i], topRing[(i + 1) % segments], topApex },
                    roofCenter
                );

                AddTriangleIndicesFacingNormal(
                    bottomApexIndex,
                    b,
                    a,
                    desiredNormal,
                    vertices,
                    triangles
                );
            }

            AddOuterFascia(
                topRing,
                roofThickness,
                vertices,
                triangles,
                uvs
            );
        }

        private static List<Vector3> BuildRing(
            int segments,
            float radiusX,
            float radiusZ,
            float y)
        {
            List<Vector3> ring = new List<Vector3>();

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = -t * Mathf.PI * 2f + Mathf.PI * 0.5f;

                float x = Mathf.Cos(angle) * radiusX;
                float z = Mathf.Sin(angle) * radiusZ;

                ring.Add(new Vector3(x, y, z));
            }

            return ring;
        }

        private static void AddRoofFaceWithVerticalThickness(
            List<Vector3> topFace,
            Vector3 roofCenter,
            float roofThickness,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (topFace == null || topFace.Count < 3)
                return;

            Vector3 desiredTopNormal = GetOutwardRoofNormal(topFace, roofCenter);

            List<Vector3> orientedTop = GetOrientedPolygon(topFace, desiredTopNormal);

            AddPolygonWithPlanarUvs(
                orientedTop,
                vertices,
                triangles,
                uvs
            );

            List<Vector3> bottomFace = new List<Vector3>();

            for (int i = orientedTop.Count - 1; i >= 0; i--)
                bottomFace.Add(orientedTop[i] - Vector3.up * roofThickness);

            AddPolygonWithPlanarUvs(
                bottomFace,
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddOuterFascia(
            List<Vector3> topRing,
            float roofThickness,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (topRing == null || topRing.Count < 3)
                return;

            int count = topRing.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 topA = topRing[i];
                Vector3 topB = topRing[(i + 1) % count];

                Vector3 bottomA = topA - Vector3.up * roofThickness;
                Vector3 bottomB = topB - Vector3.up * roofThickness;

                Vector3 mid = (topA + topB) * 0.5f;
                Vector3 desiredNormal = new Vector3(mid.x, 0f, mid.z).normalized;

                if (desiredNormal.sqrMagnitude <= Epsilon)
                    desiredNormal = Vector3.forward;

                AddQuadFacingNormal(
                    topA,
                    topB,
                    bottomB,
                    bottomA,
                    desiredNormal,
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        private static Vector3 GetOutwardRoofNormal(
            List<Vector3> face,
            Vector3 roofCenter)
        {
            Vector3 faceCenter = Vector3.zero;

            for (int i = 0; i < face.Count; i++)
                faceCenter += face[i];

            faceCenter /= face.Count;

            Vector3 desiredNormal = faceCenter - roofCenter;

            if (desiredNormal.sqrMagnitude <= Epsilon)
                desiredNormal = Vector3.up;

            desiredNormal.y = Mathf.Abs(desiredNormal.y) + 0.1f;

            return desiredNormal.normalized;
        }

        private static List<Vector3> GetOrientedPolygon(
            List<Vector3> polygon,
            Vector3 desiredNormal)
        {
            List<Vector3> result = new List<Vector3>(polygon);

            Vector3 currentNormal = CalculatePolygonNormal(result);

            if (Vector3.Dot(currentNormal, desiredNormal) < 0f)
                result.Reverse();

            return result;
        }

        private static Vector3 CalculatePolygonNormal(List<Vector3> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return Vector3.up;

            for (int i = 0; i < polygon.Count - 2; i++)
            {
                Vector3 a = polygon[i];
                Vector3 b = polygon[i + 1];
                Vector3 c = polygon[i + 2];

                Vector3 normal = Vector3.Cross(b - a, c - a);

                if (normal.sqrMagnitude > Epsilon)
                    return normal.normalized;
            }

            return Vector3.up;
        }

        private static void AddPolygonWithPlanarUvs(
            List<Vector3> polygon,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (polygon == null || polygon.Count < 3)
                return;

            int startIndex = vertices.Count;

            Vector3 normal = CalculatePolygonNormal(polygon);
            Vector3 uAxis = (polygon[1] - polygon[0]).normalized;

            if (uAxis.sqrMagnitude <= Epsilon)
                uAxis = Vector3.right;

            Vector3 vAxis = Vector3.Cross(normal, uAxis).normalized;

            if (vAxis.sqrMagnitude <= Epsilon)
                vAxis = Vector3.forward;

            Vector3 origin = polygon[0];

            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 point = polygon[i];
                Vector3 delta = point - origin;

                vertices.Add(point);
                uvs.Add(new Vector2(
                    Vector3.Dot(delta, uAxis),
                    Vector3.Dot(delta, vAxis)
                ));
            }

            for (int i = 1; i < polygon.Count - 1; i++)
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + i);
                triangles.Add(startIndex + i + 1);
            }
        }

        private static void AddQuadFacingNormal(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 currentNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            if (Vector3.Dot(currentNormal, desiredNormal.normalized) < 0f)
            {
                AddQuad(
                    v3,
                    v2,
                    v1,
                    v0,
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
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            int startIndex = vertices.Count;

            float width = Vector3.Distance(v0, v1);
            float height = Vector3.Distance(v1, v2);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(width, 0f));
            uvs.Add(new Vector2(width, height));
            uvs.Add(new Vector2(0f, height));

            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);

            triangles.Add(startIndex);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }

        private static int AddVertex(
            Vector3 vertex,
            Vector2 uv,
            List<Vector3> vertices,
            List<Vector2> uvs)
        {
            int index = vertices.Count;

            vertices.Add(vertex);
            uvs.Add(uv);

            return index;
        }

        private static Vector2 CircularUv(
            Vector3 point,
            float radiusX,
            float radiusZ)
        {
            float u = 0.5f;

            if (radiusX > Epsilon)
                u += point.x / (radiusX * 2f);

            float v = 0.5f;

            if (radiusZ > Epsilon)
                v += point.z / (radiusZ * 2f);

            return new Vector2(u, v);
        }

        private static void AddTriangleIndicesFacingNormal(
            int i0,
            int i1,
            int i2,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles)
        {
            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            Vector3 currentNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            if (Vector3.Dot(currentNormal, desiredNormal.normalized) < 0f)
            {
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i0);
            }
            else
            {
                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);
            }
        }
    }
}