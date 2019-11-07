using System;
using UnityEngine;
using UnityEngine.UI;

namespace Etc.UI
{
    /// <summary>
    /// Creates a button Input in the settings view.
    /// </summary>
    public class ButtonInput : MonoBehaviour
    {
        public Text label;
        private Action _pressAction;

        public ButtonInput Setup(Action action, string buttonName)
        {
            _pressAction = action;
            label.text = buttonName;
            return this;
        }

        public void OnPress()
        {
            _pressAction?.Invoke();
        }
    }
}