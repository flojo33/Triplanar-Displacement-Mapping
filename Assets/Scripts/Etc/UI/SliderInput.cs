using System;
using UnityEngine;
using UnityEngine.UI;

namespace Etc.UI
{
    /// <summary>
    /// Creates a slier Input in the settings view.
    /// </summary>
    public class SliderInput : MonoBehaviour
    {
        public Slider slider;
        public Text label, minLabel, maxLabel, currentLabel;
        private Action<float> _changeAction;

        public SliderInput Setup(Action<float> action, string sliderName, float min, float max)
        {
            slider.minValue = min;
            slider.maxValue = max;
            minLabel.text = min.ToString("F");
            maxLabel.text = max.ToString("F");
            currentLabel.text = slider.value.ToString("F");
            _changeAction = action;
            label.text = sliderName;
            return this;
        }

        public void SetValue(float value)
        {
            slider.value = value;
            OnValueChanged(value);
        }
    
        public void OnValueChanged(float value)
        {
            _changeAction?.Invoke(value);
            currentLabel.text = slider.value.ToString("F");
        }
    }
}