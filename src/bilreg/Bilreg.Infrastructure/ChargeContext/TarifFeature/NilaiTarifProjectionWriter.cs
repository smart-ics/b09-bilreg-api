using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class NilaiTarifProjectionWriter : INilaiTarifProjectionWriter
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly INilaiTarifDal _nilaiTarifDal;
    private readonly INilaiTarifKompDal _nilaiTarifKompDal;

    public NilaiTarifProjectionWriter(
        INilaiTarifRepo nilaiTarifRepo,
        INilaiTarifDal nilaiTarifDal,
        INilaiTarifKompDal nilaiTarifKompDal)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _nilaiTarifDal = nilaiTarifDal;
        _nilaiTarifKompDal = nilaiTarifKompDal;
    }

    public string Upsert(NilaiTarifType projection, string sourcePolicyId)
    {
        var compositeKey = NilaiTarifType.KeyComposite(
            TarifType.Key(projection.TarifId),
            TipeTarifType.Key(projection.TipeTarif.TipeTarifId),
            KelasType.Key(projection.Kelas.KelasId));

        var nilaiTarifId = _nilaiTarifRepo.LoadEntity(compositeKey)
            .Match(
                onSome: existing => existing.NilaiTarifId,
                onNone: () => Ulid.NewUlid().ToString());

        var dto = NilaiTarifDto.FromModel(projection, sourcePolicyId) with { NilaiTarifId = nilaiTarifId };

        var existingById = _nilaiTarifDal.GetData(NilaiTarifType.Key(nilaiTarifId));
        if (existingById is null)
            _nilaiTarifDal.Insert(dto);
        else
            _nilaiTarifDal.Update(dto);

        var key = NilaiTarifType.Key(nilaiTarifId);
        _nilaiTarifKompDal.Delete(key);
        var komponenDtos = projection.ListKomponen
            .Select(line => NilaiTarifKompDto.FromModel(nilaiTarifId, line))
            .ToList();
        if (komponenDtos.Count > 0)
            _nilaiTarifKompDal.Insert(komponenDtos);

        return nilaiTarifId;
    }
}
