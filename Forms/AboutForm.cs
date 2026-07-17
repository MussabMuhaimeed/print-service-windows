using System.Reflection;

namespace PrintService.Windows.Forms;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Print Service Windows - About";
        ClientSize = new Size(420, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        try
        {
            Icon = LoadAppIcon();
        }
        catch
        {
            // keep default window icon if asset is missing
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        var logo = new PictureBox
        {
            Location = new Point(20, 20),
            Size = new Size(64, 64),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadAppLogoImage(),
        };

        var title = new Label
        {
            Text = "Print Service Windows",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(100, 24),
        };

        var versionLabel = new Label
        {
            Text = $"Version: {version}",
            AutoSize = true,
            Location = new Point(100, 56),
        };

        var developerName = new Label
        {
            Text = "Created by: Mussab Muhaimeed",
            AutoSize = true,
            Location = new Point(20, 104),
        };

        var descriptionLabel = new Label
        {
            Text = "Local silent print service with system tray UI.",
            AutoSize = false,
            Location = new Point(20, 136),
            Size = new Size(380, 40),
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(170, 220),
            Width = 80,
            Height = 28,
        };
        okButton.Click += (_, _) => Close();
        AcceptButton = okButton;
        CancelButton = okButton;

        Controls.Add(logo);
        Controls.Add(title);
        Controls.Add(versionLabel);
        Controls.Add(developerName);
        Controls.Add(descriptionLabel);
        Controls.Add(okButton);
    }

    private static Icon LoadAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(path))
        {
            return new Icon(path);
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    private static Image? LoadAppLogoImage()
    {
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-logo.png");
        if (File.Exists(pngPath))
        {
            return Image.FromFile(pngPath);
        }

        var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(icoPath))
        {
            using var icon = new Icon(icoPath, 64, 64);
            return icon.ToBitmap();
        }

        return SystemIcons.Application.ToBitmap();
    }
}
