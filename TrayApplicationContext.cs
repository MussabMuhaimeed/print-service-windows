using PrintService.Windows.Config;
using PrintService.Windows.Forms;
using PrintService.Windows.Http;
using PrintService.Windows.Logging;
using PrintService.Windows.Services;

namespace PrintService.Windows;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly PrintHttpServer _server;
    private readonly FileLogger _logger;
    private readonly SettingsRepository _settings;
    private readonly HtmlPrintService _htmlPrintService;

    private StatusForm? _statusForm;
    private LogsForm? _logsForm;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext(
        PrintHttpServer server,
        FileLogger logger,
        SettingsRepository settings,
        HtmlPrintService htmlPrintService)
    {
        _server = server;
        _logger = logger;
        _settings = settings;
        _htmlPrintService = htmlPrintService;

        _trayIcon = new NotifyIcon
        {
            Text = "Print Service Windows",
            Visible = true,
            Icon = SystemIcons.Application,
        };

        _trayIcon.ContextMenuStrip = BuildMenu();
        _trayIcon.DoubleClick += (_, _) => ShowStatus();

        _server.RunningStateChanged += _ => UpdateTrayText();
        StartService();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Status");
        statusItem.Click += (_, _) => ShowStatus();

        var logsItem = new ToolStripMenuItem("View Logs");
        logsItem.Click += (_, _) => ShowLogs();

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettings();

        var startItem = new ToolStripMenuItem("Start Service");
        startItem.Click += (_, _) => StartService();

        var stopItem = new ToolStripMenuItem("Stop Service");
        stopItem.Click += (_, _) => StopService();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApplication();

        menu.Opening += (_, _) =>
        {
            startItem.Enabled = !_server.IsRunning;
            stopItem.Enabled = _server.IsRunning;
        };

        menu.Items.Add(statusItem);
        menu.Items.Add(logsItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void StartService()
    {
        try
        {
            _settings.Load();
            _server.Start();
            UpdateTrayText();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start service", ex);
            MessageBox.Show(
                $"Could not start print service on port {_settings.Get().Port}.\n\n{ex.Message}",
                "Print Service Windows",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void StopService()
    {
        _server.Stop();
        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        var port = _settings.Get().Port;
        _trayIcon.Text = _server.IsRunning
            ? $"Print Service Windows - Running (:{port})"
            : "Print Service Windows - Stopped";
    }

    private void ShowStatus()
    {
        if (_statusForm is null || _statusForm.IsDisposed)
        {
            _statusForm = new StatusForm(_server, _logger);
        }

        _statusForm.Show();
        _statusForm.BringToFront();
        _statusForm.WindowState = FormWindowState.Normal;
    }

    private void ShowLogs()
    {
        if (_logsForm is null || _logsForm.IsDisposed)
        {
            _logsForm = new LogsForm(_logger);
        }

        _logsForm.Show();
        _logsForm.BringToFront();
        _logsForm.WindowState = FormWindowState.Normal;
    }

    private void ShowSettings()
    {
        if (_settingsForm is null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settings, _server);
        }

        _settingsForm.Show();
        _settingsForm.BringToFront();
        _settingsForm.WindowState = FormWindowState.Normal;
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        _server.Dispose();
        _htmlPrintService.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
