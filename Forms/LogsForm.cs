using PrintService.Windows.Logging;

namespace PrintService.Windows.Forms;

public sealed class LogsForm : Form
{
    private readonly FileLogger _logger;
    private readonly TextBox _logBox = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public LogsForm(FileLogger logger)
    {
        _logger = logger;

        Text = "Print Service Windows - Logs";
        Width = 700;
        Height = 500;
        StartPosition = FormStartPosition.CenterScreen;

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Both;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9);
        _logBox.WordWrap = false;

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        var openButton = new Button { Text = "Open log file", Width = 120, Location = new Point(10, 8) };
        openButton.Click += (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _logger.LogFilePath,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var clearButton = new Button { Text = "Refresh", Width = 100, Location = new Point(140, 8) };
        clearButton.Click += (_, _) => LoadLogs();

        panel.Controls.Add(openButton);
        panel.Controls.Add(clearButton);

        Controls.Add(_logBox);
        Controls.Add(panel);

        _logger.LogAdded += OnLogAdded;
        _timer.Interval = 3000;
        _timer.Tick += (_, _) => LoadLogs();
        _timer.Start();

        LoadLogs();
    }

    private void OnLogAdded(string line)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            _logBox.AppendText(line + Environment.NewLine);
        });
    }

    private void LoadLogs()
    {
        var logs = _logger.GetRecentLogs();
        _logBox.Text = string.Join(Environment.NewLine, logs);
        _logBox.SelectionStart = _logBox.Text.Length;
        _logBox.ScrollToCaret();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logger.LogAdded -= OnLogAdded;
        _timer.Stop();
        base.OnFormClosed(e);
    }
}
