using Bilreg.Application.AdmisiContext.RegFeature;
using MassTransit;
using Microsoft.Extensions.Logging;
using MyHospital.MsgContract.Billing.AdmisiEvents;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class AdmisiEventPublisher : IAdmisiEventPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<AdmisiEventPublisher> _logger;

    public AdmisiEventPublisher(IBus bus, ILogger<AdmisiEventPublisher> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishRajalCreatedAsync(string regId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _bus.Publish(new RegRajalCreatedNotifEvent(regId), cancellationToken);
            _logger.LogInformation("Event notifikasi registrasi rajal berhasil dipublikasikan untuk RegId: {RegId}", regId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mempublikasikan event registrasi {EventType} untuk RegId: {RegId}. Error: {ErrorMessage}", nameof(RegRajalCreatedNotifEvent), regId, ex.Message);
        }
    }

    public async Task PublishRanapCreatedAsync(string regId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _bus.Publish(new RegRanapCreatedNotifEvent(regId), cancellationToken);
            _logger.LogInformation("Event notifikasi registrasi ranap berhasil dipublikasikan untuk RegId: {RegId}", regId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mempublikasikan event registrasi {EventType} untuk RegId: {RegId}. Error: {ErrorMessage}", nameof(RegRanapCreatedNotifEvent), regId, ex.Message);
        }
    }
}
