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
        ),
        
        Tool.FromFunc(
            "get_account_statement",
            (
                [FunctionParameter("ID del party para consultar el estado de cuenta", true)]
                string partyId,
                [FunctionParameter("Código de moneda para filtrar (ej: DOP, USD, EUR). Opcional.", false)]
                string? currencyCode,
                [FunctionParameter("Número de página. Default: 1.", false)]
                int pageNumber = 1,
                [FunctionParameter("Tamaño de página. Default: 10.", false)]
                int pageSize = 10
            ) => Task.FromResult(""),
            "Obtiene el estado de cuenta de un party desde MbSuite. " +
            "Usar cuando el usuario pregunte sobre transacciones, movimientos, " +
            "desembolsos, estado de cuenta o historial financiero de un party."
        ),
    ];
}