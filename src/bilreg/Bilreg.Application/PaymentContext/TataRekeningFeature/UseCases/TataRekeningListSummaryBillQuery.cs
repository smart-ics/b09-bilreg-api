using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record TataRekeningListSummaryBillQuery(string RegId) : IRequest<TataRekeningListSummaryBillResponse>, IRegKey;

public record TataRekeningListSummaryBillResponse(
    string RegId,
    PasienReff Pasien,
    IEnumerable<TrsBillSummaryType> Summaries);

public class TataRekeningListSummaryBillHandler : IRequestHandler<TataRekeningListSummaryBillQuery, TataRekeningListSummaryBillResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    public TataRekeningListSummaryBillHandler(IRegRepo regRepo,
        ITataRekeningRepo tataRekeningRepo)
    {
        _regRepo = regRepo;
        _tataRekeningRepo = tataRekeningRepo;
    }

    public Task<TataRekeningListSummaryBillResponse> Handle(TataRekeningListSummaryBillQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Registrasi {request.RegId} not found");
        var tataRekening = _tataRekeningRepo.LoadEntity(request).GetValueOrThrow($"Bills {request.RegId} not found");

        var result = new TataRekeningListSummaryBillResponse(reg.RegId, reg.Pasien,
            tataRekening.ListSummaryBill);

        return Task.FromResult(result);
    }
}
