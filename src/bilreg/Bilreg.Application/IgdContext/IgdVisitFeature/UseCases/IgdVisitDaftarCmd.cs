using System.Globalization;
using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitDaftarCmd(
    string UserId,
    string VisitorName,
    string VisitorGender,
    string TglLahirYmd,
    string VisitorKontak)
    : IRequest<IgdVisitDaftarResponse>;

public record IgdVisitDaftarResponse(string IgdVisitId);

public class IgdVisitDaftarHandler : IRequestHandler<IgdVisitDaftarCmd, IgdVisitDaftarResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;

    public IgdVisitDaftarHandler(IIgdVisitRepo igdVisitRepo)
    {
        _igdVisitRepo = igdVisitRepo;
    }

    public Task<IgdVisitDaftarResponse> Handle(IgdVisitDaftarCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VisitorName, nameof(request.VisitorName));

        var visitor = new VisitorType(
            VisitorName : request.VisitorName,
            Gender      : string.IsNullOrWhiteSpace(request.VisitorGender) ? "-" : request.VisitorGender,
            TglLahir    : DateOnly.ParseExact(request.TglLahirYmd, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Kontak      : string.IsNullOrWhiteSpace(request.VisitorKontak) ? "-" : request.VisitorKontak);

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        var visit = IgdVisitModel.Create(visitor, audit);

        IgdVisitDaftarResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdVisitDaftarResponse(visit.IgdVisitId);
        }

        return Task.FromResult(response);
    }
}
