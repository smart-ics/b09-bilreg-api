using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegRegAktifRemoveCmd(string RegId, string ClientIpAddress, string UserAgent) : IRequest, IRegKey;

public class RegRegAktifRemoveHandler : IRequestHandler<RegRegAktifRemoveCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAuditRepo _auditRepo;
    public RegRegAktifRemoveHandler(IRegRepo regRepo, 
        IRegAktifRepo regAktifRepo, 
        IAuditRepo auditRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _auditRepo = auditRepo;
    }
    public Task Handle(RegRegAktifRemoveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        if (IsValidToBeRemove(request, out var reg, out var regAktif))
        {
            var snapshotJson = AuditLogSnapshotJson.Serialize(regAktif);
            using (var trans = TransHelper.NewScope())
            {
                _regAktifRepo.Delete(regAktif);
                trans.Complete();
            }

            var audit = CreateAudit(regAktif, snapshotJson, request);
            _auditRepo.SaveChanges(audit);
        }

        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    private RegModel LoadReg(RegRegAktifRemoveCmd request)
    {
        return _regRepo.LoadEntity(request).GetValueOrDefault(RegModel.Default);
    }

    private bool IsValidToBeRemove(RegRegAktifRemoveCmd request, out RegModel reg, out RegAktifModel regAktif)
    {
        reg = LoadReg(request);
        if (reg.RegId == "-")
        {
            regAktif = RegAktifModel.Default;
            return false;
        }

        regAktif = _regAktifRepo.LoadEntity(reg).GetValueOrDefault(RegAktifModel.Default);
        if (regAktif.RegId == "-")
        {
            return false;
        }

        return true; // Lolos semua validasi
    }
    private AuditLog CreateAudit(RegAktifModel reg, string snapShotJson, RegRegAktifRemoveCmd cmd)
    {
        var result = AuditLog.Create(
            userId: "FO-User",
            actionType: "DELETE",
            entityName: nameof(RegAktifModel),
            entityId: reg.RegId,
            reason: "Remove Reg.Aktif",
            originalDataJson: snapShotJson,
            correlationId: reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
    #endregion
}
