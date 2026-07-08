using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;

namespace PrintService.Windows.Services;

public sealed class ImagePrintService
{
    public void PrintImageDirect(string base64Content, string printerName, int copies, string paper)
    {
        var imageBytes = DecodeBase64(base64Content);
        using var imageStream = new MemoryStream(imageBytes);
        using var image = Image.FromStream(imageStream);

        var (widthMm, heightMm) = ParsePaperSizeMm(paper, image.Width, image.Height);
        var widthHundredths = (int)(widthMm / 25.4 * 100);
        var heightHundredths = (int)(heightMm / 25.4 * 100);

        using var printDocument = new PrintDocument();
        printDocument.PrinterSettings.PrinterName = printerName;
        printDocument.PrinterSettings.Copies = (short)Math.Clamp(copies, 1, short.MaxValue);
        printDocument.PrintController = new StandardPrintController();
        printDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", widthHundredths, heightHundredths);
        printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        printDocument.PrintPage += (_, e) =>
        {
            e.Graphics!.DrawImage(image, 0, 0, e.PageBounds.Width, e.PageBounds.Height);
            e.HasMorePages = false;
        };

        for (var i = 0; i < copies; i++)
        {
            printDocument.Print();
        }
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

    private static (double widthMm, double heightMm) ParsePaperSizeMm(string paper, int imageWidth, int imageHeight)
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

        const double dpi = 203.0;
        return (imageWidth / dpi * 25.4, imageHeight / dpi * 25.4);
    }
}
