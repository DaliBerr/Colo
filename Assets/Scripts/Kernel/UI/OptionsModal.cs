using Lonize.Logging;
using Lonize.UI;
using UnityEngine.UI;

namespace Kernel.UI
{
    [UIPrefab("Prefabs/UI/OptionsModal")]
    public sealed class OptionsModal : UIScreen
    {
        public Button Btn1;

        protected override void OnInit()
        {
            // base.OnInit();
            Btn1.onClick.AddListener(() =>
            {
                Log.Info("Options Modal Button Clicked!");
            });
        }

    }



}