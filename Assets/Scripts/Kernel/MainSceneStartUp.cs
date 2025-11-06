
using System;
using System.Threading.Tasks;
using Kernel.Item;
using Lonize.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Kernel
{
    public sealed class MainSceneStartUp : MonoBehaviour
    {
        public static MainSceneStartUp Instance { get; private set; }

        private static readonly bool useDontDestroyOnLoad = true;
        async void Awake()
        {
            // Addressables.InitializeAsync().Task;
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (useDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            await InitItems();
        }

        private async Task InitItems()
        {
            await ItemDatabase.LoadAllAsync();

            //test
            var inst = ItemFactory.CreateData("iron_sword", 5);
            Log.Info($"Created Item Instance: Def={inst.Def.Id}, Stack={inst.Stack}");

        }
    }
}