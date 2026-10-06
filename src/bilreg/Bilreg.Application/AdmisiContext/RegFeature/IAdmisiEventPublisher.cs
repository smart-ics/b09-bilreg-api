namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IAdmisiEventPublisher
{
    Task PublishRajalCreatedAsync(string regId, CancellationToken cancellationToken = default);
    Task PublishRanapCreatedAsync(string regId, CancellationToken cancellationToken = default);
}
