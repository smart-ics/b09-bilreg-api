using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.JualBebasFeature.UseCases;

public record JualBebasAcceptCmd(
    string UserId,
    string RegId,
    string PasienId,
    string PasienName,
    List<JualBebasAcceptItem> Items)
    : IRequest<JualBebasAcceptResponse>;

public record JualBebasAcceptItem(int ItemNo, string BrgId, string BrgName, string SatuanId, decimal Qty, string Signa);

public record JualBebasAcceptResponse(string JualBebasId);

public record JualBebasDeclineAfterAcceptCmd(string UserId, string JualBebasId, int ExpectedVersion) : IRequest;

public class JualBebasAcceptHandler : IRequestHandler<JualBebasAcceptCmd, JualBebasAcceptResponse>
{
    private readonly IJualBebasRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public JualBebasAcceptHandler(IJualBebasRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<JualBebasAcceptResponse> Handle(JualBebasAcceptCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(JualBebasAcceptCmd), request.UserId);
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));
        var items = request.Items.Select(x =>
            new JualBebasItemModel(x.ItemNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty, x.Signa)).ToList();
        var model = JualBebasModel.Accept(
            request.RegId, request.PasienId, request.PasienName, request.UserId, DateTime.Now, items);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.FromResult(new JualBebasAcceptResponse(model.JualBebasId));
    }
}

public class JualBebasDeclineAfterAcceptHandler : IRequestHandler<JualBebasDeclineAfterAcceptCmd>
{
    private readonly IJualBebasRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public JualBebasDeclineAfterAcceptHandler(IJualBebasRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task Handle(JualBebasDeclineAfterAcceptCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(JualBebasDeclineAfterAcceptCmd), request.UserId);
        var model = _repo.LoadEntity(JualBebasModel.Key(request.JualBebasId))
            .GetValueOrThrow($"Jual Bebas '{request.JualBebasId}' not found");
        model.AssertExpectedVersion(request.ExpectedVersion);
        model.DeclineAfterAccept(request.UserId, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.CompletedTask;
    }
}
