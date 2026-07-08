using PrintService.Windows.Config;
using PrintService.Windows.Models;

namespace PrintService.Windows.Services;

public sealed class PrinterService
{
    private readonly PdfPrintService _pdfPrintService;
    private readonly HtmlPrintService _htmlPrintService;
    private readonly ImagePrintService _imagePrintService;
    private readonly SettingsRepository _settingsRepository;

    public PrinterService(
        PdfPrintService pdfPrintService,
        HtmlPrintService htmlPrintService,
        ImagePrintService imagePrintService,
        SettingsRepository settingsRepository)
    {
        _pdfPrintService = pdfPrintService;
        _htmlPrintService = htmlPrintService;
        _imagePrintService = imagePrintService;
        _settingsRepository = settingsRepository;
    }

    public List<PrinterInfo> GetPrinters()
    {
        var names = _pdfPrintService.ListPrinterNames();
        var systemDefault = _pdfPrintService.GetDefaultPrinterName();
        var settingsDefault = _settingsRepository.Get().DefaultPrinter;

        return names.Select(name => new PrinterInfo
        {
            Name = name,
            Default = name == systemDefault ||
                      (!string.IsNullOrWhiteSpace(settingsDefault) && name.Equals(settingsDefault, StringComparison.OrdinalIgnoreCase)),
        }).ToList();
    }

    public async Task ExecutePrintJobAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        var settings = _settingsRepository.Get();
        var printer = request.Printer;

        if (string.IsNullOrWhiteSpace(printer))
        {
            printer = !string.IsNullOrWhiteSpace(settings.DefaultPrinter)
                ? settings.DefaultPrinter
                : _pdfPrintService.GetDefaultPrinterName();
        }

        if (string.IsNullOrWhiteSpace(printer))
        {
            throw new InvalidOperationException("No printer specified and no default printer found");
        }

        var printerNames = _pdfPrintService.ListPrinterNames();
        if (!printerNames.Any(p => p.Equals(printer, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Printer not found: {printer}");
        }

        var copies = Math.Max(1, request.Copies);
        var paper = string.IsNullOrWhiteSpace(request.Paper) ? settings.DefaultPaper : request.Paper;

        switch (request.ContentType.ToLowerInvariant())
        {
            case "html":
                await PrintHtmlAsync(request.Content, printer, copies, paper);
                break;
            case "pdf":
                PrintPdf(request.Content, printer, copies);
                break;
            case "image":
                _imagePrintService.PrintImageDirect(request.Content, printer, copies, paper);
                break;
            default:
                throw new InvalidOperationException($"Unsupported contentType: {request.ContentType}");
        }

        await Task.CompletedTask;
    }

    private async Task PrintHtmlAsync(string html, string printer, int copies, string paper)
    {
        var pdfPath = await _htmlPrintService.HtmlToPdfFileAsync(html, paper);
        try
        {
            _pdfPrintService.PrintFile(pdfPath, printer, copies);
        }
        finally
        {
            TryDelete(pdfPath);
        }
    }

    private void PrintPdf(string base64Content, string printer, int copies)
    {
        var bytes = DecodeBase64(base64Content);
        _pdfPrintService.PrintBuffer(bytes, printer, copies);
    }

    private static byte[] DecodeBase64(string content)
    {
        var data = content.Trim();
        var comma = data.IndexOf(',');
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
        {
            data = data[(comma + 1)..];
        }

        return Convert.FromBase64String(data);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore temp file cleanup failures.
        }
    }
}
