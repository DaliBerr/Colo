using Kernel;
using Lonize.Localization;
using Lonize.UI;
using UnityEngine;

[UIPrefab("Prefabs/UI/OptionPopup")]
public class OptionConfirmPopupModal : PopupModal
{
    public int CountDownSeconds = 10;
    private float countdownTimer;
    protected override void OnInit()
    {

        closeButton.onClick.AddListener(() =>
        {
            CancelButtonAction();
        });

        confirmButton.onClick.AddListener(() =>
        {
            ConfirmButtonAction();
        });
        SetCloseButtonText("Cancel changes".Translate());
        SetConfirmButtonText("Apply changes".Translate());

    }

    public void Start()
    {
        countdownTimer = CountDownSeconds;
    }
    public void Update()
    {
        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0f)
        {
            CancelButtonAction();
        }
        else
        {
            SetMessage($"Cancel  changes in ({Mathf.CeilToInt(countdownTimer)})s");
        }
    }

    private void CancelButtonAction()
    {
        UIManager.Instance.CloseTopModal();
        OptionsManager.Instance.CancelChanges();
    }

    private void ConfirmButtonAction()
    {
        UIManager.Instance.CloseTopModal();
        OptionsManager.Instance.ApplySettings();
    }
}