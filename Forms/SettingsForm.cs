using PrintService.Windows.Config;
using PrintService.Windows.Http;
using PrintService.Windows.Models;

namespace PrintService.Windows.Forms;

public sealed class SettingsForm : Form
{
    private readonly SettingsRepository _settingsRepository;
    private readonly PrintHttpServer _server;
    private readonly TextBox _defaultPrinterBox = new();
    private readonly TextBox _defaultPaperBox = new();
    private readonly NumericUpDown _portBox = new();
    private readonly TextBox _originsBox = new();

    public SettingsForm(SettingsRepository settingsRepository, PrintHttpServer server)
    {
        _settingsRepository = settingsRepository;
        _server = server;

        Text = "Print Service Windows - Settings";
        Width = 500;
        Height = 380;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var settings = settingsRepository.Get();

        AddLabel("Default printer (empty = system default)", 20, 20);
        _defaultPrinterBox.Location = new Point(20, 45);
        _defaultPrinterBox.Width = 440;
        _defaultPrinterBox.Text = settings.DefaultPrinter;

        AddLabel("Default paper (e.g. 100x150, A4)", 20, 80);
        _defaultPaperBox.Location = new Point(20, 105);
        _defaultPaperBox.Width = 200;
        _defaultPaperBox.Text = settings.DefaultPaper;

        AddLabel("HTTP port (restart required)", 20, 140);
        _portBox.Location = new Point(20, 165);
        _portBox.Width = 120;
        _portBox.Minimum = 1024;
        _portBox.Maximum = 65535;
        _portBox.Value = settings.Port;

        AddLabel("Allowed CORS origins (one per line)", 20, 200);
        _originsBox.Location = new Point(20, 225);
        _originsBox.Width = 440;
        _originsBox.Height = 80;
        _originsBox.Multiline = true;
        _originsBox.Text = string.Join(Environment.NewLine, settings.AllowedOrigins);

        var saveButton = new Button { Text = "Save", Location = new Point(20, 320), Width = 100 };
        saveButton.Click += (_, _) => SaveSettings();

        var note = new Label
        {
            Text = "Port changes apply after restart (Stop then Start from tray menu).",
            AutoSize = true,
            Location = new Point(130, 324),
            ForeColor = Color.Gray,
        };

        Controls.Add(_defaultPrinterBox);
        Controls.Add(_defaultPaperBox);
        Controls.Add(_portBox);
        Controls.Add(_originsBox);
        Controls.Add(saveButton);
        Controls.Add(note);
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y) });
    }

    private void SaveSettings()
    {
        var origins = _originsBox.Text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (origins.Count == 0)
        {
            origins = ["http://localhost", "http://127.0.0.1"];
        }

        var newSettings = new AppSettings
        {
            DefaultPrinter = _defaultPrinterBox.Text.Trim(),
            DefaultPaper = _defaultPaperBox.Text.Trim(),
            Port = (int)_portBox.Value,
            AllowedOrigins = origins,
        };

        var portChanged = newSettings.Port != _settingsRepository.Get().Port;
        _settingsRepository.Save(newSettings);

        MessageBox.Show(
            portChanged
                ? "Settings saved. Stop and start the service from the tray menu to apply the new port."
                : "Settings saved.",
            "Print Service Windows",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
