using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;

public record SalesOrderApplyCoverageCmd(
    string UserId,
    string SalesOrderId,
    int ExpectedVersion,
    string SepNo,
    List<SalesOrderCoverageItem> Items)
    : IRequest<string>;

public record SalesOrderCoverageItem(int ItemNo, FornasCoverageEnum Coverage);

public class SalesOrderApplyCoverageHandler : IRequestHandler<SalesOrderApplyCoverageCmd, string>
{
    private readonly ISalesOrderRepo _repo;
    private readonly ISepFornasPort _sepFornas;
    private readonly IAptAuthorizationPolicy _auth;

    public SalesOrderApplyCoverageHandler(ISalesOrderRepo repo, ISepFornasPort sepFornas, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _sepFornas = sepFornas;
        _auth = auth;
    }

    public Task<string> Handle(SalesOrderApplyCoverageCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(SalesOrderApplyCoverageCmd), request.UserId);
        var so = _repo.LoadEntity(SalesOrderModel.Key(request.SalesOrderId))
            .GetValueOrThrow($"Sales Order '{request.SalesOrderId}' not found");
        so.AssertExpectedVersion(request.ExpectedVersion);
        foreach (var item in request.Items)
        {
            var evidence = _sepFornas.Evaluate(so.RegId, so.Item(item.ItemNo).BrgId);
            if (string.IsNullOrWhiteSpace(evidence.SepNo) && string.IsNullOrWhiteSpace(request.SepNo))
                throw new ApotekDomainException("Fornas master membership alone is not Coverage Clearance.");
            var sep = string.IsNullOrWhiteSpace(request.SepNo) ? evidence.SepNo : request.SepNo;
            if (sep.Length > 50)
                throw new ApotekDomainException("SepNo exceeds PD-03 width 50.");
            so.SnapshotItemCoverage(item.ItemNo, item.Coverage == FornasCoverageEnum.Unknown ? evidence.Coverage : item.Coverage, sep);
        }
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(so);
        trans.Complete();
        return Task.FromResult(so.SalesOrderId);
    }
}
