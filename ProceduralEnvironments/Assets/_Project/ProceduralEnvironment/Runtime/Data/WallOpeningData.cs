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

        public bool RoundTopCorners;
        public float TopCornerRadius;
        public int TopCornerSegments;

        public Vector3 WorldPosition;

        public float StartDistance => CenterDistance - Width * 0.5f;
        public float EndDistance => CenterDistance + Width * 0.5f;
        public float TopHeight => BottomHeight + Height;

        public float SafeTopCornerRadius
        {
            get
            {
                if (!RoundTopCorners)
                    return 0f;

                return Mathf.Min(
                    TopCornerRadius,
                    Width * 0.5f,
                    Height
                );
            }
        }
    }
}