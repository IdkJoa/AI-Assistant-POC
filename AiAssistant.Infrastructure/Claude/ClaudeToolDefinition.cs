using Anthropic.SDK.Common;

namespace AiAssistant.Infrastructure.Claude;

public static class ClaudeToolDefinitions
{
    public static List<Tool> GetAll() =>
    [
        Tool.FromFunc(
            "get_borrowers_with_approved_loans",
            () => Task.FromResult(""),
            "Obtiene la lista de prestatarios con sus préstamos aprobados desde MbSuite. " +
            "Usar cuando el usuario pregunte sobre préstamos, prestatarios, estado de cuenta, " +
            "exposición crediticia, cuotas pendientes o balances."
        ),

        Tool.FromFunc(
            "check_document_expiry",
            () => Task.FromResult(""),
            "Verifica el vencimiento de los documentos indexados en el sistema. " +
            "Usar cuando el usuario pregunte sobre documentos vencidos, próximos a vencer, " +
            "contratos, licencias o fechas de expiración."
        )
    ];
}