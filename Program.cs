using PrintService.Windows.Config;
using PrintService.Windows.Http;
using PrintService.Windows.Logging;
using PrintService.Windows.Services;

namespace PrintService.Windows;

internal static class Program
{
    private const string MutexName = "DLIS_PrintService_SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Print Service Windows is already running.\nCheck the system tray near the clock.",
                "Print Service Windows",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var logger = new FileLogger();
        var settings = new SettingsRepository();
        var pdfPrintService = new PdfPrintService();
        var htmlPrintService = new HtmlPrintService(pdfPrintService, logger);
        var imagePrintService = new ImagePrintService();
        var printerService = new PrinterService(pdfPrintService, htmlPrintService, imagePrintService, settings);
        var httpServer = new PrintHttpServer(settings, printerService, logger);

        logger.Info("Print Service Windows application started");

        Application.Run(new TrayApplicationContext(httpServer, logger, settings, htmlPrintService));
    }
}
