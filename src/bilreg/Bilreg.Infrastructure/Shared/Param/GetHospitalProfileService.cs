using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.Shared.Helpers;

namespace Bilreg.Infrastructure.Shared.Param;

public class GetHospitalProfileService : IGetHospitalProfileService
{
    private readonly IParamSistemDal _paramSistemDal;

    public const string ParamKeyKode = "RS__XXXXXX_KODE";
    public const string ParamKeyNama = "RS__XXXXXX_KODE1";
    public const string ParamKeyAlamat = "RS__XXXXXX_KODE2";
    public const string ParamKeyKota = "RS__XXXXXX_KODE3";
    public const string ParamKeyTelepon = "RS__XXXXXX_TELP";
    public const string ParamKeyEmail = "RS__XXXXXX_EMAIL";

    public GetHospitalProfileService(IParamSistemDal paramSistemDal)
    {
        _paramSistemDal = paramSistemDal;
    }

    public HospitalProfileModel Execute()
    {
        var kodeRs = DecodeParam(ParamKeyKode);
        var namaRs = DecodeParam(ParamKeyNama);
        var alamat = DecodeParam(ParamKeyAlamat);
        var kota = DecodeParam(ParamKeyKota);
        var telepon = GetPlainTextParam(ParamKeyTelepon);
        var email = GetPlainTextParam(ParamKeyEmail);

        return new HospitalProfileModel(
            KodeRs: kodeRs,
            NamaRs: namaRs,
            Alamat: alamat,
            Kota: kota,
            Telepon: telepon,
            Email: email);
    }

    private string DecodeParam(string key)
    {
        var val = _paramSistemDal.GetData(key)?.Value;
        if (string.IsNullOrWhiteSpace(val))
            return string.Empty;

        try
        {
            return X1EncryptionHelper.DecodingNeo(val)?.Trim() ?? string.Empty;
        }
        catch
        {
            return val.Trim();
        }
    }

    private string GetPlainTextParam(string key)
    {
        var val = _paramSistemDal.GetData(key)?.Value;
        return val?.Trim() ?? string.Empty;
    }
}
