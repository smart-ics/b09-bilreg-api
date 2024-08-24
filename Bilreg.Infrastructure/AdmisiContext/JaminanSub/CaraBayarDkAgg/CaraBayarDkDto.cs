using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public class CaraBayarDkDto
{
    public string fs_kd_cara_bayar_dk { get; set; }
    public string fs_nm_cara_bayar_dk { get; set; }

    public CaraBayarDkModel ToModel() => CaraBayarDkModel.Create(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
}