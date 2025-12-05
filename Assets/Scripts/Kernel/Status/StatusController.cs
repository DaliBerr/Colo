using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Kernel.Status
{
    /// <summary>
    /// 状态控制器，负责管理和更新游戏中的各种状态信息。
    /// </summary>
    public static class StatusController
    {
        // 在这里添加状态管理的相关代码
        public static List<Status> CurrentStatus = new();
        /// <summary>
        /// 添加状态,如果存在互斥状态则添加失败
        /// </summary>
        /// <param name="status"> 要添加的状态 </param>
        /// <returns> 是否成功添加状态 </returns>
        public static bool AddStatus(Status status)
        {
            if (!CurrentStatus.Contains(status))
            {
                if(status.InActiveWith != null)
                {
                    foreach(var s in CurrentStatus)
                    {
                        if(status.InActiveWith.Contains(s.StatusName))
                        {
                            // CurrentStatus.Remove(s);
                            // CurrentStatus.Add(status);
                            // return true;
                            //存在互斥状态，不能添加
                            return false;
                        }
                    }
                }
                if(status.allowSwitchWith != null)
                {
                    foreach(var s in CurrentStatus)
                    {
                        if(status.allowSwitchWith.Contains(s.StatusName))
                        {
                            CurrentStatus.Remove(s);
                            break;
                        }
                    }
                }
                CurrentStatus.Add(status);
                return true;
            }
            return false;
        }
        public static void RemoveStatus(Status status)
        {
            if (CurrentStatus.Contains(status))
            {
                CurrentStatus.Remove(status);
            }
        }
        public static void ClearStatus()
        {
            CurrentStatus.Clear();
        }
        public static bool HasStatus(Status status)
        {
            return CurrentStatus.Contains(status);
        }
        // private static bool StatusCheck()
        // {
        //     //检查是否存在互斥状态
        //     return false;
        // }
    }


}