using UnityEngine;

namespace ProceduralEnvironment
{
    public struct WallOpeningData
    {
        public WallOpeningType OpeningType;

        public float CenterDistance;
        public float Width;

        public float BottomHeight;
        public float Height;

        public float StartDistance => CenterDistance - Width * 0.5f;
        public float EndDistance => CenterDistance + Width * 0.5f;
        public float TopHeight => BottomHeight + Height;

        public Vector3 WorldPosition;
    }
}