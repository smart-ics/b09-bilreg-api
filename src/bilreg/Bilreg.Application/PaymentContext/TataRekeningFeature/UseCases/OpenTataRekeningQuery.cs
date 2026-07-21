using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record OpenTataRekeningQuery(string RegId) : IRequest<OpenTataRekeningResponse>, IRegKey;

public record OpenTataRekeningResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<TrsBillSummaryDto> Bills,
    IReadOnlyList<PaymentProjectionDto> Projection,
    IReadOnlyList<MergeRequestSummaryDto> PendingMergeRequests);

public class OpenTataRekeningHandler : IRequestHandler<OpenTataRekeningQuery, OpenTataRekeningResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IRegRepo _regRepo;
    private readonly IMergeRequestRepo _mergeRequestRepo;

    public OpenTataRekeningHandler(
        ITataRekeningRepo tataRekeningRepo,
        IRegRepo regRepo,
        IMergeRequestRepo mergeRequestRepo)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _regRepo = regRepo;
        _mergeRequestRepo = mergeRequestRepo;
    }

    public Task<OpenTataRekeningResponse> Handle(OpenTataRekeningQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

        var pendingMerges = LoadPendingMergeRequests(request, reg.Pasien.PasienId);

        var response = new OpenTataRekeningResponse(
            TataRekeningApplicationMapper.ToSummaryDto(tataRekening),
            tataRekening.ListTrsBill.Select(TataRekeningApplicationMapper.ToBillSummaryDto).ToList(),
            tataRekening.ListPayment.Select(TataRekeningApplicationMapper.ToProjectionDto).ToList(),
            pendingMerges);

        return Task.FromResult(response);
    }

    private IReadOnlyList<MergeRequestSummaryDto> LoadPendingMergeRequests(IRegKey regKey, string pasienId)
    {
        var byReg = _mergeRequestRepo.ListPendingByReg(regKey);
        var byPatient = _mergeRequestRepo.ListPendingByPatient(pasienId);

        return byReg
            .Concat(byPatient)
            .GroupBy(m => m.MergeRequestId, StringComparer.Ordinal)
            .Select(g => g.First())
            .Select(TataRekeningApplicationMapper.ToMergeRequestDto)
            .ToList();
    }
}
