using Lonize.Events;
using UnityEngine;
namespace Kernel.Building
{   
public class BuildingSelectionClearer : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask buildingLayerMask;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 从鼠标发射一条射线，看看有没有打到建筑
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 pos2D = worldPos;

            RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero, 0f, buildingLayerMask);

            if (hit.collider == null)
            {
                // 没点到建筑 → 清空选中
                Events.eventBus.Publish(new BuildingSelected { buildingRuntime = null , isSelected = false });
            }
        }
    }
}   
}