using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record PasienTelpDto(
    string fs_mr, string fs_kd_jenis_telp, string fs_no_telp, bool fb_default)
{
    public static PasienTelpDto FromMr(PasienModel model)
    {
        var hp = ContactType.Default;
        if (model.ListContact.Any(x => x.JenisContact == JenisContactEnum.Mobile))
            hp = model.ListContact.FirstOrDefault(x => x.JenisContact == JenisContactEnum.Mobile);
        var result = new PasienTelpDto(model.PasienId, "HP", hp.ContactDetail, true);
        return result;
    }
}
