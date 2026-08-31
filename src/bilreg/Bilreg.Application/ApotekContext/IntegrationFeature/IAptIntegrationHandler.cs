using Bilreg.Domain.ApotekContext.IntegrationFeature;

namespace Bilreg.Application.ApotekContext.IntegrationFeature;

public record AptIntegrationHandleResult(bool Success, string CorrelationId, string? ErrorMessage);

public interface IAptIntegrationHandler
{
    AptIntegrationTaskTypeEnum TaskType { get; }
    AptIntegrationHandleResult Handle(AptIntegrationTaskModel task);
}
