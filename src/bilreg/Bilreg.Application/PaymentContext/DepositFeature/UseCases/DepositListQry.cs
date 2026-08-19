using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.DepositFeature.UseCases;

public record DepositListQry(string RegId) : IRequest<IEnumerable<DepositListResponse>>, IRegKey;
public record DepositListResponse(string DepositId, string DepositDate, string UserId,
    RegReff Reg, LayananReff Layanan, string Keterangan, decimal NilaiDeposit);

public class DepositListHandler : IRequestHandler<DepositListQry, IEnumerable<DepositListResponse>>
{
    private readonly IDepositRepo _depositRepo;

    public DepositListHandler(IDepositRepo depositRepo)
    {
        _depositRepo = depositRepo;
    }

    public Task<IEnumerable<DepositListResponse>> Handle(DepositListQry request, CancellationToken cancellationToken)
    {
        var deposits = _depositRepo.ListData(request) ?? [];
        var result = deposits.Select(d => new DepositListResponse(
            DepositId: d.DepositId,
            DepositDate: d.DepositDate.ToString(DateFormatEnum.YMD_HMS),
            UserId: d.Audit.Created.UserId,
            Reg: d.Reg,
            Layanan: d.Layanan,
            Keterangan: d.Keterangan,
            NilaiDeposit: d.NilaiDeposit
        ));

        return Task.FromResult(result);
    }
}