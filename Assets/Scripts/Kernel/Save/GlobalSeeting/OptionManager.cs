using System.IO;
using UnityEngine;
using Lonize.Scribe;
using Lonize.Logging; // 假设你有这个日志工具

namespace Kernel{
public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }

    // ★★★ 核心：这里是你的设置数据实例 ★★★
    public GlobalSettings Settings = new GlobalSettings();

    // ★★★ 核心：指定一个不同的文件名 ★★★
    private string filePath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 设置文件路径，与 save.json 分开
        filePath = Path.Combine(Application.persistentDataPath, "settings.json");

        // 游戏启动时立刻加载设置
        LoadOptions();
    }

    public void SaveOptions()
    {
        try
        {
            // 1. 创建文件流 (Create/Overwrite)
            using (var fs = File.Create(filePath))
            {
                // 2. 初始化 Scribe (这是通用的，给它什么流它就写哪里)
                Scribe.InitSaving(fs);

                // 3. 写入你的设置对象
                // 注意：这里不需要 Items 列表，直接 Look 这个对象即可
                Scribe.Look(ref Settings);

                // 4. 结束写入
                Scribe.FinalizeWriting();
            }
            Log.Info("[Options] Settings saved to " + filePath);
        }
        catch (System.Exception ex)
        {
            Log.Error("[Options] Failed to save settings: " + ex);
            Scribe.FinalizeWriting(); // 确保状态复位
        }
    }

    public void LoadOptions()
    {
        if (!File.Exists(filePath))
        {
            Log.Warn("[Options] No settings file found, using defaults.");
            Settings = new GlobalSettings(); // 使用默认值
            SaveOptions(); // 生成一份默认文件
            return;
        }

        try
        {
            using (var fs = File.OpenRead(filePath))
            {
                Scribe.InitLoading(fs);
                
                // 读取数据到 Settings 变量中
                Scribe.Look(ref Settings);
                
                Scribe.FinalizeLoading();
            }
            
            ApplySettings(); // ★ 读取完立刻应用（如修改音量）
            Log.Info("[Options] Settings loaded.");
        }
        catch (System.Exception ex)
        {
            Log.Error("[Options] Failed to load settings: " + ex);
            Settings = new GlobalSettings(); // 出错则重置为默认
            Scribe.FinalizeLoading();
        }
    }

    // 应用设置的逻辑
    private void ApplySettings()
    {
        // 例子：应用分辨率
        Screen.fullScreen = Settings.FullScreen;
        
        // 例子：应用音量
        AudioListener.volume = Settings.MasterVolume;
        
        // 键位不需要“应用”，游戏逻辑直接访问 OptionsManager.Instance.Settings.KeyJump 即可
    }
}
}