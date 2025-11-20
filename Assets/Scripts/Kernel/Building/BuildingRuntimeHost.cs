
using System.Collections.Generic;
using Lonize.Logging;
using UnityEngine;

namespace Kernel.Building
{
    public class BuildingRuntimeHost : MonoBehaviour
    {
        // [SerializeField]
        public BuildingRuntime Runtime;
        public List<IBuildingBehaviour> Behaviours = new();

        void Start()
        {
            // var def = Runtime.Def;
            // Log.Info($"[BuildingRuntimeHost] Building '{def.Name}' initialized.");
            // var sprite = GetComponent<SpriteRenderer>();
            // var collider = GetComponent<BoxCollider>();

            // collider.size = new Vector3(def.Width, def.Height, 1f);
        }
        // void Start()
        // {
        //     var def = Runtime.Def;
        //     // var sprite = GetComponent<SpriteRenderer>();
        //     var collider = GetComponent<BoxCollider2D>();

        //     collider.size = new Vector2(def.Width, def.Height);


        // }
    }
}