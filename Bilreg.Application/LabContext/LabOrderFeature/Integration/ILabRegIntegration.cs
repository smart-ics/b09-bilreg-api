namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabRegExecutionRequest(string OrderId, string UserId);

public interface ILabRegIntegration
{
    string CreateExecutionRegistration(LabRegExecutionRequest request);
}
