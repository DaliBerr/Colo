using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Lonize.Events;
using Lonize.UI;
using Unity.VisualScripting;
namespace Kernel
{
    public class MiniMapControl : CameraControl
    {
        public override Camera CameraComponent { get; set; }

        // private Transform MapCenter;
        private void OnEnable()
        {
            Events.eventBus.Subscribe<MapReady>(OnMapReady);
        }

        private void OnDisable()
        {
            Events.eventBus.Unsubscribe<MapReady>(OnMapReady);
        }

        void Start()
        {
            CameraComponent = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (!isPointerCanMoveMiniMapCamera())
                return;
            HandlePan();
            HandleZoom();
        }

        private void OnMapReady(MapReady evt)
        {
            if (evt.value)
            {
                var MapCenter = evt.mapCenterPosition;
                Vector3 newPos = new Vector3(MapCenter.x, MapCenter.y, this.transform.position.z);
                this.transform.position = newPos;
            }
        }

        private bool isPointerCanMoveMiniMapCamera()
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

    }
}