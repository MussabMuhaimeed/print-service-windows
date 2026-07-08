using System.Globalization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PrintService.Windows.Logging;

namespace PrintService.Windows.Services;

/// <summary>
/// Converts HTML to PDF using WebView2 (Edge), then prints via PdfPrintService.
/// </summary>
public sealed class HtmlPrintService : IDisposable
{
    private readonly PdfPrintService _pdfPrintService;
    private readonly FileLogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private WebView2? _webView;
    private Form? _hostForm;

    public HtmlPrintService(PdfPrintService pdfPrintService, FileLogger logger)
    {
        _pdfPrintService = pdfPrintService;
        _logger = logger;
    }

    public async Task<string> HtmlToPdfFileAsync(string html, string paper)
    {
        await _semaphore.WaitAsync();
        try
        {
            await EnsureWebViewAsync();

            var wrappedHtml = WrapHtml(html, paper);
            _webView!.NavigateToString(wrappedHtml);

            await WaitForNavigationAsync();

            var tempPdf = Path.Combine(Path.GetTempPath(), $"print-html-{Guid.NewGuid():N}.pdf");
            await _webView.CoreWebView2!.PrintToPdfAsync(tempPdf);
            return tempPdf;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureWebViewAsync()
    {
        if (_webView is not null)
        {
            return;
        }

        _hostForm = new Form
        {
            Width = 1,
            Height = 1,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            Opacity = 0,
            WindowState = FormWindowState.Minimized,
        };

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
        };

        _hostForm.Controls.Add(_webView);
        _hostForm.Show();

        await _webView.EnsureCoreWebView2Async();
        _logger.Info("WebView2 initialized for HTML printing");
    }

    private static async Task WaitForNavigationAsync()
    {
        await Task.Delay(500);
    }

    private static string WrapHtml(string html, string paper)
    {
        var (widthMm, heightMm) = ParsePaperSizeMm(paper);

        if (html.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return InjectPageSize(html, widthMm, heightMm);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            """
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <style>
                @page {{ size: {0}mm {1}mm; margin: 0; }}
                body {{ margin: 0; padding: 0; }}
              </style>
            </head>
            <body>{2}</body>
            </html>
            """,
            widthMm,
            heightMm,
            html);
    }

    private static string InjectPageSize(string html, double widthMm, double heightMm)
    {
        var style = $"<style>@page {{ size: {widthMm}mm {heightMm}mm; margin: 0; }} body {{ margin: 0; }}</style>";
        if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            return html.Replace("</head>", style + "</head>", StringComparison.OrdinalIgnoreCase);
        }

        return style + html;
    }

    private static (double widthMm, double heightMm) ParsePaperSizeMm(string paper)
    {
        if (paper.Contains('x', StringComparison.Ordinal))
        {
            var parts = paper.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out var w) &&
                double.TryParse(parts[1], out var h))
            {
                return (w, h);
            }
        }

        return paper.ToUpperInvariant() switch
        {
            "A4" => (210, 297),
            _ => (100, 150),
        };
    }

    public void Dispose()
    {
        _webView?.Dispose();
        _hostForm?.Dispose();
        _semaphore.Dispose();
    }
}
