using System.Text;
using Api.Application.Proposals;

namespace Api.Infrastructure.Proposals;

public class SimpleProposalPdfGenerator : IProposalPdfGenerator
{
    public byte[] Generate(
        string title,
        decimal amount,
        string currency,
        string recipientName,
        DateTime createdAtUtc,
        string trackingUrl)
    {
        var text =
            $"Proposal: {Sanitize(title)}\\n" +
            $"Recipient: {Sanitize(recipientName)}\\n" +
            $"Amount: {amount:0.00} {Sanitize(currency)}\\n" +
            $"Created: {createdAtUtc:yyyy-MM-dd HH:mm:ss} UTC\\n" +
            $"Track: {Sanitize(trackingUrl)}";

        // Minimal one-page PDF document with plain text content stream.
        var escaped = EscapePdfText(text);
        var stream = $"BT /F1 12 Tf 50 760 Td ({escaped}) Tj ET";

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");
        var offsets = new List<int>();

        void AppendObject(string content)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine(content);
        }

        AppendObject("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
        AppendObject("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
        AppendObject("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj");
        AppendObject("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");
        AppendObject($"5 0 obj << /Length {Encoding.ASCII.GetByteCount(stream)} >> stream\\n{stream}\\nendstream endobj");

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine("0 6");
        pdf.AppendLine("0000000000 65535 f ");

        foreach (var off in offsets)
        {
            pdf.AppendLine($"{off:D10} 00000 n ");
        }

        pdf.AppendLine("trailer << /Size 6 /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefOffset.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }

    private static string Sanitize(string value)
    {
        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
