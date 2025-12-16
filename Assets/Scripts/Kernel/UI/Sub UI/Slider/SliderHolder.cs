
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class SliderHolder : MonoBehaviour
{
    public abstract Slider _slider { get; }

    protected virtual void Start()
    {
    }

    public void SetValueWithoutNotify(float value)
    {
        if (_slider == null) return;
        _slider.SetValueWithoutNotify(value);
    }

    public void onValueChanged(Action<float> action)
    {
        if (_slider == null) return;

        UnityAction<float> unityAction = new UnityAction<float>(action);
        _slider.onValueChanged.AddListener(unityAction);
    }

    public void removeOnValueChanged(Action<float> action)
    {
        if (_slider == null) return;

        UnityAction<float> unityAction = new UnityAction<float>(action);
        _slider.onValueChanged.RemoveListener(unityAction);
    }
}