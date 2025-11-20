

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Lonize.Logging;


namespace Lonize.UI
{
    public enum UILayer { Screen, Modal, Overlay, Toast }

    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Roots")]
        public Canvas rootCanvas;
        public RectTransform layerScreen;
        public RectTransform layerModal;
        public RectTransform layerOverlay;
        public RectTransform layerToast;

        [Header("Defaults")]
        public float defaultShow = 0.15f;
        public float defaultHide = 0.12f;

        readonly Stack<UIScreen> screenStack = new();
        readonly Stack<UIScreen> modalStack = new();
        readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> addrInstances = new();

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // if (!rootCanvas) Debug.LogError("[UI] rootCanvas NOT set!");
            // if (!layerScreen) Debug.LogError("[UI] layerScreen NOT set!");
            // if (!layerModal)  Debug.LogError("[UI] layerModal NOT set!");
            // if (!layerOverlay)Debug.LogError("[UI] layerOverlay NOT set!");
            // if (!layerToast)  Debug.LogError("[UI] layerToast NOT set!");
            // 防呆：确保有 EventSystem
            if (!FindAnyObjectByType<EventSystem>())
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(go);
            }
        }

        // --------- 公共 API ---------
        public void PushScreen<T>() where T : UIScreen
        {
            StartCoroutine(PushScreenCo<T>());
        }
        public void PopScreen()
        {
            if (screenStack.Count == 0) return;
            StartCoroutine(PopScreenCo());
        }
        public void ShowModal<T>() where T : UIScreen
        {
            StartCoroutine(ShowModalCo<T>());
        }
        public void CloseTopModal()
        {
            if (modalStack.Count == 0) return;
            StartCoroutine(DestroyAfterHide(modalStack.Pop()));
        }

        public void CloseTop()
        {
            if (modalStack.Count == 0)
            {
                if (screenStack.Count > 1)
                    PopScreen();
                else
                {
                    // switch()
                }
            }
            else
            {
                // if(screenStack.Count == 1) return;
                CloseTopModal();
            }
        }
        // public T PushScreen<T>() where T : UIScreen
        // {
        //     var screen = Create<T>(UILayer.Screen);
        //     if (screenStack.Count > 0) StartCoroutine(screenStack.Peek().Hide(defaultHide));
        //     screenStack.Push(screen);
        //     StartCoroutine(screen.Show(defaultShow));
        //     return screen;
        // }

        // public void PopScreen()
        // {
        //     if (screenStack.Count == 0) return;
        //     var top = screenStack.Pop();
        //     StartCoroutine(DestroyAfterHide(top));

        //     if (screenStack.Count > 0)
        //         StartCoroutine(screenStack.Peek().Show(defaultShow));
        // }

        // public T ShowModal<T>() where T : UIScreen
        // {
        //     var modal = Create<T>(UILayer.Modal);
        //     modalStack.Push(modal);
        //     StartCoroutine(modal.Show(defaultShow));
        //     return modal;
        // }

        // public void CloseTopModal()
        // {
        //     if (modalStack.Count == 0) return;
        //     var m = modalStack.Pop();
        //     StartCoroutine(DestroyAfterHide(m));
        // }
        public T ShowOverlayImmediate<T>(T existing) where T : UIScreen
        {
            // 可选：复用场景里的现成 Overlay（非 Addressables）
            existing.transform.SetParent(layerOverlay, false);
            existing.__Init(this);
            StartCoroutine(existing.Show(0f));
            return existing;
        }
        // public T ShowOverlay<T>() where T : UIScreen
        // {
        //     var ov = Create<T>(UILayer.Overlay);
        //     StartCoroutine(ov.Show(0f));
        //     return ov;
        // }

        public void HideAndDestroy(UIScreen s) => StartCoroutine(DestroyAfterHide(s));

        // --------- 内部：加载/实例化 ---------
        IEnumerator PushScreenCo<T>() where T : UIScreen
        {
            if (screenStack.Count > 0)
                yield return screenStack.Peek().Hide(defaultHide);

            T screen = null;
            yield return CreateScreenCo<T>(UILayer.Screen, s => screen = s);
            if (screen == null) yield break;

            screenStack.Push(screen);
            yield return screen.Show(defaultShow);
        }

        IEnumerator PopScreenCo()
        {
            var top = screenStack.Pop();
            yield return DestroyAfterHide(top);

            if (screenStack.Count > 0)
                yield return screenStack.Peek().Show(defaultShow);
        }

        IEnumerator ShowModalCo<T>() where T : UIScreen
        {
            T modal = null;
            yield return CreateScreenCo<T>(UILayer.Modal, m => modal = m);
            if (modal == null) yield break;

            modalStack.Push(modal);
            yield return modal.Show(defaultShow);
        }

        IEnumerator CreateScreenCo<T>(UILayer layer, Action<T> onReady) where T : UIScreen
        {
            var address = GetPrefabAddress(typeof(T));
            var parent = GetLayer(layer) ?? (RectTransform)rootCanvas.transform;

            var handle = AddressableInstantiate(address, parent);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error($"[UI] Addressables instantiate failed: {address}");
                yield break;
            }

            var go = handle.Result;
            addrInstances[go] = handle; // 记录句柄，销毁时释放
            var screen = go.GetComponent<T>() ?? go.AddComponent<T>();

            screen.__Init(this);
            NormalizeRect(go.transform as RectTransform);
            onReady?.Invoke(screen);
        }
        static AsyncOperationHandle<GameObject> AddressableInstantiate(string address, Transform parent)
        {
            // 直接实例化到父节点下，避免世界坐标错乱
            return Addressables.InstantiateAsync(address, parent);
        }

        string GetPrefabAddress(Type t)
        {
            var attr = t.GetCustomAttribute<UIPrefabAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.Path)) return attr.Path;
            // 没写特性就用类名作为地址（确保你在 Addressables 里也这么配）
            return $"UI/{t.Name}";
        }

        // T Create<T>(UILayer layer) where T : UIScreen
        // {
        //     var path = GetPrefabPath(typeof(T));
        //     var prefab = Resources.Load<GameObject>(path);
        //     if (!prefab) throw new Exception($"UIPrefab not found at: {path}");

        //     var parent = GetLayer(layer);
        //     if (parent == null) {
        //         Debug.LogError($"[UI] Target layer {layer} is not assigned. Falling back to rootCanvas!");
        //         parent = (RectTransform)rootCanvas.transform; // 兜底：挂到整个 Canvas 下
        //     }

        //     // ——关键点：不要用不带 parent 的 Instantiate，也不要传 worldPositionStays=true——
        //     var go = Instantiate(prefab);                     // 先裸实例化
        //     go.transform.SetParent(parent, worldPositionStays: false); // 再强制设父节点
        //     var screen = go.GetComponent<T>() ?? go.AddComponent<T>();
        //     screen.__Init(this);
        //     NormalizeRect(go.transform as RectTransform);
        //     return screen;
        // }
        // T Create<T>(UILayer layer) where T : UIScreen
        // {
        //     var path = GetPrefabPath(typeof(T));
        //     var prefab = Resources.Load<GameObject>(path);
        //     if (!prefab) throw new Exception($"UIPrefab not found at: {path}");

        //     var parent = GetLayer(layer);
        //     var go = Instantiate(prefab, parent, worldPositionStays:false);
        //     var screen = go.GetComponent<T>();
        //     if (!screen) screen = go.AddComponent<T>(); // 防呆
        //     screen.__Init(this);
        //     NormalizeRect(go.transform as RectTransform);
        //     return screen;
        // }
        RectTransform GetLayer(UILayer layer) => layer switch
        {
            UILayer.Screen => layerScreen,
            UILayer.Modal => layerModal,
            UILayer.Overlay => layerOverlay,
            UILayer.Toast => layerToast,
            _ => layerScreen
        };
        // string GetPrefabPath(Type t)
        // {
        //     var attr = t.GetCustomAttribute<UIPrefabAttribute>();
        //     if (attr != null && !string.IsNullOrEmpty(attr.Path)) return attr.Path;
        //     // 约定：如果没写 Attribute，则走默认路径
        //     return $"UI/{t.Name}";
        // }

        // RectTransform GetLayer(UILayer layer) => layer switch
        // {
        //     UILayer.Screen  => layerScreen,
        //     UILayer.Modal   => layerModal,
        //     UILayer.Overlay => layerOverlay,
        //     UILayer.Toast   => layerToast,
        //     _ => layerScreen
        // };

        IEnumerator DestroyAfterHide(UIScreen s)
        {
            yield return s.Hide(defaultHide);
            if (s) Destroy(s.gameObject);
        }

        static void NormalizeRect(RectTransform rt)
        {
            if (!rt) return;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}