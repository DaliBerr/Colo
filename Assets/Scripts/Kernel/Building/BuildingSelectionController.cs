using Lonize.Events;
using Lonize.Logging;
using UnityEngine;

namespace Kernel.Building
{   
    /// <summary>
    /// 建筑选中控制器：负责处理玩家点击建筑以选中/取消选中建筑的逻辑
    /// </summary>
    public class BuildingSelectionController : MonoBehaviour
    {
        public Camera mainCamera;
        public LayerMask buildingLayerMask;

        // 当前选中的建筑 ID（从 Runtime.BuildingID 来）
        private long _selectedBuildingID = -1;

        // 当前选中的建筑运行时 & 视图
        private BuildingRuntimeHost _selectedBuildingRuntimeHost;
        private BuildingView _selectedBuildingView;

        private void Update()
        {
            CheckBuildingSelected();
        }

        public void CheckBuildingSelected()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            // 从鼠标位置获取世界坐标
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 pos2D = worldPos;

            // 用 OverlapPoint 检测当前鼠标下有没有建筑的碰撞体
            // buildingLayerMask 里只放“建筑”的 Layer 就行
            Collider2D hitCollider = Physics2D.OverlapPoint(pos2D, buildingLayerMask);

            // 点到某个碰撞体
            if (hitCollider != null)
            {
                // 兼容“碰撞体在子物体上”的情况，所以用 GetComponentInParent
                BuildingRuntimeHost hitRuntimeHost = hitCollider.GetComponentInParent<BuildingRuntimeHost>();
                BuildingView hitView = hitCollider.GetComponentInParent<BuildingView>();

                // 碰到的不是建筑（没有 RuntimeHost / View），就当点到空地来处理
                if (hitRuntimeHost == null || hitView == null)
                {
                    // 如果本来有选中的，点到非建筑就清空选中
                    if (_selectedBuildingID != -1)
                    {
                        ClearSelection();
                    }
                    return;
                }

                long hitId = (hitRuntimeHost.Runtime != null) ? hitRuntimeHost.Runtime.BuildingID : -1;

                Log.Info("[BuildingSelectionController] Clicked building id: " + hitId + ", current selected: " + _selectedBuildingID);

                // 再次点击同一建筑 → 取消选中
                if (_selectedBuildingID == hitId)
                {
                    ClearSelection();
                }
                else
                {
                    // 先把旧的选中取消
                    if (_selectedBuildingView != null)
                    {
                        _selectedBuildingView.SetMode(BuildingViewMode.Normal);
                    }

                    // 更新当前选中
                    _selectedBuildingRuntimeHost = hitRuntimeHost;
                    _selectedBuildingView = hitView;
                    _selectedBuildingID = hitId;

                    // 设置为选中模式
                    _selectedBuildingView.SetMode(BuildingViewMode.Selected);

                    // 广播选中事件
                    Events.eventBus.Publish(new BuildingSelected
                    {
                        buildingRuntime = _selectedBuildingRuntimeHost.Runtime,
                        isSelected = true
                    });

                    Log.Info("[BuildingSelectionController] Selected building id: " + _selectedBuildingID);
                }
            }
            else
            {
                // hitCollider == null → 点到空地
                if (_selectedBuildingID != -1)
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// 清空当前选中状态（包括视图 + 事件）
        /// </summary>
        private void ClearSelection()
        {
            // 先把视图恢复成 Normal
            if (_selectedBuildingView != null)
            {
                _selectedBuildingView.SetMode(BuildingViewMode.Normal);
            }

            // 发送“没有建筑被选中”的事件
            Events.eventBus.Publish(new BuildingSelected
            {
                buildingRuntime = null,
                isSelected = false
            });

            Log.Info("[BuildingSelectionController] Clear selection. Previous id: " + _selectedBuildingID);

            // 清掉内部状态
            _selectedBuildingID = -1;
            _selectedBuildingRuntimeHost = null;
            _selectedBuildingView = null;
        }
    }   
}
