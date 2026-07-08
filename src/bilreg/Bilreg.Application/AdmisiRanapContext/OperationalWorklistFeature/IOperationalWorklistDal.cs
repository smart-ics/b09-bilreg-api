namespace Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;

public interface IOperationalWorklistDal
{
    IEnumerable<OperationalWorklistItemView> List(OperationalWorklistFilter filter);
}

public record OperationalWorklistFilter(
    string? Jenis = null,
    string? DokterId = null,
    string? BangsalId = null,
    string? KelasId = null,
    string? TipeJaminanId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? SearchTerm = null,
    bool IncludeTerminal = false);

public record OperationalWorklistItemView(
    string ItemId,
    string Jenis,
    int AggregateStatus,
    string PasienId,
    string PasienName,
    string Gender,
    string DokterId,
    string DokterName,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    string TipeJaminanId,
    string TipeJaminanName,
    DateTime SortDate,
    DateTime CrtDate,
    int? Priority);
