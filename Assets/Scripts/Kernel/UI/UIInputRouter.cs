using System.Collections;
using Kernel;
using Kernel.GameState;
using Kernel.UI;
using Lonize.Logging;
using Lonize.UI;
using UnityEngine;

namespace Kernel.UI
{
    public sealed class UIInputRouter : MonoBehaviour
    {
        // public KeyCode backKey = KeyCode.Escape;

        void Update()
        {
            if (Input.GetKeyDown(InputConfiguration.ControlCommand["back"]))
            {
                if (UIManager.Instance)
                {
                    if (StatusController.HasStatus(StatusList.PlayingStatus))
                    {
                        // 在游戏中按返回键，先打开暂停菜单
                        UIManager.Instance.PushScreen<PauseMenuUI>();
                        StatusController.AddStatus(StatusList.InPauseMenuStatus);
                        return;
                    }
                    if(StatusController.HasStatus(StatusList.InPauseMenuStatus))
                    {
                        // 在菜单中按返回键，关闭当前屏幕
                        UIManager.Instance.PopScreen();
                        StatusController.AddStatus(StatusList.PlayingStatus);
                        return;
                    }
                    if(StatusController.HasStatus(StatusList.InMainMenuStatus))
                    {
                        // 在主菜单中按返回键，退出游戏
                        //TODO : 弹出确认对话框
                        // GameDebug.Log(UIManager.Instance.GetTopScreen(false)?.name ?? "No Screen MainCall");
                        Application.Quit();
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
                    }
                }
            }
        }
        IEnumerator CloseAndSync()
        {
            yield return UIManager.Instance.PopScreenAndWait();
            var top = UIManager.Instance.GetTopScreen(true);
            GameDebug.Log("[UIInputRouter] New Top Screen: " + (top?.name ?? "No Screen AfterPop"));
            StatusController.AddStatus(top?.currentStatus ?? StatusList.InMenuStatus);
        }
    }
}