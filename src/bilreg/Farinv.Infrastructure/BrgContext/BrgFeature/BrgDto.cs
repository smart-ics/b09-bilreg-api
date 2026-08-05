using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record BrgDto(
    string fs_kd_barang, string fs_nm_barang, bool fb_aktif,
    string fs_ket_barang, string fs_kd_grup_rek,
    //
    string fs_kd_golongan, string fs_kd_kelompok, string fs_kd_sifat,
    string fs_kd_bentuk, string fs_kd_pabrik, string fs_kd_gol_obat_dk,
    //
    string fs_kd_generik, string fs_kd_gol_terapi, string fs_kd_kelas_terapi,
    string fs_kd_original,
    //
    string fs_nm_grup_rek, string fs_kd_grup_rek_dk, string fs_nm_grup_rek_dk,
    //
    string fs_nm_golongan, string fs_nm_kelompok, string fs_nm_sifat,
    string fs_nm_bentuk, string fs_nm_pabrik, string fs_nm_gol_obat_dk,
    //
    string fs_nm_generik, string fs_nm_gol_terapi, string fs_nm_kelas_terapi,
    string fs_nm_original)
{
    public static BrgDto FromModel(BrgObatType model)
    {
        return new BrgDto(model.BrgId, model.BrgName, model.IsAktif, model.KetBarang, model.GroupRek.GroupRekId,
            model.Golongan.GolonganId, model.Kelompok.KelompokId, model.Sifat.SifatId,
            model.Bentuk.BentukId, model.Pabrik.PabrikId, model.GroupObatDk.GroupObatDkId,
            model.Generik.GenerikId, model.GolTerapi.GolTerapiId, model.KelasTerapi.KelasTerapiId,
            model.Original.OriginalId,
            model.GroupRek.GroupRekName, model.GroupRekDk.GroupRekDkId, model.GroupRekDk.GroupRekDkName,
            model.GroupObatDk.GroupObatDkId, model.Kelompok.KelompokName, model.Sifat.SifatName,
            model.Bentuk.BentukName, model.Pabrik.PabrikName, model.GroupObatDk.GroupObatDkName,
            model.Generik.GenerikName, model.GolTerapi.GolTerapiName, model.KelasTerapi.KelasTerapiName,
            model.Original.OriginalName);
    }

    public static BrgDto FromModel(BrgBhpType model)
    {
        return new BrgDto(model.BrgId, model.BrgName, model.IsAktif, model.KetBarang, model.GroupRek.GroupRekId,
            model.Golongan.GolonganId, model.Kelompok.KelompokId, model.Sifat.SifatId,
            model.Bentuk.BentukId, model.Pabrik.PabrikId, model.GroupObatDk.GroupObatDkId,
            "", "", "","",
            model.GroupRek.GroupRekName, model.GroupRekDk.GroupRekDkId, model.GroupRekDk.GroupRekDkName,
            model.GroupObatDk.GroupObatDkId, model.Kelompok.KelompokName, model.Sifat.SifatName,
            model.Bentuk.BentukName, model.Pabrik.PabrikName, model.GroupObatDk.GroupObatDkName,
            "", "", "","");
    }
    
    public BrgObatType ToModelObat(IEnumerable<BrgSatuanType> listSatuan)
    {
        return new BrgObatType(fs_kd_barang, fs_nm_barang, fb_aktif, fs_ket_barang, 
            new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek),
            new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk),
            new GolonganType(fs_kd_golongan, fs_nm_golongan),
            new GroupObatDkType(fs_kd_gol_obat_dk, fs_nm_gol_obat_dk),
            new KelompokType(fs_kd_kelompok, fs_nm_kelompok),
            new SifatType(fs_kd_sifat, fs_nm_sifat),
            new BentukType(fs_kd_bentuk, fs_nm_bentuk),
            new PabrikType(fs_kd_pabrik, fs_nm_pabrik),
            new GenerikReff(fs_kd_generik, fs_nm_generik),
            new KelasTerapiType(fs_kd_kelas_terapi, fs_nm_kelas_terapi),
            new GolTerapiType(fs_kd_gol_terapi, fs_nm_gol_terapi),
            new OriginalType(fs_kd_original, fs_nm_original),
            listSatuan);
    }
    public BrgBhpType ToModelBhp(IEnumerable<BrgSatuanType> listSatuan)
    {
        return new BrgBhpType(fs_kd_barang, fs_nm_barang, fb_aktif, fs_ket_barang, 
            new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek),
            new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk),
            new GolonganType(fs_kd_golongan, fs_nm_golongan),
            new GroupObatDkType(fs_kd_gol_obat_dk, fs_nm_gol_obat_dk),
            new KelompokType(fs_kd_kelompok, fs_nm_kelompok),
            new SifatType(fs_kd_sifat, fs_nm_sifat),
            new BentukType(fs_kd_bentuk, fs_nm_bentuk),
            new PabrikType(fs_kd_pabrik, fs_nm_pabrik),
            listSatuan);
    }
    

}