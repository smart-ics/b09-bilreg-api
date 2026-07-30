using Bilreg.Api.Configurations;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.AdmisiContext.AntrianFeature;

public sealed record AdmissionQueueWorkstationContext(
    string WorkstationKey,
    string WorkstationDisplayName,
    string LoketKey,
    bool Active,
    string Source);

public interface IAdmissionQueueWorkstationResolver
{
    AdmissionQueueWorkstationContext Resolve(HttpRequest request, string? payloadLoketKey);
}

public sealed class AdmissionQueueWorkstationResolver : IAdmissionQueueWorkstationResolver
{
    private readonly IAdmissionWorkstationRepo _repo;
    private readonly AdmissionQueueApiOptions _options;
    private readonly ILogger<AdmissionQueueWorkstationResolver> _logger;

    public AdmissionQueueWorkstationResolver(
        IAdmissionWorkstationRepo repo,
        IOptions<AdmissionQueueApiOptions> options,
        ILogger<AdmissionQueueWorkstationResolver> logger)
    {
        _repo = repo;
        _options = options.Value;
        _logger = logger;
    }

    public AdmissionQueueWorkstationContext Resolve(HttpRequest request, string? payloadLoketKey)
    {
        var workstationKey = request.Headers["X-Workstation-Key"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(workstationKey))
            throw new ArgumentException("X-Workstation-Key is required.");

        var headerLoket = request.Headers["X-Loket-Key"].FirstOrDefault()?.Trim();
        var legacyUsed = !string.IsNullOrWhiteSpace(headerLoket) || !string.IsNullOrWhiteSpace(payloadLoketKey);
        if (legacyUsed)
        {
            _logger.LogInformation(
                "Admission Queue legacy loket contract used for workstation {WorkstationKey}",
                workstationKey);
        }

        var db = _repo.LoadEntity(AdmissionWorkstationModel.Key(workstationKey));
        AdmissionQueueWorkstationContext resolved;
        if (db.HasValue)
        {
            if (!db.Value.Active)
                throw new AdmissionQueueConfigurationException(
                    AdmissionQueueConfigurationErrorCodes.WorkstationInactive,
                    $"Workstation '{workstationKey}' is inactive.");
            resolved = new AdmissionQueueWorkstationContext(
                db.Value.WorkstationKey,
                db.Value.DisplayName,
                db.Value.LoketKey,
                db.Value.Active,
                "database");
        }
        else
        {
            var configured = _options.Workstations.SingleOrDefault(x =>
                string.Equals(x.WorkstationKey?.Trim(), workstationKey, StringComparison.OrdinalIgnoreCase));
            if (configured is null || string.IsNullOrWhiteSpace(configured.LoketKey))
                throw new AdmissionQueueConfigurationException(
                    AdmissionQueueConfigurationErrorCodes.WorkstationNotFound,
                    $"Workstation '{workstationKey}' was not found.");
            resolved = new AdmissionQueueWorkstationContext(
                configured.WorkstationKey.Trim(),
                configured.WorkstationKey.Trim(),
                configured.LoketKey.Trim(),
                true,
                "appsettings");
        }

        if (!string.IsNullOrWhiteSpace(headerLoket) &&
            !string.Equals(headerLoket, resolved.LoketKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"X-Loket-Key '{headerLoket}' does not match server-resolved LoketKey '{resolved.LoketKey}'.");
        }

        if (!string.IsNullOrWhiteSpace(payloadLoketKey) &&
            !string.Equals(payloadLoketKey.Trim(), resolved.LoketKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Body LoketKey '{payloadLoketKey}' does not match server-resolved LoketKey '{resolved.LoketKey}'.");
        }

        return resolved;
    }
}
