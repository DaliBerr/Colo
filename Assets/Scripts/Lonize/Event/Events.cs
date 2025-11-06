using System.Collections.Generic;
using Kernel.Item;
using Lonize.Tick;
using UnityEngine;

namespace Lonize.Events
{
    public static class Events
    {
        public static readonly EventBus eventBus = new();
    }
    public readonly struct MapReady
    {
        public readonly bool value;
        public readonly Vector3 mapCenterPosition;
        public MapReady(bool value, Vector3 mapCenterPosition)
        {
            this.value = value;
            this.mapCenterPosition = mapCenterPosition;
        }
    }
    public readonly struct SpeedChange
    {
        public readonly float speedMultiplier;
        public readonly GameSpeed currentGameSpeed;
        public SpeedChange(float speedMultiplier, GameSpeed currentGameSpeed)
        {
            this.speedMultiplier = speedMultiplier;
            this.currentGameSpeed = currentGameSpeed;
        }
    }
    public readonly struct ItemLoadingProgress
    {
        public readonly int loadedCount;
        public readonly int totalCount;
        public ItemLoadingProgress(int loadedCount, int totalCount)
        {
            this.loadedCount = loadedCount;
            this.totalCount = totalCount;
        }
    }
    public readonly struct ItemLoaded
    {
        public readonly int itemCount;
        // public readonly string Defkeys;
        
        // public readonly List<ItemDef> itemDefs;
        public ItemLoaded(int itemCount)
        {
            this.itemCount = itemCount;
            // this.Defkeys = defKeys;
            // this.itemDefs = itemDefs;
        }
    }
}
    

