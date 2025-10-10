using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record DeepSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienModel>>;

public class DeepSearchPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<SearchPasienModel>>
{
    private readonly IDeepSearchPasienDal _deepSearchDal;

    public DeepSearchPasienHandler(IDeepSearchPasienDal deepSearchDal)
    {
        _deepSearchDal = deepSearchDal;
    }

    public Task<IEnumerable<SearchPasienModel>> Handle(DeepSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");
        var result = new List<SearchPasienModel>();
        var keyword = request.Keyword.Trim();
        if (keyword.All(char.IsLetter))
            result.AddRange(SearchByPasienName(keyword));
        else
            result.AddRange(Search(keyword));

        return Task.FromResult(result.Distinct());
    }

    #region ByNotName
    private IEnumerable<SearchPasienModel> Search(string keyword)
    {
        var result = _deepSearchDal.ListData(keyword)
                .Match(
                    some => some,
                    () => throw new KeyNotFoundException("data not found")
                );
        return result;
    }
    #endregion
    
    #region ByName
    private IEnumerable<SearchPasienModel> SearchByPasienName(string name)
    {
        var variasiEjaan = GenerateVariasiEjaan(name);
        var listKeyword = variasiEjaan
            .SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        listKeyword.Add(name);
        
        var result = FindByWords(listKeyword);

        return result;
    }

    private record Ejaan(string Eja1, string Eja2);
    private static IEnumerable<string> GenerateVariasiEjaan(string originName)
    {
        var spellingVariations = new List<Ejaan>
         {
             new("dj", "j"), new("j", "dj"),
             new("tj", "c"), new("c", "tj"),
             new("sj", "sy"), new("sy", "sj"),
             new("oe", "u"), new("u", "oe"),
             new("dh", "d"), new("d", "dh"),
             new("j", "y"), new("y", "j"),
             new("i", "ie"), new("ie", "i")
         };

        var cleanOriginName = RemovePunctuation(originName);

        // collection variasi ejaan original name
        var result = spellingVariations
            .Aggregate(new List<string> { cleanOriginName }, (variants, entry) => variants
                .Concat(variants
                    .Where(name => name.Contains(entry.Eja1, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name.Replace(entry.Eja1, entry.Eja2, StringComparison.OrdinalIgnoreCase))
                ).Distinct().ToList()
            );
        return result;
    }
    private static string RemovePunctuation(string input)
    {
        return new string(input.Where(c => !char.IsPunctuation(c)).ToArray());
    }

    private IEnumerable<SearchPasienModel> FindByWords(IEnumerable<string> variasiEjaan)
    {
        var result = _deepSearchDal.ListData(variasiEjaan)
            .Match(
                    some => some,
                    () => new List<SearchPasienModel>()
                );

        return result;
    }
    #endregion
}
