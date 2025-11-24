using UnityEngine;
using Lonize.Flow;


namespace Kernel.Flow
{
    /// <summary>
    /// 用于把 Unity 中的建筑对象接入 FlowSystem 的桥接组件。
    /// </summary>
    [DisallowMultipleComponent]
    public class FlowEndpointComponent : MonoBehaviour, IFlowEndpointAdapter
    {
        [Header("Flow 基本配置")]
        public FlowResourceType resourceType = FlowResourceType.Power;
        public FlowEndpointKind endpointKind = FlowEndpointKind.Consumer;

        [Tooltip("最大流量上限，例如最大用电功率。")]
        public float maxRate = 10f;

        [Tooltip("希望的目标流量，例如满负荷时需要的电力。")]
        public float desiredRate = 10f;

        [Tooltip("优先级，资源不足时数值越大越优先被满足。")]
        public int priority = 0;

        /// <summary>
        /// 最近一次结算得到的实际流量，仅用于调试或 UI 显示。
        /// </summary>
        public float LastActualRate { get; private set; }

        /// <summary>
        /// 对应的 Flow 端口对象，可供外部通过代码连接使用。
        /// </summary>
        public FlowEndpoint Endpoint => _endpoint;

        private FlowEndpoint _endpoint;

        /// <summary>
        /// Unity 生命周期回调，在对象启用时创建端口并注册到 FlowSystem。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void Awake()
        {
            _endpoint = new FlowEndpoint(resourceType, endpointKind, this);
            FlowSystem.Instance.RegisterEndpoint(_endpoint);
        }

        /// <summary>
        /// Unity 生命周期回调，在对象销毁时从 FlowSystem 中注销端口。
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
        /// 获取当前希望的流量，默认直接返回组件上配置的值。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>希望的流量。</returns>
        public float GetDesiredRate(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            // 这里你可以改成从 BuildingRuntimeHost/Def 里读取动态值
            return desiredRate;
        }

        /// <summary>
        /// 获取该端口的最大流量上限。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>最大流量上限。</returns>
        public float GetMaxRate(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            return maxRate;
        }

        /// <summary>
        /// 获取该端口的优先级。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <returns>优先级数值。</returns>
        public int GetPriority(FlowResourceType resourceType, FlowEndpointKind kind)
        {
            return priority;
        }

        /// <summary>
        /// 应用 FlowSystem 的结算结果。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="kind">端口类型。</param>
        /// <param name="actualRate">实际流量。</param>
        /// <param name="deltaTime">时间步长。</param>
        /// <returns>无返回值。</returns>
        public void ApplyFlow(FlowResourceType resourceType, FlowEndpointKind kind, float actualRate, float deltaTime)
        {
            LastActualRate = actualRate;
            // TODO：在这里把 actualRate * deltaTime 转换成：
            //  - 若是消费者：消耗多少“电量/算力”，控制生产效率、开关机状态等。
            //  - 若是生产者：根据实际出力消耗燃料、消耗算力等。
        }
    }
}
