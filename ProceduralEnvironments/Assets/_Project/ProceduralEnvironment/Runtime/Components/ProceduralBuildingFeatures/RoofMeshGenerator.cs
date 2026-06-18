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
                    BuildPyramidRoof(
                        6,
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
            float topY = wallHeight + roofHeight;

            Vector3 roofCenter = new Vector3(0f, baseY, 0f);

            Vector3 northWest = new Vector3(-halfWidth, baseY, halfDepth);
            Vector3 northEast = new Vector3(halfWidth, baseY, halfDepth);
            Vector3 southEast = new Vector3(halfWidth, baseY, -halfDepth);
            Vector3 southWest = new Vector3(-halfWidth, baseY, -halfDepth);

            List<Vector3> eaveRing = new List<Vector3>
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
                    Vector3 apex = new Vector3(0f, topY, 0f);

                    AddRoofSolidFace(new List<Vector3> { northWest, northEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { northEast, southEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southEast, southWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southWest, northWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
                else
                {
                    Vector3 ridgeLeft = new Vector3(-ridgeHalfLength, topY, 0f);
                    Vector3 ridgeRight = new Vector3(ridgeHalfLength, topY, 0f);

                    AddRoofSolidFace(new List<Vector3> { northWest, northEast, ridgeRight, ridgeLeft }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { northEast, southEast, ridgeRight }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southEast, southWest, ridgeLeft, ridgeRight }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southWest, northWest, ridgeLeft }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
            }
            else
            {
                float ridgeHalfLength = Mathf.Max(0f, halfDepth - halfWidth);

                if (ridgeHalfLength <= Epsilon)
                {
                    Vector3 apex = new Vector3(0f, topY, 0f);

                    AddRoofSolidFace(new List<Vector3> { northWest, northEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { northEast, southEast, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southEast, southWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southWest, northWest, apex }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
                else
                {
                    Vector3 ridgeBack = new Vector3(0f, topY, -ridgeHalfLength);
                    Vector3 ridgeFront = new Vector3(0f, topY, ridgeHalfLength);

                    AddRoofSolidFace(new List<Vector3> { northWest, northEast, ridgeFront }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { northEast, southEast, ridgeBack, ridgeFront }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southEast, southWest, ridgeBack }, roofCenter, roofThickness, vertices, triangles, uvs);
                    AddRoofSolidFace(new List<Vector3> { southWest, northWest, ridgeFront, ridgeBack }, roofCenter, roofThickness, vertices, triangles, uvs);
                }
            }

            AddFasciaRing(
                eaveRing,
                roofThickness,
                vertices,
                triangles,
                uvs
            );
        }

        private static void BuildPyramidRoof(
            int sides,
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
            float radiusX = width * 0.5f + overhang;
            float radiusZ = depth * 0.5f + overhang;

            float baseY = wallHeight;
            float apexY = wallHeight + roofHeight;

            Vector3 roofCenter = new Vector3(0f, baseY, 0f);
            Vector3 apex = new Vector3(0f, apexY, 0f);

            List<Vector3> ring = BuildRing(sides, radiusX, radiusZ, baseY);

            for (int i = 0; i < sides; i++)
            {
                Vector3 a = ring[i];
                Vector3 b = ring[(i + 1) % sides];

                AddRoofSolidFace(
                    new List<Vector3> { a, b, apex },
                    roofCenter,
                    roofThickness,
                    vertices,
                    triangles,
                    uvs
                );
            }

            AddFasciaRing(
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
            List<Vector3> bottomRing = OffsetDown(topRing, roofThickness);

            int topApexIndex = AddVertex(topApex, new Vector2(0.5f, 0.5f), vertices, uvs);
            List<int> topRingIndices = AddRingVerticesWithCircularUvs(topRing, radiusX, radiusZ, vertices, uvs);

            for (int i = 0; i < segments; i++)
            {
                int a = topRingIndices[i];
                int b = topRingIndices[(i + 1) % segments];

                Vector3 desiredNormal = GetOutwardRoofNormal(
                    new List<Vector3> { vertices[a], vertices[b], topApex },
                    roofCenter
                );

                AddTriangleIndicesFacingNormal(a, b, topApexIndex, desiredNormal, vertices, triangles);
            }

            int bottomApexIndex = AddVertex(bottomApex, new Vector2(0.5f, 0.5f), vertices, uvs);
            List<int> bottomRingIndices = AddRingVerticesWithCircularUvs(bottomRing, radiusX, radiusZ, vertices, uvs);

            for (int i = 0; i < segments; i++)
            {
                int a = bottomRingIndices[i];
                int b = bottomRingIndices[(i + 1) % segments];

                Vector3 topA = topRing[i];
                Vector3 topB = topRing[(i + 1) % segments];

                Vector3 desiredNormal = -GetOutwardRoofNormal(
                    new List<Vector3> { topA, topB, topApex },
                    roofCenter
                );

                AddTriangleIndicesFacingNormal(bottomApexIndex, b, a, desiredNormal, vertices, triangles);
            }

            AddFasciaRing(
                topRing,
                roofThickness,
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddRoofSolidFace(
            List<Vector3> topFace,
            Vector3 roofCenter,
            float roofThickness,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (topFace == null || topFace.Count < 3)
                return;

            Vector3 topNormal = GetOutwardRoofNormal(topFace, roofCenter);

            AddPolygonFace(
                topFace,
                topNormal,
                vertices,
                triangles,
                uvs
            );

            List<Vector3> bottomFace = OffsetDown(topFace, roofThickness);

            AddPolygonFace(
                bottomFace,
                -topNormal,
                vertices,
                triangles,
                uvs
            );
        }

        private static void AddFasciaRing(
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
                Vector3 desiredNormal = new Vector3(mid.x, 0f, mid.z);

                if (desiredNormal.sqrMagnitude <= Epsilon)
                    desiredNormal = Vector3.forward;

                AddQuadFace(
                    topA,
                    topB,
                    bottomB,
                    bottomA,
                    desiredNormal.normalized,
                    vertices,
                    triangles,
                    uvs
                );
            }
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

        private static List<Vector3> OffsetDown(
            List<Vector3> points,
            float amount)
        {
            List<Vector3> result = new List<Vector3>();

            for (int i = 0; i < points.Count; i++)
                result.Add(points[i] - Vector3.up * amount);

            return result;
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
            desiredNormal.y = Mathf.Abs(desiredNormal.y) + 0.25f;

            if (desiredNormal.sqrMagnitude <= Epsilon)
                return Vector3.up;

            return desiredNormal.normalized;
        }

        private static void AddPolygonFace(
            List<Vector3> polygon,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            if (polygon == null || polygon.Count < 3)
                return;

            List<Vector3> oriented = new List<Vector3>(polygon);
            Vector3 currentNormal = CalculatePolygonNormal(oriented);

            if (Vector3.Dot(currentNormal, desiredNormal.normalized) < 0f)
                oriented.Reverse();

            int startIndex = vertices.Count;

            Vector3 normal = CalculatePolygonNormal(oriented);

            Vector3 uAxis = (oriented[1] - oriented[0]).normalized;

            if (uAxis.sqrMagnitude <= Epsilon)
                uAxis = Vector3.right;

            Vector3 vAxis = Vector3.Cross(normal, uAxis).normalized;

            if (vAxis.sqrMagnitude <= Epsilon)
                vAxis = Vector3.forward;

            Vector3 origin = oriented[0];

            for (int i = 0; i < oriented.Count; i++)
            {
                Vector3 point = oriented[i];
                Vector3 delta = point - origin;

                vertices.Add(point);
                uvs.Add(new Vector2(
                    Vector3.Dot(delta, uAxis),
                    Vector3.Dot(delta, vAxis)
                ));
            }

            for (int i = 1; i < oriented.Count - 1; i++)
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + i);
                triangles.Add(startIndex + i + 1);
            }
        }

        private static void AddQuadFace(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 desiredNormal,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            AddPolygonFace(
                new List<Vector3> { v0, v1, v2, v3 },
                desiredNormal,
                vertices,
                triangles,
                uvs
            );
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

        private static List<int> AddRingVerticesWithCircularUvs(
            List<Vector3> ring,
            float radiusX,
            float radiusZ,
            List<Vector3> vertices,
            List<Vector2> uvs)
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < ring.Count; i++)
            {
                indices.Add(AddVertex(
                    ring[i],
                    CircularUv(ring[i], radiusX, radiusZ),
                    vertices,
                    uvs
                ));
            }

            return indices;
        }

        private static Vector2 CircularUv(
            Vector3 point,
            float radiusX,
            float radiusZ)
        {
            float u = 0.5f;
            float v = 0.5f;

            if (radiusX > Epsilon)
                u += point.x / (radiusX * 2f);

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