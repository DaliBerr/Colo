using UnityEngine;
using UnityEngine.UI;
using Lonize.UI;
using Kernel.Status;
using UnityEngine.SceneManagement;

namespace Kernel.UI
{
    [UIPrefab("Prefabs/UI/MainMenuUI")]
    public sealed class MainMenuScreen : UIScreen
    {
        public Button startBtn, optionsBtn, quitBtn;

        protected override void OnInit()
        {
            startBtn.onClick.AddListener(() => TryStartGame());
            optionsBtn.onClick.AddListener(() => UIManager.Instance.PushScreen<OptionsModal>());
            quitBtn.onClick.AddListener(() => Application.Quit());
        }

        private void TryStartGame()
        {
            if(StatusController.HasStatus(StatusList.DevModeStatus))
            {
                // UIManager.Instance.PushScreen<MainUI>();
                SceneManager.LoadScene("Main");
            }
            else
            {
                UIManager.Instance.PushScreen<GameLoading>();
            }
        }

    }
}
