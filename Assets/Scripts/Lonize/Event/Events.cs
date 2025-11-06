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
}
    

