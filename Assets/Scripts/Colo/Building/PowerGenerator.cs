using Kernel.Building;
using Kernel.Flow;
using Lonize.Flow;
using Lonize.Logging;
using UnityEngine;

namespace Game.Buildings
{
    /// <summary>
    /// 使用 BuildingDef 初始化参数的发电机运行时脚本。
    /// </summary>
    public class PowerGeneratorRuntime : MonoBehaviour, IFlowEndpointAdapter
    {
        [Header("引用")]
        [SerializeField]
        // private Kernel.Building.BuildingRuntimeHost _buildingRuntimeHost;

        [Header("发电机基础参数（会在 Start 时被 Def 覆盖）")]
        public float maxOutput = 100f;
        public float desiredOutput = 100f;
        public float fuelPerPowerUnit = 0.01f;
        public int supplyPriority = 0;

        [Header("运行时状态")]
        public float fuelAmount = 100f;
        public bool isOn = true;
        public float lastActualOutput = 0f;

        private FlowEndpoint _endpoint;

        /// <summary>
        /// 在 Awake 中注册 Flow 端口。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void Awake()
        {
            _endpoint = new FlowEndpoint(
                FlowResourceType.Power,
                FlowEndpointKind.Producer,
                this
            );
            FlowSystem.Instance.RegisterEndpoint(_endpoint);
        }

        /// <summary>
        /// 在 Start 中从 BuildingDef 读取发电机参数。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void Start()
        {
            var _buildingRuntimeHost = GetComponentInParent<BuildingRuntimeHost>();
            if (_buildingRuntimeHost == null)
            {
                return;
            }

            var def = _buildingRuntimeHost.Runtime?.Def as Colo.Def.Building.PowerGeneratorDef;
            
            Debug.Log($"[PowerGeneratorRuntime] Runtime={_buildingRuntimeHost.Runtime}, " +
            $"Def={_buildingRuntimeHost.Runtime?.Def}, " +
            $"DefType={_buildingRuntimeHost.Runtime?.Def?.GetType()}");

            Log.Info($"[PowerGeneratorRuntime] 加载发电机参数，Def={def?.Name}");
            if (def == null)
            {
                Log.Warn("[PowerGeneratorRuntime] 发电机参数 Def 为空，无法加载参数");
                return;
            }

            maxOutput = def.maxOutput;
            desiredOutput = def.defaultDesiredOutput;
            fuelPerPowerUnit = def.fuelPerPowerUnit;
            supplyPriority = def.defaultSupplyPriority;
        }

        /// <summary>
        /// 在对象销毁时注销 Flow 端口。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void OnDestroy()
        {
            if (_endpoint != null)
            {
                FlowSystem.Instance.UnregisterEndpoint(_endpoint);
                _endpoint = null;
            }
        }

        /// <summary>
        /// 设置发电机的开关状态。
        /// </summary>
        /// <param name="on">是否开启。</param>
        /// <returns>无返回值。</returns>
        public void SetOn(bool on)
        {
            isOn = on;
        }

        /// <summary>
        /// 获取希望的输出功率。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>希望输出功率。</returns>
        public float GetDesiredRate(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            if (!isOn)
            {
                return 0f;
            }

            if (fuelAmount <= 0f)
            {
                return 0f;
            }

            return desiredOutput;
        }

        /// <summary>
        /// 获取最大发电功率上限。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>最大发电功率上限。</returns>
        public float GetMaxRate(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            return maxOutput;
        }

        /// <summary>
        /// 获取供电优先级。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>供电优先级数值。</returns>
        public int GetPriority(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            return supplyPriority;
        }

        /// <summary>
        /// 应用实际输出功率并更新燃料与状态。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <param name="actualRate">实际输出功率。</param>
        /// <param name="deltaTime">时间步长。</param>
        /// <returns>无返回值。</returns>
        public void ApplyFlow(FlowResourceType resourceType, FlowEndpointKind kind, float actualRate, float deltaTime)
        {
            lastActualOutput = actualRate;

            float fuelConsumed = actualRate * fuelPerPowerUnit * deltaTime;
            fuelAmount -= fuelConsumed;

            if (fuelAmount <= 0f)
            {
                fuelAmount = 0f;
                isOn = false;
                // TODO: 在这里可以触发“没油了”的事件或 UI 提示
            }
        }
    }
}
