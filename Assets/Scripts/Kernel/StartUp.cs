using System.Collections;
using System.IO;
using Kernel.UI;
using Lonize.Logging;
using Lonize.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Kernel
{


    public sealed class Startup : MonoBehaviour
    {
        public static Startup Instance { get; private set; }
        private static readonly bool useDontDestroyOnLoad = true;
        [Header("系统模块加载开关")]
        [SerializeField] private bool isLoadMusicSystem = true;
        // [SerializeField] private bool isLoadSaveSystem = true;

        [SerializeField] private bool isLoadStartMenu = true;
        [SerializeField] private bool isLoadLocalizationSystem = true;
        [Header("系统模块预制体")]
        [SerializeField] private AssetReference musicSystemPrefab;

        public static class LoggingInit
        {
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            public static void Init()
            {
                Log.MinLevel = LogLevel.Debug;
                Log.AddSink(new FileSink(Path.Combine(Application.persistentDataPath, "Logs/game.log")));
#if UNITY_EDITOR
                Log.AddSink(new UnitySink());
#endif
                Log.Info("Log bootstrap ok (pid={0})", System.Diagnostics.Process.GetCurrentProcess().Id);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (useDontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        // 启动时显示主菜单
        IEnumerator Start()
        {
            // yield return null; // 等一帧，确保 UIRoot/Manager 就绪
            yield return StartCoroutine(Boot());
        }

        private IEnumerator Boot()
        {
            yield return StartCoroutine(InitGlobal());
            if (isLoadStartMenu)
            {
                UIManager.Instance.PushScreen<MainMenuScreen>();
                yield return null;
            }
            // yield return StartCoroutine(InitKeyBindings());
        }

        private IEnumerator InitGlobal()
        {
            yield return Addressables.InitializeAsync();
            if (isLoadMusicSystem && musicSystemPrefab != null)
            {
                var handle = musicSystemPrefab.InstantiateAsync();
                yield return handle;
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    if (useDontDestroyOnLoad)
                        DontDestroyOnLoad(handle.Result);
                }
                else
                {
                    // Debug.LogError("[Startup] 音乐系统预制体实例化失败！");
                    Log.Error("[Startup] Music system prefab instantiation failed!");
                }
            }
            yield return null;
            yield break;


        }
        // private IEnumerator InitKeyBindings()
        // {
            

        // }



    }

}