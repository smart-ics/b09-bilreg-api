using Bilreg.Domain.ApotekContext.QueueFeature;

namespace Bilreg.Application.ApotekContext.QueueFeature;

public interface IPharmacyLoketServicePointRepo
{
    IReadOnlyList<string> ListServicePointsByLoket(string loketKey);
    IReadOnlyList<ApotekLoketServicePointModel> ListMappingsByLoket(string loketKey);
    void ReplaceServicePoints(string loketKey, IEnumerable<string> servicePointIds, string actor, DateTime at);
}
