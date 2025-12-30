
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfImportNilaiTarifCmd() : IRequest;

public class TrfImportNilaiTarifHandler : IRequestHandler<TrfImportNilaiTarifCmd>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;

    public TrfImportNilaiTarifHandler(INilaiTarifRepo nilaiTarifRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task Handle(TrfImportNilaiTarifCmd request, CancellationToken cancellationToken)
    {
        _nilaiTarifRepo.Import();
        return Task.CompletedTask;
    }
}