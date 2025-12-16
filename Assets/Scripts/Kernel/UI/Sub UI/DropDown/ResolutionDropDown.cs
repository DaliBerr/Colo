
using System.Collections.Generic;
using Kernel;
using Lonize.Events;
using TMPro;
using UnityEngine;

public class ResolutionDropDown : DropdownHolder
{
    public  TMP_Dropdown _dropdown;
    public Vector2Int prev = new Vector2Int(1920, 1080);
    public List<string> _options = new List<string>()
    {
        "1920 x 1080",
        "1600 x 900",
        "1366 x 768",
        "1280 x 720"
    };

    public override TMP_Dropdown dropdown => _dropdown;
    public override List<string> Options { get => _options; set => _options = value; }
    

    protected override void Start()
    {
        base.Start();
        prev = new Vector2Int(OptionsManager.Instance.Settings.Resolution.x, OptionsManager.Instance.Settings.Resolution.y);
        int defaultIndex = Options.IndexOf(prev.x + " x " + prev.y);
        if (defaultIndex < 0) defaultIndex = 0;
        SetOptions(Options, defaultIndex);
        onValueChanged(index =>
        {
            var res = Options[index].Split('x');
            if (res.Length == 2 &&
                int.TryParse(res[0].Trim(), out int width) &&
                int.TryParse(res[1].Trim(), out int height))
            {
                OptionsManager.Instance.Settings.Resolution = new Vector2Int(width, height);
                Events.eventBus.Publish(new SettingChanged(true));
            }
            //TODO:添加确认弹窗,并计时回退
        });
    }
}