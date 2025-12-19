

using System.Collections;
using Kernel.GameState;
using Unity.VisualScripting;
using UnityEngine;

namespace Lonize.UI
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class UIScreen : MonoBehaviour
    {
        [Header("Auto")]
        [SerializeField] protected CanvasGroup canvasGroup;

        public abstract Status currentStatus { get; }
        protected bool isVisible;
        protected UIManager ui;

        // 生命周期：子类可 override
        protected virtual void OnBeforeShow() { }
        protected virtual void OnAfterShow()  { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnAfterHide()  { }

        internal void __Init(UIManager manager)
        {
            ui = manager;
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isVisible = false;
            StatusController.AddStatus(currentStatus);
            OnInit();
        }

        // public void Start()
        // {
            
        //     HandleStart();
        // }
        // public abstract void HandleStart();
        // 子类做一次性初始化（抓引用/绑定按钮）
        protected virtual void OnInit() { }
        
        public virtual IEnumerator Show(float fade = 0.15f)
        {
            OnBeforeShow();
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            isVisible = true;

            if (fade > 0f)
            {
                float t = 0f;
                while (t < fade)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fade);
                    yield return null;
                }
            }
            canvasGroup.alpha = 1f;
            OnAfterShow();
        }

        public virtual IEnumerator Hide(float fade = 0.12f)
        {
            OnBeforeHide();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;

            if (fade > 0f)
            {
                float t = 0f;
                float start = canvasGroup.alpha;
                while (t < fade)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fade);
                    yield return null;
                }
            }
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            OnAfterHide();
        }

        public void setAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
        public float getAlpha()
        {
            float alpha = canvasGroup.alpha;
            return alpha;
        }
        
    }
}