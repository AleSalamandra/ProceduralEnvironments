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
            float textureScale)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Generated_Wall_Mesh";

            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            if (stroke == null || stroke.Count < 2)
                return mesh;

            float accumulatedDistance = 0f;

            for (int i = 0; i < stroke.Count - 1; i++)
            {
                Vector3 worldA = stroke.GetPoint(i);
                Vector3 worldB = stroke.GetPoint(i + 1);

                Vector3 localA = owner.InverseTransformPoint(worldA);
                Vector3 localB = owner.InverseTransformPoint(worldB);

                float segmentLength = Vector3.Distance(localA, localB);

                if (segmentLength <= 0.001f)
                    continue;

                AddWallSegment(
                    localA,
                    localB,
                    height,
                    thickness,
                    textureScale,
                    accumulatedDistance,
                    segmentLength,
                    vertices,
                    triangles,
                    uvs
                );

                accumulatedDistance += segmentLength;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddWallSegment(
            Vector3 a,
            Vector3 b,
            float height,
            float thickness,
            float textureScale,
            float distanceStart,
            float segmentLength,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector3 direction = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * (thickness * 0.5f);

            Vector3 aLeftBottom = a - side;
            Vector3 aRightBottom = a + side;
            Vector3 bLeftBottom = b - side;
            Vector3 bRightBottom = b + side;

            Vector3 aLeftTop = aLeftBottom + Vector3.up * height;
            Vector3 aRightTop = aRightBottom + Vector3.up * height;
            Vector3 bLeftTop = bLeftBottom + Vector3.up * height;
            Vector3 bRightTop = bRightBottom + Vector3.up * height;

            float u0 = distanceStart / textureScale;
            float u1 = (distanceStart + segmentLength) / textureScale;
            float vHeight = height / textureScale;
            float vThickness = thickness / textureScale;

            // Lado izquierdo
            AddQuad(
                aLeftBottom, bLeftBottom, bLeftTop, aLeftTop,
                new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, vHeight), new Vector2(u0, vHeight),
                vertices, triangles, uvs
            );

            // Lado derecho
            AddQuad(
                bRightBottom, aRightBottom, aRightTop, bRightTop,
                new Vector2(u1, 0), new Vector2(u0, 0), new Vector2(u0, vHeight), new Vector2(u1, vHeight),
                vertices, triangles, uvs
            );

            // Parte superior
            AddQuad(
                aLeftTop, bLeftTop, bRightTop, aRightTop,
                new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, vThickness), new Vector2(u0, vThickness),
                vertices, triangles, uvs
            );

            // Tapa inicial
            AddQuad(
                aRightBottom, aLeftBottom, aLeftTop, aRightTop,
                new Vector2(0, 0), new Vector2(vThickness, 0), new Vector2(vThickness, vHeight), new Vector2(0, vHeight),
                vertices, triangles, uvs
            );

            // Tapa final
            AddQuad(
                bLeftBottom, bRightBottom, bRightTop, bLeftTop,
                new Vector2(0, 0), new Vector2(vThickness, 0), new Vector2(vThickness, vHeight), new Vector2(0, vHeight),
                vertices, triangles, uvs
            );
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