using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    [System.Serializable]
    public class ProceduralStroke
    {
        [SerializeField] private List<Vector3> points = new List<Vector3>();

        public int Count => points != null ? points.Count : 0;

        public IReadOnlyList<Vector3> Points => points;

        public Vector3 GetPoint(int index)
        {
            if (points == null)
                return Vector3.zero;

            if (index < 0 || index >= points.Count)
                return Vector3.zero;

            return points[index];
        }

        public void AddPoint(Vector3 point)
        {
            EnsureList();
            points.Add(point);
        }

        public void SetPoint(int index, Vector3 point)
        {
            if (points == null)
                return;

            if (index < 0 || index >= points.Count)
                return;

            points[index] = point;
        }

        public void RemovePointAt(int index)
        {
            if (points == null)
                return;

            if (index < 0 || index >= points.Count)
                return;

            points.RemoveAt(index);
        }

        public void Clear()
        {
            EnsureList();
            points.Clear();
        }

        public void SetWorldPoints(List<Vector3> newPoints)
        {
            EnsureList();
            points.Clear();

            if (newPoints == null)
                return;

            for (int i = 0; i < newPoints.Count; i++)
                points.Add(newPoints[i]);
        }

        public List<Vector3> ToList()
        {
            EnsureList();
            return new List<Vector3>(points);
        }

        private void EnsureList()
        {
            if (points == null)
                points = new List<Vector3>();
        }
    }
}