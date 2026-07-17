using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegInapRepo : IRegInapRepo
{
    private readonly Ita_reg_inap_dal _taRegInapDal;
    private readonly IRegHistoryDokterDal _regHistoryDokterDal;
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;

    public RegInapRepo(
        Ita_reg_inap_dal taRegInapDal,
        IRegHistoryDokterDal regHistoryDokterDal,
        IProsedureMasukInapRepo prosedurMasukInapRepo)
    {
        _taRegInapDal = taRegInapDal;
        _regHistoryDokterDal = regHistoryDokterDal;
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
    }

    public void SaveChanges(RegInapModel model)
    {
        var dto = ta_reg_inap_dto.FromModel(model);
        var existing = _taRegInapDal.GetData(model);
        if (existing is null)
            _taRegInapDal.Insert(dto);
        else
            _taRegInapDal.Update(dto);

        _regHistoryDokterDal.Delete(model);
        var history = model.ListDokter
            .Select(x => RegHistoryDokterDto.FromModel(model.RegId, x))
            .ToList();
        if (history.Count > 0)
            _regHistoryDokterDal.Insert(history);
    }

    public MayBe<RegInapModel> LoadEntity(IRegKey key)
    {
        var header = _taRegInapDal.GetData(key);
        if (header is null)
            return MayBe<RegInapModel>.None;

        var prosedurId = ta_reg_inap_dto.NormalizeOptional(header.fs_kd_caramasuk_inap);
        if (prosedurId.Length == 0)
            throw new InvalidOperationException(
                $"ta_reg_inap '{key.RegId}' memiliki prosedur masuk inap kosong.");

        var prosedur = _prosedurMasukInapRepo
            .LoadEntity(ProsedurMasukInapType.Key(prosedurId))
            .GetValueOrThrow(
                $"Prosedur Masuk Inap '{prosedurId}' pada ta_reg_inap '{key.RegId}' tidak ditemukan.");

        var history = _regHistoryDokterDal.ListData(key)?.ToList() ?? [];
        var assignments = history.Select(MapHistoryToAssignment).ToList();

        return MayBe.From(RegInapModel.Rehydrate(header.fs_kd_reg.Trim(), prosedur, assignments));
    }

    public void DeleteEntity(IRegKey key)
    {
        _regHistoryDokterDal.Delete(key);
        _taRegInapDal.Delete(key);
    }

    private static RegDokterType MapHistoryToAssignment(RegHistoryDokterDto dto)
    {
        var releaseRaw = ta_reg_inap_dto.NormalizeOptional(dto.fd_tgl_selesai);
        var releaseDate = releaseRaw.Length == 0
            ? (DateOnly?)null
            : DateOnly.Parse(releaseRaw);

        return RegDokterType.Rehydrate(
            new PpaReff(dto.fs_kd_dokter.Trim(), dto.fs_nm_peg?.Trim() ?? string.Empty),
            DokterRoleEnum.Dpjp,
            dto.fb_primer ? DpjpResponsibilityEnum.Primary : DpjpResponsibilityEnum.Secondary,
            DateOnly.Parse(dto.fd_tgl_mulai.Trim()),
            releaseDate);
    }
}