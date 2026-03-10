using System.Text;

namespace AiAssistant.Domain.Tools;

public sealed record EmailDraft
{
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required List<string> AffectedDocuments { get; init; }
}

public static class DraftEmailTool
{
    public static EmailDraft? Execute(IReadOnlyList<DocumentExpiryResult> expiryResults)
    {
        if (expiryResults.Count == 0)
            return null;

        var expired = expiryResults.Where(r => r.Status == ExpiryStatus.Expired).ToList();
        var urgent  = expiryResults.Where(r => r.Status == ExpiryStatus.Urgent).ToList();
        var warning = expiryResults.Where(r => r.Status == ExpiryStatus.Warning).ToList();

        var subject = BuildSubject(expired, urgent);
        var body    = BuildBody(expired, urgent, warning);

        return new EmailDraft
        {
            Subject           = subject,
            Body              = body,
            AffectedDocuments = expiryResults.Select(r => r.FileName).ToList()
        };
    }

    private static string BuildSubject(
        List<DocumentExpiryResult> expired,
        List<DocumentExpiryResult> urgent)
    {
        if (expired.Count > 0)
            return $"[URGENTE] {expired.Count} documento(s) vencido(s) requieren atención inmediata";

        if (urgent.Count > 0)
            return $"[AVISO] {urgent.Count} documento(s) vencen en menos de 7 días";

        return "[RECORDATORIO] Documentos próximos a vencer";
    }

    private static string BuildBody(
        List<DocumentExpiryResult> expired,
        List<DocumentExpiryResult> urgent,
        List<DocumentExpiryResult> warning)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Estimado equipo,");
        sb.AppendLine();
        sb.AppendLine("El sistema de gestión documental ha detectado los siguientes documentos que requieren atención:");
        sb.AppendLine();

        if (expired.Count > 0)
        {
            sb.AppendLine("VENCIDOS:");
            foreach (var doc in expired)
                sb.AppendLine($"   - {doc.FileName} (venció hace {Math.Abs(doc.DaysUntilExpiry)} días, el {doc.ExpiryDate:dd/MM/yyyy})");
            sb.AppendLine();
        }

        if (urgent.Count > 0)
        {
            sb.AppendLine("URGENTES (vencen en menos de 7 días):");
            foreach (var doc in urgent)
                sb.AppendLine($"   - {doc.FileName} (vence en {doc.DaysUntilExpiry} días, el {doc.ExpiryDate:dd/MM/yyyy})");
            sb.AppendLine();
        }

        if (warning.Count > 0)
        {
            sb.AppendLine("PRÓXIMOS A VENCER (menos de 30 días):");
            foreach (var doc in warning)
                sb.AppendLine($"   - {doc.FileName} (vence en {doc.DaysUntilExpiry} días, el {doc.ExpiryDate:dd/MM/yyyy})");
            sb.AppendLine();
        }

        sb.AppendLine("Por favor, tome las acciones necesarias a la brevedad.");
        sb.AppendLine();
        sb.AppendLine("Este mensaje fue generado automáticamente por el Asistente de IA de SG-MbSuite.");
        sb.AppendLine("IMPORTANTE: Este es un borrador. Requiere aprobación humana antes de ser enviado.");

        return sb.ToString();
    }
}