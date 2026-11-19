using System.Drawing;
using System.Windows.Forms;

namespace BigDaddy
{
    // Accepts a numeric PIN through mouse-clicked keypad buttons.
    internal class BigDaddyPinDialog : Form
    {
        private readonly TextBox pinTextBox = new TextBox();

        public string Pin => pinTextBox.Text;

        public BigDaddyPinDialog(string title, string prompt)
        {
            Text = title;
            ClientSize = new Size(280, 385);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            TopMost = true;

            Label promptLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 16),
                Size = new Size(248, 38),
                Text = prompt,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pinTextBox.Location = new Point(50, 62);
            pinTextBox.ReadOnly = true;
            pinTextBox.Size = new Size(180, 23);
            pinTextBox.TextAlign = HorizontalAlignment.Center;
            pinTextBox.UseSystemPasswordChar = true;

            Controls.Add(promptLabel);
            Controls.Add(pinTextBox);

            for (int digit = 1; digit <= 9; digit++)
            {
                int index = digit - 1;
                Button button = CreateKeyButton(digit.ToString(), 50 + (index % 3) * 60, 105 + (index / 3) * 52);
                Controls.Add(button);
            }

            Button zeroButton = CreateKeyButton("0", 110, 261);
            Controls.Add(zeroButton);

            Button deleteButton = new Button
            {
                Location = new Point(50, 313),
                Size = new Size(85, 26),
                Text = "Delete"
            };
            deleteButton.Click += DeleteButton_Click;

            Button submitButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(145, 313),
                Size = new Size(85, 26),
                Text = "Confirm"
            };

            Button cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(100, 347),
                Size = new Size(80, 23),
                Text = "Cancel"
            };

            Controls.Add(deleteButton);
            Controls.Add(submitButton);
            Controls.Add(cancelButton);

            Shown += BigDaddyPinDialog_Shown;
        }

        private Button CreateKeyButton(string digit, int left, int top)
        {
            Button button = new Button
            {
                Location = new Point(left, top),
                Size = new Size(50, 42),
                Text = digit
            };
            button.Click += DigitButton_Click;
            return button;
        }

        private void DigitButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button && pinTextBox.Text.Length < 32)
            {
                pinTextBox.Text += button.Text;
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (pinTextBox.Text.Length > 0)
            {
                pinTextBox.Text = pinTextBox.Text[..^1];
            }
        }

        // Ensures the keypad is visible above the topmost break overlay.
        private void BigDaddyPinDialog_Shown(object? sender, EventArgs e)
        {
            BringToFront();
            Activate();
        }
    }
}
