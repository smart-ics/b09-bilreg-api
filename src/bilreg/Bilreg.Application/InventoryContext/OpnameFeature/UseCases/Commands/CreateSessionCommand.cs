using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record CreateSessionScopeItemRequest(string BrgId, string? BrgName = null);

public record CreateSessionCommand(
    string LayananId,
    SessionScopeEnum ScopeMode,
    DateTime CutoffDate,
    string CreatedBy,
    IEnumerable<CreateSessionScopeItemRequest>? ScopeItems = null,
    string? SessionId = null) : IRequest<CreateSessionResponse>;

public record CreateSessionResponse(
    string SessionId,
    string LayananId,
    SessionScopeEnum ScopeMode,
    SessionStatusEnum Status,
    DateTime CutoffDate,
    string CreatedBy,
    DateTime CreatedAt,
    int ScopeItemsCount);

public class CreateSessionHandler : IRequestHandler<CreateSessionCommand, CreateSessionResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public CreateSessionHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<CreateSessionResponse> Handle(
        CreateSessionCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.CreatedBy, nameof(request.CreatedBy));

        // BR-SO-002: Single active session per location
        if (_sessionRepo.HasActiveSessionAtLocation(request.LayananId))
        {
            throw new InvalidOperationException($"Active stock opname session already exists at location '{request.LayananId}'.");
        }

        var sessionId = !string.IsNullOrWhiteSpace(request.SessionId)
            ? request.SessionId.Trim()
            : $"SO{DateTime.Now:yyyyMMdd}{Guid.NewGuid():N}"[..24];

        var scopeItems = new List<StockOpnameScopeItemModel>();
        if (request.ScopeMode == SessionScopeEnum.Partial && request.ScopeItems != null)
        {
            foreach (var item in request.ScopeItems)
            {
                if (!string.IsNullOrWhiteSpace(item.BrgId))
                {
                    var scopeItemId = $"SCP{Guid.NewGuid():N}"[..16];
                    scopeItems.Add(StockOpnameScopeItemModel.Create(
                        scopeItemId,
                        sessionId,
                        item.BrgId,
                        item.BrgName ?? string.Empty));
                }
            }
        }

        var session = StockOpnameSessionModel.Create(
            sessionId: sessionId,
            layananId: request.LayananId,
            scopeMode: request.ScopeMode,
            cutoffDate: request.CutoffDate,
            createdBy: request.CreatedBy,
            createdAt: DateTime.Now,
            scopeItems: scopeItems);

        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new CreateSessionResponse(
            session.SessionId,
            session.LayananId,
            session.ScopeMode,
            session.Status,
            session.CutoffDate,
            session.CreatedBy,
            session.CreatedAt,
            session.ScopeItems.Count));
    }
}
