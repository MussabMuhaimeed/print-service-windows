namespace PrintService.Windows.Models;

public sealed class AppSettings
{
    public string DefaultPrinter { get; set; } = string.Empty;
    public string DefaultPaper { get; set; } = "100x150";
    public int Port { get; set; } = 4510;
    public List<string> AllowedOrigins { get; set; } = ["http://localhost", "http://127.0.0.1"];

    public static AppSettings CreateDefault() => new();
}
