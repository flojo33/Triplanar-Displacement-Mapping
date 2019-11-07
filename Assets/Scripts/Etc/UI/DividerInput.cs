using UnityEngine;
using UnityEngine.UI;

namespace Etc.UI
{
    /// <summary>
    /// Creates a title divider in the settings view.
    /// </summary>
    public class DividerInput : MonoBehaviour
    {
        public Text nameText;

        public DividerInput Setup(string dividerName)
        {
            nameText.text = dividerName;
            return this;
        }
    }
}