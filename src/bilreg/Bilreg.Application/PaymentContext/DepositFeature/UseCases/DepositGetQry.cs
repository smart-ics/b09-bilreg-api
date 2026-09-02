using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.DepositFeature.UseCases;

public record DepositGetQry(string DepositId) : IRequest<DepositGetResponse>, IDepositId;
public record DepositGetResponse(string DepositId, string DepositDate, string UserId,
    RegReff Reg, LayananReff Layanan, string Keterangan, decimal NilaiDeposit);

public class DepositGetHandler : IRequestHandler<DepositGetQry, DepositGetResponse>
{
    private readonly IDepositRepo _depositRepo;
    public DepositGetHandler(IDepositRepo depositRepo)
    {
        _depositRepo = depositRepo;
    }
    public Task<DepositGetResponse> Handle(DepositGetQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DepositId);

        var deposit = _depositRepo.LoadEntity(request).GetValueOrThrow($"Deposit {request.DepositId} not found");
        var result = new DepositGetResponse(
            deposit.DepositId,
            deposit.DepositDate.ToString(DateFormatEnum.YMD_HMS),
            deposit.Audit.Created.UserId,
            deposit.Reg,
            deposit.Layanan,
            deposit.Keterangan,
            deposit.NilaiDeposit
        );

        return Task.FromResult( result );
    }
}
