using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSearchQuery(string Keyword) : IRequest<IEnumerable<JaminanSearchResponse>>;

public record JaminanSearchResponse(
    string JaminanId,
    string JaminanName,
    string CaraBayarDkName,
    string GrupJaminanName);

public class JeminanSearchHandler : IRequestHandler<JaminanSearchQuery, IEnumerable<JaminanSearchResponse>>
{
    private readonly IJaminanDal _dal;

    public JeminanSearchHandler(IJaminanDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<JaminanSearchResponse>> Handle(JaminanSearchQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");
        var listJaminan = _dal.ListData()
            .Match(
                some => some,    
                () => throw new KeyNotFoundException("Jaminan not found"));

        var result = listJaminan.ToList()
            .Where(x => x.JaminanName.ToLower().Contains(request.Keyword.ToLower()))
            .Select(x => new JaminanSearchResponse(x.JaminanName, x.JaminanName, 
                x.CaraBayarDk.CaraBayarDkName, x.GroupJaminan.GroupJaminanName));

        return Task.FromResult(result.Distinct());
    }
}