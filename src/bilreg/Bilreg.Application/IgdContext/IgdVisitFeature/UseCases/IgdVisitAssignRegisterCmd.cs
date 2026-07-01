using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitAssignRegisterCmd(
    string IgdVisitId,
    string RegId,
    string UserId)
    : IRequest, IIgdVisitKey, IRegKey;

public class IgdVisitAssignRegisterHandler : IRequestHandler<IgdVisitAssignRegisterCmd>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;

    public IgdVisitAssignRegisterHandler(IIgdVisitRepo igdVisitRepo, IRegRepo regRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
    }

    public Task Handle(IgdVisitAssignRegisterCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Reg '{request.RegId}' not found");

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        visit.AssignRegister(reg, audit);

        using var trans = TransHelper.NewScope();
        _igdVisitRepo.SaveChanges(visit);
        trans.Complete();
        return Task.CompletedTask;
    }
}
