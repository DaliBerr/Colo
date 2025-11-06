using UnityEngine;
using UnityEngine.UI;
using Lonize.UI;

namespace Kernel.UI
{
    [UIPrefab("Prefabs/UI/MainMenuUI")]
    public sealed class MainMenuScreen : UIScreen
    {
        public Button startBtn, optionsBtn, quitBtn;

        protected override void OnInit()
        {
            // startBtn.onClick.AddListener(() => UIManager.Instance.PushScreen<GameLoading>());
            optionsBtn.onClick.AddListener(() => UIManager.Instance.PushScreen<OptionsModal>());
            quitBtn.onClick.AddListener(() => Application.Quit());
        }
    }
}
