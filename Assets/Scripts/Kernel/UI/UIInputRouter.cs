using System.Collections;
using Kernel;
using Kernel.GameState;
using Kernel.UI;
using Lonize.Logging;
using Lonize.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kernel.UI
{
    public sealed class UIInputRouter : MonoBehaviour
    {
        private bool isProcessingBack = false;
        // public KeyCode backKey = KeyCode.Escape;
        //TODO: 存在返回过程中重复点击后出现问题, 需要加锁防止多次触发
        void Update()
        {
            if (Input.GetKeyDown(InputConfiguration.ControlCommand["back"]) && !isProcessingBack)
            {
                isProcessingBack = true;
                if (UIManager.Instance)
                {
                    if (StatusController.HasStatus(StatusList.PopUpStatus))
                    {
                        
                        var top = UIManager.Instance.GetTopModal(true);
                        if (top is PopupModal popup)
                        {
                            StatusController.RemoveStatus(StatusList.PopUpStatus);
                            popup.CancelButtonAction();
                            StartCoroutine(ResetFlag());
                            return;
                        }
                    }
                    if (StatusController.HasStatus(StatusList.PlayingStatus))
                    {
                        StatusController.AddStatus(StatusList.InPauseMenuStatus);
                        // 在游戏中按返回键，先打开暂停菜单
                        UIManager.Instance.PushScreen<PauseMenuUI>();
                        
                        StartCoroutine(ResetFlag());
                        return;
                    }
                    if(StatusController.HasStatus(StatusList.InPauseMenuStatus))
                    {
                        StatusController.AddStatus(StatusList.PlayingStatus);
                        // 在菜单中按返回键，关闭当前屏幕
                        UIManager.Instance.PopScreen();
                        
                        StartCoroutine(ResetFlag());
                        return;
                    }
                    if(StatusController.HasStatus(StatusList.InMainMenuStatus))
                    {
                        // 在主菜单中按返回键，退出游戏
                        //TODO : 弹出确认对话框
                        // GameDebug.Log(UIManager.Instance.GetTopScreen(false)?.name ?? "No Screen MainCall");
                        // Application.Quit();
                        UIManager.Instance.ShowModal<QuitConfirmPopupModal>();
                        StartCoroutine(ResetFlag());
                        return;
                    }
                    else
                    {
                        StartCoroutine(CloseAndSync());
                        // GameDebug.Log("[UIInputRouter] Popping Screen");
                        // GameDebug.Log(UIManager.Instance.GetTopScreen(false)?.name ?? "No Screen BeforePop");
                        // UIManager.Instance.PopScreenAndWait();
                        // GameDebug.Log(UIManager.Instance.GetTopScreen(false)?.name ?? "No Screen AfterPop");
                        // StatusController.AddStatus(UIManager.Instance.GetTopScreen(false)?.currentStatus ?? StatusList.InMenuStatus);
                        StartCoroutine(ResetFlag());
                        return;
                    }
                }
            }
        }
        IEnumerator ResetFlag()
        {
            yield return null;
            yield return null;
            yield return null;
            isProcessingBack = false;
        }
        IEnumerator CloseAndSync()
        {
            yield return UIManager.Instance.PopScreenAndWait();
            var top = UIManager.Instance.GetTopScreen(true);
            StatusController.AddStatus(top?.currentStatus ?? StatusList.InMenuStatus);
            GameDebug.Log("[UIInputRouter] New Top Screen: " + (top?.name ?? "No Screen AfterPop"));
            
        }
    }
}