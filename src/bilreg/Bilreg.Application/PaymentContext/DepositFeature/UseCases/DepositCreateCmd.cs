using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.DepositFeature.UseCases;

public record DepositCreateCmd(
    string RegId,
    string LayananId,
    string Keterangan,
    decimal NilaiDeposit,
    string UserId) : IRequest<DepositCreateResponse>, IRegKey;

public record DepositCreateResponse(string DepositId);

public class DepositCreateHandler : IRequestHandler<DepositCreateCmd, DepositCreateResponse>
{
    private readonly IDepositRepo _depositRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DepositCreateHandler(
        IDepositRepo depositRepo,
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        ITglJamProvider tglJamProvider)
    {
        _depositRepo = depositRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<DepositCreateResponse> Handle(DepositCreateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.NilaiDeposit, nameof(request.NilaiDeposit));

        var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
            .GetValueOrThrow($"Registrasi {request.RegId} tidak ditemukan.");
        var layanan = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
            .GetValueOrThrow($"Layanan {request.LayananId} tidak ditemukan.");

        var audit = AuditTrailType.Create(request.UserId, _tglJamProvider.Now);
        var model = DepositModel.Create(reg, layanan, request.Keterangan, request.NilaiDeposit, audit);

        _depositRepo.SaveChanges(model);

        return Task.FromResult(new DepositCreateResponse(model.DepositId));
    }
}