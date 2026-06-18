using System.Collections.Generic;
using UnityEngine;

namespace ProceduralEnvironment
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(ProceduralBuilding))]
    public class BuildingWallsFeature : ProceduralBuildingFeature
    {
        protected override string GeneratedRootName => "_GeneratedBuildingWalls";

        [Header("Wall Source")]
        [SerializeField] private ProceduralWall wallPrefab;

        [Header("Generated Wall")]
        [SerializeField] private string generatedWallName = "Generated_ContinuousWall";
        [SerializeField] private bool forceClosedLoop = true;

        protected override void OnRebuild(ProceduralBuilding building)
        {
            List<Vector3> points = building.GetFootprintWorldPoints(forceClosedLoop);

            if (points == null || points.Count < 3)
                return;

            Transform root = GetOrCreateGeneratedRoot();

            ProceduralWall wall = CreateWall(root);

            if (wall == null)
                return;

            wall.name = generatedWallName;

            wall.SetStrokeWorldPoints(points);
            wall.SetWallSettings(building.WallHeight, building.WallThickness);
            wall.ForceRegenerateWall();
        }

        private ProceduralWall CreateWall(Transform parent)
        {
            ProceduralWall wall;

            if (wallPrefab != null)
            {
                wall = Instantiate(wallPrefab, parent);
            }
            else
            {
                GameObject wallObject = new GameObject(generatedWallName);
                wallObject.transform.SetParent(parent);
                wallObject.transform.localPosition = Vector3.zero;
                wallObject.transform.localRotation = Quaternion.identity;
                wallObject.transform.localScale = Vector3.one;

                wall = wallObject.AddComponent<ProceduralWall>();
            }

            wall.transform.localPosition = Vector3.zero;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one;

            return wall;
        }
    }
}