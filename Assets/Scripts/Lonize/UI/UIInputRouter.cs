using Kernel;
using UnityEngine;

namespace Lonize.UI
{
    public sealed class UIInputRouter : MonoBehaviour
    {
        // public KeyCode backKey = KeyCode.Escape;

        void Update()
        {
            if (Input.GetKeyDown(InputConfiguration.ControlCommand["back"]))
            {
                // 先关顶层 Modal，否则 Pop Screen
                if (UIManager.Instance)
                {
                    // 这里简单判断：若有 Modal 就关 Modal；否则 Pop Screen
                    UIManager.Instance.CloseTop();
                    // （若希望只有当没有 Modal 时才 Pop，可在 UIManager 暴露“HasModal”属性）
                }
            }
        }
    }
}