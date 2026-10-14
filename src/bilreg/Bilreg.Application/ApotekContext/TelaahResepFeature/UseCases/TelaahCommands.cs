using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;

public record TelaahStartCmd(string UserId, string ResepKerjaId, int ExpectedVersion)
    : IRequest<TelaahResponse>;

public record TelaahKajianUpdate(string KajianResepId, bool IsYes, string CatatanKajian);

public record TelaahUpdateKajianCmd(
    string UserId,
    string TelaahResepId,
    int ExpectedVersion,
    IReadOnlyList<TelaahKajianUpdate> Items)
    : IRequest<TelaahResponse>;

public record TelaahUpdateItemCmd(
    string UserId,
    string TelaahResepId,
    int ExpectedVersion,
    int ItemNo,
    TelaahDispositionEnum Disposition,
    string AcceptedBrgId,
    string AcceptedBrgName,
    decimal AcceptedQty,
    string Reason)
    : IRequest<TelaahResponse>;

public record TelaahCompleteCmd(string UserId, string TelaahResepId, int ExpectedVersion)
    : IRequest<TelaahResponse>;

public record TelaahResponse(string TelaahResepId, TelaahStatusEnum Status, int Version);

public class TelaahStartHandler : IRequestHandler<TelaahStartCmd, TelaahResponse>
{
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IResepKerjaRepo _resepRepo;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly IKemkesKajianResepCatalogPort? _catalogPort;

    public TelaahStartHandler(
        ITelaahResepRepo telaahRepo,
        IResepKerjaRepo resepRepo,
        IAptAuthorizationPolicy auth,
        IKemkesKajianResepCatalogPort? catalogPort = null)
    {
        _telaahRepo = telaahRepo;
        _resepRepo = resepRepo;
        _auth = auth;
        _catalogPort = catalogPort;
    }

    public Task<TelaahResponse> Handle(TelaahStartCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(TelaahStartCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.ResepKerjaId, nameof(request.ResepKerjaId));
        var resep = _resepRepo.LoadEntity(Bilreg.Domain.ApotekContext.ResepKerjaFeature.ResepKerjaModel.Key(request.ResepKerjaId))
            .GetValueOrThrow($"Resep Kerja '{request.ResepKerjaId}' not found");
        var existing = _telaahRepo.LoadByResepKerja(request.ResepKerjaId);
        TelaahResepModel telaah;
        if (existing.HasValue)
        {
            telaah = existing.Value;
            if (request.ExpectedVersion > 0)
                telaah.AssertExpectedVersion(request.ExpectedVersion);
        }
        else
        {
            var catalog = _catalogPort?.GetAll();
            var kajianItems = catalog != null && catalog.Count > 0
                ? catalog.Select(x => TelaahResepKajianModel.Create(x.NoUrut, x.KajianResepId, x.KajianResepName, x.GrupKajianResepId, x.GrupKajianResepName)).ToList()
                : TelaahResepKajianModel.DefaultCatalog();

            telaah = TelaahResepModel.Open(
                resep.ResepKerjaId,
                resep.RegId,
                resep.Items.Select(x => TelaahResepItemModel.Pending(x.ItemNo, x.ItemNo, x.BrgId, x.BrgName, x.Qty)),
                kajianItems);
        }

        telaah.Start(request.UserId, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _telaahRepo.SaveChanges(telaah);
        trans.Complete();
        return Task.FromResult(new TelaahResponse(telaah.TelaahResepId, telaah.TelaahStatus, telaah.Version));
    }
}

public class TelaahUpdateKajianHandler : IRequestHandler<TelaahUpdateKajianCmd, TelaahResponse>
{
    private readonly ITelaahResepRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public TelaahUpdateKajianHandler(ITelaahResepRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<TelaahResponse> Handle(TelaahUpdateKajianCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(TelaahUpdateKajianCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.TelaahResepId, nameof(request.TelaahResepId));
        var telaah = _repo.LoadEntity(TelaahResepModel.Key(request.TelaahResepId))
            .GetValueOrThrow($"Telaah '{request.TelaahResepId}' not found");
        telaah.AssertExpectedVersion(request.ExpectedVersion);
        var items = request.Items ?? Array.Empty<TelaahKajianUpdate>();
        telaah.UpdateKajianBatch(items.Select(x => (x.KajianResepId, x.IsYes, x.CatatanKajian)));
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(telaah);
        trans.Complete();
        return Task.FromResult(new TelaahResponse(telaah.TelaahResepId, telaah.TelaahStatus, telaah.Version));
    }
}

public class TelaahUpdateItemHandler : IRequestHandler<TelaahUpdateItemCmd, TelaahResponse>
{
    private readonly ITelaahResepRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public TelaahUpdateItemHandler(ITelaahResepRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<TelaahResponse> Handle(TelaahUpdateItemCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(TelaahUpdateItemCmd), request.UserId);
        var telaah = _repo.LoadEntity(TelaahResepModel.Key(request.TelaahResepId))
            .GetValueOrThrow($"Telaah '{request.TelaahResepId}' not found");
        telaah.AssertExpectedVersion(request.ExpectedVersion);
        var current = telaah.Items.First(x => x.ItemNo == request.ItemNo);
        // Note: current.WithDisposition preserves PrescribedBrgId, PrescribedBrgName, and PrescribedQty
        // from the existing item snapshot without modification or dropping.
        telaah.UpdateItem(current.WithDisposition(
            request.Disposition, request.AcceptedBrgId, request.AcceptedBrgName, request.AcceptedQty,
            request.Reason, request.UserId));
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(telaah);
        trans.Complete();
        return Task.FromResult(new TelaahResponse(telaah.TelaahResepId, telaah.TelaahStatus, telaah.Version));
    }
}

public class TelaahCompleteHandler : IRequestHandler<TelaahCompleteCmd, TelaahResponse>
{
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IResepKerjaRepo _resepRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public TelaahCompleteHandler(ITelaahResepRepo telaahRepo, IResepKerjaRepo resepRepo, IAptAuthorizationPolicy auth)
    {
        _telaahRepo = telaahRepo;
        _resepRepo = resepRepo;
        _auth = auth;
    }

    public Task<TelaahResponse> Handle(TelaahCompleteCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(TelaahCompleteCmd), request.UserId);
        var telaah = _telaahRepo.LoadEntity(TelaahResepModel.Key(request.TelaahResepId))
            .GetValueOrThrow($"Telaah '{request.TelaahResepId}' not found");
        telaah.AssertExpectedVersion(request.ExpectedVersion);
        telaah.Complete(DateTime.Now);
        var resep = _resepRepo.LoadEntity(
            Bilreg.Domain.ApotekContext.ResepKerjaFeature.ResepKerjaModel.Key(telaah.ResepKerjaId))
            .GetValueOrThrow($"Resep Kerja '{telaah.ResepKerjaId}' not found");
        resep.FreezeItems();
        using var trans = TransHelper.NewScope();
        _telaahRepo.SaveChanges(telaah);
        _resepRepo.SaveChanges(resep);
        trans.Complete();
        return Task.FromResult(new TelaahResponse(telaah.TelaahResepId, telaah.TelaahStatus, telaah.Version));
    }
}
