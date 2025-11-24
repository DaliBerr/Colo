using Kernel;
using Kernel.Building;
using Lonize.Logging;
using Lonize.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kernel.UI
{
    [UIPrefab("Prefabs/UI/Main UI")]
    public sealed class MainUI : UIScreen
    {
        public Button Btn1, Btn2;
        public GameObject MiniMapContainer;
        protected override void OnInit()
        {
            BuildingPlacementController buildingPlacementController = FindAnyObjectByType<BuildingPlacementController>();
            BuildingRemoveController buildingRemoveController = FindAnyObjectByType<BuildingRemoveController>();

            Btn1.onClick.AddListener(() => buildingPlacementController.StartPlacementByIndex(0));
            Btn2.onClick.AddListener(() => buildingRemoveController.StartRemoveMode());
        }
    }
}