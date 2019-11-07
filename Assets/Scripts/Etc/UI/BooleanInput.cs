using System;
using UnityEngine;
using UnityEngine.UI;

namespace Etc.UI
{
    /// <summary>
    /// Creates a boolean checkbox input in the main settings view.
    /// </summary>
    public class BooleanInput : MonoBehaviour
    {
        public Toggle toggle;
        public Text label;
        private Action<bool> _toggleAction;

        public BooleanInput Setup(Action<bool> action, string booleanName)
        {
            _toggleAction = action;
            label.text = booleanName;
            return this;
        }

        public void SetValue(bool value)
        {
            toggle.isOn = value;
            OnToggle(value);
        }
    
        public void OnToggle(bool isOn)
        {
            _toggleAction?.Invoke(isOn);
        }
    }
}