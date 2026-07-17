using System.Reflection;

namespace PrintService.Windows.Forms;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Print Service Windows - About";
        Width = 420;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";

        var title = new Label
        {
            Text = "Print Service Windows",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20),
        };

        var versionLabel = new Label
        {
            Text = $"Version: {version}",
            AutoSize = true,
            Location = new Point(20, 60),
        };

        var developerName = new Label
        {
            Text = "Created by: Mussab Muhaimeed",
            AutoSize = true,
            Location = new Point(20, 90),
        };

        var descriptionLabel = new Label
        {
            Text = "Local silent print service with system tray UI.",
            AutoSize = false,
            Location = new Point(20, 120),
            Size = new Size(360, 40),
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(160, 180),
            Width = 80,
        };
        AcceptButton = okButton;

        Controls.Add(title);
        Controls.Add(versionLabel);
        Controls.Add(developerName);
        Controls.Add(descriptionLabel);
        Controls.Add(okButton);
    }
}
