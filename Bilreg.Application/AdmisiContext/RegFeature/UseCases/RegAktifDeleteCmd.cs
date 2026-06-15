using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifDeleteCmd(string RegId, string ClientIpAddress, string UserAgent) : IRequest, IRegKey;

public class RegAktifDeleteHandler : IRequestHandler<RegAktifDeleteCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAuditRepo _auditRepo;
    public RegAktifDeleteHandler(IRegRepo regRepo, 
        IRegAktifRepo regAktifRepo, 
        IAuditRepo auditRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _auditRepo = auditRepo;
    }
    public Task Handle(RegAktifDeleteCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var reg = LoadReg(request);
        if (reg is null)
            return Task.CompletedTask;
        if (reg.IsAktif == false)
            throw new KeyNotFoundException($"Register {request.RegId} sudah tidak aktif");
        var snapshotJson = AuditLogSnapshotJson.Serialize(reg);

        using (var trans = TransHelper.NewScope())
        {
            _regAktifRepo.Delete(reg);
            trans.Complete();
        }

        var audit = CreateAudit(reg, snapshotJson, request);
        _auditRepo.SaveChanges(audit);

        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    private RegModel? LoadReg(RegAktifDeleteCmd request)
    {
        return _regRepo.LoadEntity(request).GetValueOrDefault(RegModel.Default);
    }

    private AuditLog CreateAudit(RegModel reg, string snapShotJson, RegAktifDeleteCmd cmd)
    {
        var result = AuditLog.Create(
            userId: "FO-User",
            actionType: "DELETE",
            entityName: nameof(RegModel),
            entityId: reg.RegId,
            reason: "Delete Reg. Aktif",
            originalDataJson: snapShotJson,
            correlationId: reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
    #endregion
}
