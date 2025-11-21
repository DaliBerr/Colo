using Lonize.Events;
using Lonize.Logging;
using UnityEngine;

namespace Kernel.Building
{
    public enum BuildingViewMode
    {
        Normal,
        Ghost,
        Selected,
        Disabled
    }

    [RequireComponent(typeof(BuildingRuntimeHost))]
    public class BuildingView : MonoBehaviour
    {
        [Header("基础引用")]
        [SerializeField] private SpriteRenderer _mainRenderer;
        [SerializeField] private GameObject _selectionHighlight;
        [SerializeField] private GameObject _ghostVisual;

        [Header("颜色配置")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _ghostOkColor = new Color(0.7f, 1f, 0.7f, 0.6f);
        [SerializeField] private Color _ghostBlockedColor = new Color(1f, 0.5f, 0.5f, 0.6f);

        private BuildingRuntimeHost _host;

        void Awake()
        {
            _host = GetComponent<BuildingRuntimeHost>();
            SetMode(BuildingViewMode.Normal);
        }
        // private void OnEnable()
        // {
        //     Events.eventBus.Subscribe<BuildingSelected>(OnBuildingSelected);
        // }
        // private void OnDisable()
        // {
        //     Events.eventBus.Unsubscribe<BuildingSelected>(OnBuildingSelected);
        // }
        private void OnMouseDown()
        {
            Log.Info("[BuildingView] OnMouseDown - selecting building.");
            // 如果 runtimeHost 还没准备好，就别选了，避免空引用
            if (_host == null || _host.Runtime == null)
            {
                Debug.LogWarning("[BuildingView] OnMouseDown but runtimeHost / Runtime is null.");
                return;
            }

            var evt = new BuildingSelected
            {
                buildingRuntime = _host.Runtime,
                isSelected = true,
            };

            Events.eventBus.Publish(evt);
        }
        // private void OnBuildingSelected(BuildingSelected evt)
        // {
        //     // Log.Info("[BuildingView] OnBuildingSelected event received.");
        //     // Log.Info($"[BuildingView] Event buildingID: {evt.buildingRuntime.BuildingID},{_host.Runtime.BuildingID} isSelected: {evt.isSelected},");
        //     if(evt.buildingRuntime == null)
        //     {
        //         SetMode(BuildingViewMode.Normal);
        //         return;
        //     }
        //     if (evt.buildingRuntime.BuildingID == _host.Runtime.BuildingID)
        //     {
                
        //         SetMode(evt.isSelected ? BuildingViewMode.Selected : BuildingViewMode.Normal);
                
        //     }
        // }
        /// <summary>切换显示模式。</summary>
        public void SetMode(BuildingViewMode mode)
        {
            switch (mode)
            {
                case BuildingViewMode.Normal:
                    if (_ghostVisual) _ghostVisual.SetActive(false);
                    if (_selectionHighlight) _selectionHighlight.SetActive(false);
                    if (_mainRenderer) _mainRenderer.color = _normalColor;
                    break;

                case BuildingViewMode.Ghost:
                    if (_ghostVisual) _ghostVisual.SetActive(true);
                    if (_selectionHighlight) _selectionHighlight.SetActive(false);
                    if (_mainRenderer) _mainRenderer.color = _ghostOkColor;
                    break;

                case BuildingViewMode.Selected:
                    if (_ghostVisual) _ghostVisual.SetActive(false);
                    if (_selectionHighlight) _selectionHighlight.SetActive(true);
                    if (_mainRenderer) _mainRenderer.color = _normalColor;
                    break;

                case BuildingViewMode.Disabled:
                    if (_ghostVisual) _ghostVisual.SetActive(false);
                    if (_selectionHighlight) _selectionHighlight.SetActive(false);
                    if (_mainRenderer) _mainRenderer.color = Color.gray;
                    break;
            }
        }

        /// <summary>放置时检测不通过，可切换为红色 ghost 之类。</summary>
        public void SetGhostBlocked(bool blocked)
        {
            if (_mainRenderer)
                _mainRenderer.color = blocked ? _ghostBlockedColor : _ghostOkColor;
        }

        /// <summary>运行时替换精灵（如果你想从 Def 换 sprite）。</summary>
        public void SetSprite(Sprite sprite)
        {
            if (_mainRenderer && sprite != null)
                _mainRenderer.sprite = sprite;
        }
    }
}
