namespace AiAssistant.Domain.Tools;

public static class CheckDocumentExpiryTool
{
    public static IReadOnlyList<DocumentExpiryResult> Execute(IEnumerable<Dictionary<string, string>> metadataList)
    {
        var results = new List<DocumentExpiryResult>();
        var now = DateTime.UtcNow;

        foreach (var metadata in metadataList)
        {
            if (!metadata.TryGetValue("expiry_date", out var expiryRaw))
                continue;

            if (!DateTime.TryParse(expiryRaw, out var expiryDate))
                continue;
            
            var daysUntil = (int)(expiryDate.Date - now.Date).TotalDays;
            
            var status = daysUntil switch
            {
                < 0 => ExpiryStatus.Expired,
                <= 7 => ExpiryStatus.Urgent,
                <= 30 => ExpiryStatus.Warning,
                _ => ExpiryStatus.Ok
            };
            
            if (status == ExpiryStatus.Ok)
                continue;
            
            results.Add(new DocumentExpiryResult
            {
                FileName = metadata.GetValueOrDefault("file_name", "desconocido"),
                ExpiryDate = expiryDate,
                Status = status,
                DaysUntilExpiry =  daysUntil
            });
        }
        
        return results.OrderBy(r => r.DaysUntilExpiry).ToList();
    }
}