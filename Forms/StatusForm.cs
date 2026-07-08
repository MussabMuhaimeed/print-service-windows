using PrintService.Windows.Http;
using PrintService.Windows.Logging;

namespace PrintService.Windows.Forms;

public sealed class StatusForm : Form
{
    private readonly PrintHttpServer _server;
    private readonly FileLogger _logger;
    private readonly Label _statusLabel = new();
    private readonly Label _portLabel = new();
    private readonly Label _urlLabel = new();
    private readonly Label _logPathLabel = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public StatusForm(PrintHttpServer server, FileLogger logger)
    {
        _server = server;
        _logger = logger;

        Text = "Print Service Windows - Status";
        Width = 480;
        Height = 280;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var title = new Label
        {
            Text = "Print Service Windows",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20),
        };

        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(20, 60);
        _statusLabel.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);

        _portLabel.AutoSize = true;
        _portLabel.Location = new Point(20, 95);

        _urlLabel.AutoSize = true;
        _urlLabel.Location = new Point(20, 120);

        _logPathLabel.AutoSize = false;
        _logPathLabel.Location = new Point(20, 155);
        _logPathLabel.Size = new Size(420, 40);

        var refreshButton = new Button
        {
            Text = "Refresh",
            Location = new Point(20, 210),
            Width = 100,
        };
        refreshButton.Click += (_, _) => RefreshStatus();

        Controls.Add(title);
        Controls.Add(_statusLabel);
        Controls.Add(_portLabel);
        Controls.Add(_urlLabel);
        Controls.Add(_logPathLabel);
        Controls.Add(refreshButton);

        _timer.Interval = 2000;
        _timer.Tick += (_, _) => RefreshStatus();
        _timer.Start();

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var running = _server.IsRunning;
        _statusLabel.Text = running ? "Status: Running" : "Status: Stopped";
        _statusLabel.ForeColor = running ? Color.Green : Color.Red;
        _portLabel.Text = $"Port: {_server.Port}";
        _urlLabel.Text = $"URL: http://127.0.0.1:{_server.Port}";
        _logPathLabel.Text = $"Log file: {_logger.LogFilePath}";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosed(e);
    }
}
