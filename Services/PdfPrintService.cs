using System.Drawing.Printing;
using PdfiumViewer;

namespace PrintService.Windows.Services;

public sealed class PdfPrintService
{
    public IReadOnlyList<string> ListPrinterNames()
    {
        return PrinterSettings.InstalledPrinters.Cast<string>().ToList();
    }

    public string? GetDefaultPrinterName()
    {
        var settings = new PrinterSettings();
        return string.IsNullOrWhiteSpace(settings.PrinterName) ? null : settings.PrinterName;
    }

    public void PrintFile(string filePath, string printerName, int copies)
    {
        using var document = PdfDocument.Load(filePath);
        PrintDocument(document, printerName, copies);
    }

    public void PrintBuffer(byte[] pdfBytes, string printerName, int copies)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Load(stream);
        PrintDocument(document, printerName, copies);
    }

    private static void PrintDocument(PdfDocument document, string printerName, int copies)
    {
        using var printDocument = document.CreatePrintDocument();
        printDocument.PrinterSettings.PrinterName = printerName;
        printDocument.PrinterSettings.Copies = (short)Math.Clamp(copies, 1, short.MaxValue);
        printDocument.PrintController = new StandardPrintController();
        printDocument.Print();
    }
}
