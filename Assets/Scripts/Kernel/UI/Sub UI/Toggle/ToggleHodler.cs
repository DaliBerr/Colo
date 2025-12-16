
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class ToggleHolder : MonoBehaviour
{
    public abstract Toggle _toggle { get; }

    protected virtual void Start()
    {
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        if (_toggle == null) return;
        _toggle.SetIsOnWithoutNotify(isOn);
    }

    public void onValueChanged(Action<bool> action)
    {
        if (_toggle == null) return;

        UnityAction<bool> unityAction = new UnityAction<bool>(action);
        _toggle.onValueChanged.AddListener(unityAction);
    }

    public void removeOnValueChanged(Action<bool> action)
    {
        if (_toggle == null) return;

        UnityAction<bool> unityAction = new UnityAction<bool>(action);
        _toggle.onValueChanged.RemoveListener(unityAction);
    }
}