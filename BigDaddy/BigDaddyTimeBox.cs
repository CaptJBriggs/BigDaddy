using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace BigDaddy
{
    // A text box that accepts only a six-digit HH:MM:SS time value.
    public class BigDaddyTimeBox : TextBox
    {
        // Stores the six digits without colons so they can be shifted left or right easily.
        private string timeDigits = "000000";

        public BigDaddyTimeBox()
        {
            Text = "00:00:00";
        }

        [DefaultValue("00:00:00")]
        public new string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;

                if (value.Length == 8 && value[2] == ':' && value[5] == ':')
                {
                    timeDigits = value.Replace(":", string.Empty);
                }
            }
        }

        // Handles keyboard input so the user never has to type colons manually.
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar))
            {
                timeDigits = timeDigits.Substring(1) + e.KeyChar;

                Text = $"{timeDigits.Substring(0, 2)}:" +
                       $"{timeDigits.Substring(2, 2)}:" +
                       $"{timeDigits.Substring(4, 2)}";

                SelectionStart = Text.Length;
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '\b')
            {
                timeDigits = "0" + timeDigits.Substring(0, 5);

                Text = $"{timeDigits.Substring(0, 2)}:" +
                       $"{timeDigits.Substring(2, 2)}:" +
                       $"{timeDigits.Substring(4, 2)}";

                SelectionStart = Text.Length;
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }
    }
}
