using Kernel;
using Kernel.Building;
using UnityEngine;

namespace Colo
{
    public class GameSaveController : MonoBehaviour
    {
        public static GameSaveController Instance;
        public void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        public void Update()
        {
            if(Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }
            if(Input.GetKeyDown(KeyCode.F6))
            {
                LoadGame();
            }
        }

        // 在这里添加游戏存档管理的相关代码
        public void SaveGame()
        {
            var saveMgr = ScribeSaveManager.Instance;
        
        // 1. 【关键】先清空，否则数据会无限堆叠
        saveMgr.Data.Items.Clear();

        // 2. 收集玩家数据（举例）
        // var player = FindObjectOfType<PlayerController>();
        // var savePlayer = new SaveInt(); // 假设你用 SaveInt 存血量作为演示
        // savePlayer.Value = player.HP;
        // saveMgr.AddItem(savePlayer);

        // 3. 收集所有建筑数据
        var allBuildings = FindObjectsByType<BuildingView>(FindObjectsSortMode.None);
        
        
        foreach (var build in allBuildings)
        {

            /* var saveBuild = new SaveBuilding();
            saveBuild.Position = build.transform.position;
            saveBuild.TypeId = build.BuildingTypeId;
            saveMgr.AddItem(saveBuild);
            */
        }
        // Debug.Log($"Collected {allBuildings.Length} buildings for saving.");
        // 添加状态数据保存项
        saveMgr.AddItem(new Kernel.Status.StatusSaveData());
        // Debug.Log("saveItem : " + BuildingIdGenerator._saveItem);

        saveMgr.AddItem(BuildingIdGenerator._saveItem);
        // 4. 落盘
        saveMgr.Save();
        Debug.Log("游戏已保存！");

        }

        
    public void LoadGame()
    {
        var saveMgr = ScribeSaveManager.Instance;

        // 1. 读文件
        if (!saveMgr.Load())
        {
            Debug.LogWarning("没有存档文件！");
            return;
        }

        // 2. 清理场景（把旧的建筑删了，准备生成新的）
        var oldBuildings = FindObjectsByType<BuildingView>(FindObjectsSortMode.None);
        foreach (var b in oldBuildings) Destroy(b.gameObject);

        // 3. 还原数据
        foreach (var item in saveMgr.Data.Items)
        {
            // 识别这是什么类型的数据
            if (item is SaveInt playerHp) // C# 模式匹配
            {
                // FindObjectOfType<PlayerController>().HP = playerHp.Value;
            }
            // else if (item is SaveBuilding buildData)
            // {
            //     // 重新生成物体
            //     var newObj = Instantiate(buildingPrefab, buildData.Position, Quaternion.identity);
            //     newObj.GetComponent<BuildingView>().Init(buildData);
            // }
        }
        
        Debug.Log("游戏读取完毕！");
    }
    }
}