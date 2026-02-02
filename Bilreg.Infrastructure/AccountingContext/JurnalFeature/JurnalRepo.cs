using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.JurnalFeature;

public class JurnalRepo : IJurnalRepo
{
    private readonly IJurnalDal _jurnalDal;
    private readonly IJurnal2Dal _jurnal2Dal;

    public JurnalRepo(IJurnalDal jurnalDal, IJurnal2Dal jurnal2Dal)
    {
        _jurnalDal = jurnalDal;
        _jurnal2Dal = jurnal2Dal;
    }

    public void SaveChanges(JurnalType model)
    {
        // Simpan header jurnal (insert/update)
        LoadEntity(model)
            .Match(
                onSome: _ => _jurnalDal.Update(JurnalDto.FromModel(model)),
                onNone: () => _jurnalDal.Insert(JurnalDto.FromModel(model)));

        // Siapkan DTO detail jurnal2
        var listJurnal2Dto = model.ListJurnal2
            .Select(x => Jurnal2Dto.FromModel(x, model.JurnalId));

        // Hapus semua detail lama, lalu insert baru (replace strategy)
        _jurnal2Dal.Delete(model);
        _jurnal2Dal.Insert(listJurnal2Dto);
    }

    public MayBe<JurnalType> LoadEntity(IJurnalKey key)
    {
        var headerDto = _jurnalDal.GetData(key);
        if (headerDto is null)
            return MayBe<JurnalType>.None;

        var detailDtos = _jurnal2Dal.ListData(key);
        var detailModels = detailDtos.Select(dto => dto.ToModel());

        var jurnalModel = headerDto.ToModel(detailModels);

        return MayBe.From(jurnalModel);
    }

    public void DeleteEntity(IJurnalKey key)
    {
        // Hapus detail dulu (opsional, tergantung constraint DB)
        _jurnal2Dal.Delete(key);
        // Hapus header
        _jurnalDal.Delete(key);
    }
}