
using System.Collections.Generic;

namespace Kernel.Status
{
    public struct Status
    {
        public string StatusName;
        public List<string> InActiveWith;
        public List<string> allowSwitchWith;
    };
    public static class StatusList
    {
        public static Status BuildingPlacementStatus = new Status
        {
            StatusName = "PlacingBuilding",
            InActiveWith = null,
            allowSwitchWith = new List<string> { "RemovingBuilding" }
        };
        public static Status RemovingBuildingStatus = new Status
        {
            StatusName = "RemovingBuilding",
            InActiveWith = null,
            allowSwitchWith = new List<string> { "PlacingBuilding" }
        };
        
    }
}