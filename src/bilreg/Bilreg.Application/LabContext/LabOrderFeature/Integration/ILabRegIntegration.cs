namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabRegExecutionRequest(string OrderId, string UserId, string? ExecutionRegId = null);

public interface ILabRegIntegration
{
    string CreateExecutionRegistration(LabRegExecutionRequest request);
}
