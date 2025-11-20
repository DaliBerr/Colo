using UnityEngine;
using UnityEngine.Tilemaps;
using Kernel.Building;
using Lonize.Logging;

public class BuildingColliderInit : MonoBehaviour
{
    [SerializeField]
    private BuildingRuntimeHost buildingRuntimeHost;

    [SerializeField]
    private Tilemap referenceTilemap;   // 可选：如果你想用 cellSize 来算物理尺寸

    private bool _initialized;

    // 允许从外部（Factory）显式调用
    public void Init()
    {
        Log.Info("[BuildingColliderInit] 初始化碰撞体");
        if (_initialized) return;

        // 自动 GetComponent，避免必须在 Inspector 里拖
        if (!buildingRuntimeHost)
            buildingRuntimeHost = GetComponent<BuildingRuntimeHost>();

        if (!buildingRuntimeHost)
        {
            Debug.LogError("[BuildingColliderInit] 没找到 BuildingRuntimeHost 组件", this);
            return;
        }

        var runtime = buildingRuntimeHost.Runtime;
        if (runtime == null)
        {
            Debug.LogError("[BuildingColliderInit] Runtime 为空（Factory 还没赋值？）", this);
            return;
        }

        var def = runtime.Def;
        if (def == null)
        {
            Debug.LogError("[BuildingColliderInit] Def 为空（BuildingDatabase/Factory 有问题）", this);
            return;
        }

        // 计算碰撞体大小
        Vector2 size;
        // Vector2 offset;

        if (referenceTilemap != null)
        {
            // Log.Info("[BuildingColliderInit] 使用 Tilemap cellSize 计算碰撞体尺寸");
            var cs = referenceTilemap.cellSize;
            size = new Vector2(def.Width * cs.x, def.Height * cs.y);
            // offset = new Vector2(size.x * 0.5f, size.y * 0.5f);
        }
        else
        {
            // 如果你世界坐标就是 1 单位 = 1 格，也可以直接用 width/height
            size = new Vector2(def.Width, def.Height);
            // offset = new Vector2(def.Width * 0.5f, def.Height * 0.5f);
        }
        Log.Info($"[BuildingColliderInit] 碰撞体尺寸：{size}");
        var collider = gameObject.GetComponent<BoxCollider2D>();
        collider.size = size;
        // collider.offset = offset;
        collider.isTrigger = true;

        _initialized = true;
    }
}
