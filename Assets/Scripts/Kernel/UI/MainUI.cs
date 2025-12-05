using System.Collections.Generic;
using Kernel;
using Kernel.Building;
using Kernel.Status;
using Lonize.Logging;
using Lonize.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Kernel.UI
{
    [UIPrefab("Prefabs/UI/Main UI")]
    public sealed class MainUI : UIScreen
    {
        public Button Btn1, Btn2;
        public GameObject MiniMapContainer;
        // public List<string> CurrentStatus = new();
        
        protected override void OnInit()
        {
            // BuildingPlacementController buildingPlacementController = FindAnyObjectByType<BuildingPlacementController>();

            Btn1.onClick.AddListener(() => TrybuildingPlacementMode());
            Btn2.onClick.AddListener(() => TryBuildingRemoveMode());
        }

        private void TryBuildingRemoveMode()
        {
            BuildingRemoveController buildingRemoveController = FindAnyObjectByType<BuildingRemoveController>();
            if(StatusController.HasStatus(StatusList.RemovingBuildingStatus))
            {
                buildingRemoveController.StopRemoveMode();
                return;
            }
            else
            {
                buildingRemoveController.StartRemoveMode();
                return;
            }
            // Status currentStatus = StatusController.CurrentStatus.Find(s => s.StatusName == "RemovingBuilding");
            // BuildingRemoveController buildingRemoveController = FindAnyObjectByType<BuildingRemoveController>();
        }

        private void TrybuildingPlacementMode()
        {
            BuildingPlacementController buildingPlacementController = FindAnyObjectByType<BuildingPlacementController>();
            if(StatusController.HasStatus(StatusList.BuildingPlacementStatus))
            {
                buildingPlacementController.CancelPlacement();
                return;
            }
            else
            {
                buildingPlacementController.StartPlacementByIndex(0);
                return;
            }
        }
    }
}