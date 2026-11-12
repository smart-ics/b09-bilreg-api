using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.StandardFeature;

namespace Bilreg.Infrastructure.BrgContext.BrgFeature;

public record BrgDto(
    string fs_kd_barang,
    string fs_nm_barang,
    bool fb_aktif,
    string fs_ket_barang,
    string fs_kd_grup_rek,
    string fs_kd_pabrik,
    string fs_nm_grup_rek,
    string fs_kd_grup_rek_dk,
    string fs_nm_grup_rek_dk,
    string fs_nm_pabrik)
{
    public static BrgDto FromModel(IBrg model)
    {
        return new BrgDto(
            model.BrgId,
            model.BrgName,
            model.IsAktif,
            model.KetBarang,
            model.GroupRek.GroupRekId,
            model.Pabrik?.PabrikId ?? string.Empty,
            model.GroupRek.GroupRekName,
            model.GroupRekDk.GroupRekDkId,
            model.GroupRekDk.GroupRekDkName,
            model.Pabrik?.PabrikName ?? string.Empty);
    }

    public BarangModel ToModel(IEnumerable<BrgSatuanType> listSatuan)
    {
        var satuanList = listSatuan?.ToList() ?? new List<BrgSatuanType>();
        var satuanKecil = satuanList.OrderBy(x => x.Konversi).FirstOrDefault()?.Satuan ?? SatuanType.Default;
        var satuanBesarItem = satuanList.OrderByDescending(x => x.Konversi).FirstOrDefault();
        var satuanBesar = satuanBesarItem?.Satuan ?? SatuanType.Default;
        var konversi = satuanBesarItem?.Konversi ?? 1;

        return new BarangModel(
            fs_kd_barang,
            fs_nm_barang,
            fb_aktif,
            fs_ket_barang,
            new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek),
            new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk),
            new PabrikReff(fs_kd_pabrik, fs_nm_pabrik),
            satuanKecil,
            satuanBesar,
            konversi);
    }

    [Obsolete("Deprecated in favor of ToModel")]
    public BrgObatType ToModelObat(IEnumerable<BrgSatuanType> listSatuan)
    {
        return new BrgObatType(
            fs_kd_barang,
            fs_nm_barang,
            fb_aktif,
            fs_ket_barang,
            new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek),
            new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk),
            GolonganType.Default,
            GroupObatDkType.Default,
            KelompokType.Default,
            SifatType.Default,
            BentukType.Default,
            new PabrikType(fs_kd_pabrik, fs_nm_pabrik),
            GenerikType.Default.ToReff(),
            KelasTerapiType.Default,
            GolTerapiType.Default,
            OriginalType.Default,
            listSatuan);
    }

    [Obsolete("Deprecated in favor of ToModel")]
    public BrgBhpType ToModelBhp(IEnumerable<BrgSatuanType> listSatuan)
    {
        return new BrgBhpType(
            fs_kd_barang,
            fs_nm_barang,
            fb_aktif,
            fs_ket_barang,
            new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek),
            new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk),
            GolonganType.Default,
            GroupObatDkType.Default,
            KelompokType.Default,
            SifatType.Default,
            BentukType.Default,
            new PabrikType(fs_kd_pabrik, fs_nm_pabrik),
            listSatuan);
    }
}