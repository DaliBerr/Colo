
using System.Collections.Generic;
// using Lonize.Logging;
using UnityEngine;

namespace Kernel.Building
{
    public class BuildingRuntimeHost : MonoBehaviour
    {
        // [SerializeField]
        public BuildingRuntime Runtime;
        public List<IBuildingBehaviour> Behaviours = new();

    }
}