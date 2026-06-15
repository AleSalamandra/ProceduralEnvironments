using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    [System.Serializable]
    public class ProceduralStroke
    {
        [SerializeField] private List<Vector3> points = new();

        public IReadOnlyList<Vector3> Points => points;
        public int Count => points.Count;

        public void AddPoint(Vector3 point)
        {
            points.Add(point);
        }

        public void SetPoint(int index, Vector3 point)
        {
            if (index < 0 || index >= points.Count)
                return;

            points[index] = point;
        }

        public Vector3 GetPoint(int index)
        {
            if (index < 0 || index >= points.Count)
                return Vector3.zero;

            return points[index];
        }

        public void RemovePointAt(int index)
        {
            if (index < 0 || index >= points.Count)
                return;

            points.RemoveAt(index);
        }

        public void Clear()
        {
            points.Clear();
        }
    }
}