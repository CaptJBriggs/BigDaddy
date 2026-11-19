using System.Drawing;
using System.Windows.Forms;

namespace BigDaddy
{
    // Lets the user optionally replace a break overlay's default message.
    internal class BigDaddyBreakMessageDialog : Form
    {
        private readonly TextBox messageTextBox = new TextBox();

        public string MessageText => messageTextBox.Text.Trim();

        public BigDaddyBreakMessageDialog(string breakName, string currentMessage)
        {
            Text = $"{breakName} Message";
            ClientSize = new Size(420, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            Label instructions = new Label
            {
                AutoSize = true,
                Location = new Point(16, 16),
                Text = "Optional break message (leave blank for the default):"
            };

            messageTextBox.Location = new Point(16, 42);
            messageTextBox.Multiline = true;
            messageTextBox.MaxLength = 500;
            messageTextBox.ScrollBars = ScrollBars.Vertical;
            messageTextBox.Size = new Size(388, 145);
            messageTextBox.Text = currentMessage;

            Button saveButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(248, 205),
                Size = new Size(75, 23),
                Text = "Save"
            };

            Button cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(329, 205),
                Size = new Size(75, 23),
                Text = "Cancel"
            };

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            Controls.Add(instructions);
            Controls.Add(messageTextBox);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);
        }
    }
}
