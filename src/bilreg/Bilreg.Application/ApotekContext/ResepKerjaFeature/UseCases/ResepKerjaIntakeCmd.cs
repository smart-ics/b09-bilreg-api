using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;

public record ResepKerjaIntakeElectronicCmd(
    string UserId,
    ResepKerjaSourceKindEnum SourceKind,
    string SourceResepId)
    : IRequest<ResepKerjaIntakeResponse>;

public record ResepKerjaIntakePhysicalCmd(
    string UserId,
    string RegId,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    string LayananId,
    string CaptureNote,
    string DocumentRef,
    List<ResepKerjaPhysicalItem> Items)
    : IRequest<ResepKerjaIntakeResponse>;

public record ResepKerjaPhysicalItem(
    int ItemNo,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    string Signa,
    string Instruction,
    string Note,
    bool IsRacik);

public record ResepKerjaIntakeResponse(string ResepKerjaId, bool IdempotentReplay);

public class ResepKerjaIntakeElectronicHandler
    : IRequestHandler<ResepKerjaIntakeElectronicCmd, ResepKerjaIntakeResponse>
{
    private readonly IResepKerjaRepo _repo;
    private readonly IPrescriptionContractPort _contract;
    private readonly IAptAuthorizationPolicy _auth;

    public ResepKerjaIntakeElectronicHandler(
        IResepKerjaRepo repo,
        IPrescriptionContractPort contract,
        IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _contract = contract;
        _auth = auth;
    }

    public Task<ResepKerjaIntakeResponse> Handle(ResepKerjaIntakeElectronicCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(ResepKerjaIntakeElectronicCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.SourceResepId, nameof(request.SourceResepId));
        if (request.SourceKind == ResepKerjaSourceKindEnum.Physical)
            throw new InvalidOperationException("Use physical intake for Physical source.");

        var existing = _repo.LoadBySource(request.SourceKind, request.SourceResepId);
        if (existing.HasValue)
            return Task.FromResult(new ResepKerjaIntakeResponse(existing.Value.ResepKerjaId, true));

        var contract = _contract.Load(request.SourceKind, request.SourceResepId);
        var items = contract.Items.Select((x, i) => new ResepKerjaItemModel(
            i + 1, x.SourceItemNo, x.BrgId, x.BrgName, x.SatuanId, x.SatuanName,
            x.Qty, x.Iter, x.Signa, x.Instruction, x.Note, x.IsRacik)).ToList();
        var components = contract.Components.Select(x => new ResepKerjaComponentModel(
            items.First(i => i.SourceItemNo == x.SourceItemNo).ItemNo,
            x.ComponentNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty)).ToList();
        var model = ResepKerjaModel.IntakeElectronic(
            contract.SourceKind, contract.SourceResepId, contract.RegId, contract.PasienId, contract.PasienName,
            contract.DokterId, contract.DokterName, contract.LayananId, contract.Urgenitas, contract.IterEntitled,
            items, components, AuditTrailType.Create(request.UserId, DateTime.Now));

        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.FromResult(new ResepKerjaIntakeResponse(model.ResepKerjaId, false));
    }
}

public class ResepKerjaIntakePhysicalHandler
    : IRequestHandler<ResepKerjaIntakePhysicalCmd, ResepKerjaIntakeResponse>
{
    private readonly IResepKerjaRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public ResepKerjaIntakePhysicalHandler(IResepKerjaRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<ResepKerjaIntakeResponse> Handle(ResepKerjaIntakePhysicalCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(ResepKerjaIntakePhysicalCmd), request.UserId);
        var items = request.Items.Select(x => new ResepKerjaItemModel(
            x.ItemNo, x.ItemNo, x.BrgId, x.BrgName, x.SatuanId, x.SatuanName,
            x.Qty, 0, x.Signa, x.Instruction, x.Note, x.IsRacik)).ToList();
        var model = ResepKerjaModel.IntakePhysical(
            request.RegId, request.PasienId, request.PasienName, request.DokterId, request.DokterName,
            request.LayananId, request.CaptureNote, request.DocumentRef, items, [],
            AuditTrailType.Create(request.UserId, DateTime.Now));

        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.FromResult(new ResepKerjaIntakeResponse(model.ResepKerjaId, false));
    }
}
