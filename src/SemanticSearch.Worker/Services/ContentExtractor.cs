using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using UglyToad.PdfPig;

namespace SemanticSearch.Worker.Services;

public class ContentExtractor(ILogger<ContentExtractor> logger)
{
    /// <summary>
    /// Never throws: a file that fails to parse (corrupt, password-protected, unsupported
    /// internal format, etc.) must not abort the rest of the indexing run.
    /// </summary>
    public string Extract(string filePath)
    {
        try
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".pdf"  => ExtractPdf(filePath),
                ".docx" => ExtractDocx(filePath),
                ".xlsx" => ExtractXlsx(filePath),
                _       => string.Empty
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Content extraction failed for {File}", Path.GetFileName(filePath));
            return string.Empty;
        }
    }

    private static string ExtractPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(filePath);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocx(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;
        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }

    private static string ExtractXlsx(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = SpreadsheetDocument.Open(filePath, false);
        var workbook = doc.WorkbookPart;
        if (workbook == null) return string.Empty;

        var sharedStrings = workbook.SharedStringTablePart?.SharedStringTable;

        foreach (var sheet in workbook.WorksheetParts)
        {
            foreach (var cell in sheet.Worksheet.Descendants<Cell>())
            {
                var value = cell.CellValue?.Text;
                if (cell.DataType?.Value == CellValues.SharedString && sharedStrings != null)
                    value = sharedStrings.ElementAt(int.Parse(value ?? "0")).InnerText;
                if (!string.IsNullOrWhiteSpace(value))
                    sb.Append(value).Append(' ');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public (string? Author, DateTime? CreatedAt) ExtractDocxMetadata(string filePath)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var props = doc.PackageProperties;
            return (props.Creator, props.Created);
        }
        catch
        {
            return (null, null);
        }
    }
}
